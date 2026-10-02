<#
.SYNOPSIS
    Erfasst die Trefferlisten des bestehenden BE-LabelEditor auf dem synthetischen Datensatz,
    als erwartete Ergebnisse für TC04.

.DESCRIPTION
    Das Skript bedient die Oberfläche des laufenden BE-LabelEditor über UI Automation. Es
    stellt jeden der acht Suchmodi ein, sucht jeden Begriff aus der Begriffsliste und liest die
    vollständige Trefferliste mit ID, angezeigter Sprache und deren Score. Es ändert keine
    Datei des Tools und keine Label-Datei.

    Vorbereitung, von Hand:
    1. settings.json des Tools sichern.
    2. Darin SearchPaths auf eine Kopie von
       BE.LabelExtension.Tests\TestData\PackagesLocalDirectory setzen und LanguagesToLoad auf
       de, en-us, fr-ch, it-ch, die Sprachen der Unit Tests.
    3. Das Tool starten und warten, bis es die Labels geladen hat.
    4. Dieses Skript ausführen, danach das Tool schliessen und erst dann settings.json
       zurückkopieren, weil das Tool seine Einstellungen beim Beenden schreibt.

    Läuft unter Windows PowerShell 5.1.

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File .\Capture-ExistingToolResults.ps1 -ProcessId 1234 -Output ..\BE.LabelExtension.Tests\TestData\ExpectedSearchResults.json
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][int]$ProcessId,
    [Parameter(Mandatory)][string]$Output,
    [string]$TermsFile = (Join-Path $PSScriptRoot 'ExistingToolSearchTerms.txt')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
$A = [System.Windows.Automation.AutomationElement]
$Scope = [System.Windows.Automation.TreeScope]
$Modes = @('ExactMatch', 'ExactMatchIgnoreCase', 'SubString', 'SubStringIgnoreCase', 'AnythingLikeThat', 'AnythingLikeThatIgnoreCase', 'LabelId', 'MatchWord')
$Terms = @([IO.File]::ReadAllLines($TermsFile, [Text.Encoding]::UTF8) | Where-Object { $_ -ne '' })

function Cond($property, $value) { New-Object System.Windows.Automation.PropertyCondition($property, $value) }

$window = $A::RootElement.FindFirst($Scope::Children, (Cond $A::ProcessIdProperty $ProcessId))
if (-not $window) { throw "No window of process $ProcessId." }
$combo = $window.FindFirst($Scope::Descendants, (Cond $A::ControlTypeProperty ([System.Windows.Automation.ControlType]::ComboBox)))
$search = $window.FindFirst($Scope::Descendants, (Cond $A::AutomationIdProperty 'SearchText'))
$button = [System.Windows.Automation.TreeWalker]::ControlViewWalker.GetNextSibling($search)
$list = $window.FindFirst($Scope::Descendants, (Cond $A::AutomationIdProperty 'listBox'))

# The tool disables its controls while it loads the label files.
$deadline = (Get-Date).AddSeconds(90)
while (-not ($combo.Current.IsEnabled -and $search.Current.IsEnabled) -and (Get-Date) -lt $deadline) { Start-Sleep -Milliseconds 500 }
if (-not $combo.Current.IsEnabled) { throw 'The tool did not become ready. Did it find the label files?' }

function Select-Mode([string]$mode) {
    $combo.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Expand()
    Start-Sleep -Milliseconds 300
    $item = $null
    foreach ($candidate in $combo.FindAll($Scope::Descendants, (Cond $A::ControlTypeProperty ([System.Windows.Automation.ControlType]::ListItem)))) {
        if ($candidate.Current.Name -like "($mode,*") { $item = $candidate; break }
    }
    if (-not $item) { throw "Mode $mode not found." }
    $item.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
    $combo.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Collapse()
    Start-Sleep -Milliseconds 300
}

function Invoke-Search([string]$term, [int]$wait) {
    $search.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue($term)
    $button.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    Start-Sleep -Milliseconds $wait
}

# The list is virtualized: only the rows on screen exist for UI Automation. The item
# container walks all entries and realizes each one before it is read.
function Read-Hits {
    $container = $list.GetCurrentPattern([System.Windows.Automation.ItemContainerPattern]::Pattern)
    $hits = @()
    $item = $container.FindItemByProperty($null, $null, $null)
    while ($item) {
        $virtualized = $null
        if ($item.TryGetCurrentPattern([System.Windows.Automation.VirtualizedItemPattern]::Pattern, [ref]$virtualized)) { $virtualized.Realize() }
        $texts = @($item.FindAll($Scope::Descendants, (Cond $A::ControlTypeProperty ([System.Windows.Automation.ControlType]::Text))) | ForEach-Object { $_.Current.Name })
        # Layout of an entry: <empty> | language | score | "Score:" | ID | text | ...
        $scoreIndex = [Array]::IndexOf($texts, 'Score:')
        $hits += [ordered]@{ id = $texts[$scoreIndex + 1]; score = [int]$texts[$scoreIndex - 1]; language = $texts[$scoreIndex - 2] }
        $item = $container.FindItemByProperty($item, $null, $null)
    }
    return ,$hits
}

$results = @()
foreach ($mode in $Modes) {
    Select-Mode $mode
    foreach ($term in $Terms) {
        # A term that finds nothing first, so a stale list is never read.
        Invoke-Search '§§§nothing§§§' 250
        Invoke-Search $term 700
        $hits = Read-Hits
        $results += [ordered]@{ mode = $mode; term = $term; hits = $hits }
        Write-Host ("{0,-28} {1,-28} {2,3} hits" -f $mode, $term, $hits.Count)
    }
}

[IO.File]::WriteAllText($Output, ($results | ConvertTo-Json -Depth 5), (New-Object System.Text.UTF8Encoding($false)))
Write-Host "Written $($results.Count) searches to $Output"
