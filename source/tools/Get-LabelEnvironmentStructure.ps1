<#
.SYNOPSIS
    Liest den Aufbau der Metadaten-Konfigurationen und der Package-Verzeichnisse, als
    Vorlage für die synthetischen Testdaten der BE-LabelExtension.

.DESCRIPTION
    Das Skript liest nur. Es schreibt keine Datei und ändert nichts. Die Ausgabe geht auf
    die Konsole und enthält nur Feldnamen, Typen, Anzahlen, Laufzeiten, Sprachcodes und
    Pfade mit Platzhaltern. Namen von Models, Packages, Label-Dateien, Elementen, Benutzern
    oder Rechnern gibt es nicht aus.

    Geprüft wird:
    - die JSON-Dateien in %LOCALAPPDATA%\Microsoft\Dynamics365\XPPConfig: Felder und Typen
    - je Package-Verzeichnis die Descriptor-Dateien: Elemente, Layer, Locked und was die
      Regel zum Schreibschutz daraus macht
    - die Label-Dateien: Ablage, Benennung und, für eine Stichprobe, Kodierung, Zeilenenden,
      Kommentare und Form der IDs
    - die XML-Beschreibungen der Label-Dateien: ob das Element Language fehlt
    - die kompilierten Ressourcen *.Resources.dll: Ablage und Dauer der rekursiven Suche,
      für bis zu 20 mit verschiedenen Dateinamen der Name der Manifest-Ressource und die Form
      der Einträge. Die Assembly wird dafür nur zum Lesen geöffnet (ReflectionOnly), es wird
      kein Code ausgeführt. Deshalb Windows PowerShell 5.1, PowerShell 7 kennt ReflectionOnly
      nicht.
    - die Element-Dateien für die Verwendungssuche: Anzahl, Grösse und Dauer der Aufzählung
    - den Ordner XppSource des X++-Editors

.PARAMETER PackagesDirectory
    Weitere Package-Verzeichnisse, etwa K:\AOSService\PackagesLocalDirectory auf der
    klassischen Entwicklungs-VM. Gefundene Standardverzeichnisse werden ohnehin geprüft.

.PARAMETER SkipElementFiles
    Überspringt das Zählen der Element-Dateien, das auf grossen Verzeichnissen dauert.

.PARAMETER LabelFileSample
    Anzahl Label-Dateien je Package-Verzeichnis, deren Inhalt ausgewertet wird.

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File .\Get-LabelEnvironmentStructure.ps1 > "$env:USERPROFILE\Desktop\label-struktur.txt"
#>
[CmdletBinding()]
param(
    [string[]]$PackagesDirectory = @(),
    [switch]$SkipElementFiles,
    [int]$LabelFileSample = 300
)

$ErrorActionPreference = 'Continue'

# Ordnernamen, die nichts über Kunden, Models oder Personen verraten und deshalb im
# Klartext erscheinen dürfen. Alles andere wird zu <Name>.
$GenericSegments = @(
    'Microsoft', 'Dynamics365', 'PackagesLocalDirectory', 'Metadata', 'XppSource', 'XPPConfig',
    'AOSService', 'bin', 'Resources', 'Descriptor', 'AxLabelFile', 'LabelResources',
    'AppData', 'Local', 'Roaming'
)

$LayerNames = @('SYS', 'SYP', 'GLS', 'GLP', 'FPK', 'FPP', 'SLN', 'SLP', 'ISV', 'ISP', 'VAR', 'VAP', 'CUS', 'CUP', 'USR', 'USP')

function Out-Line([string]$Text = '') { Write-Output $Text }

function Add-Count([hashtable]$Table, [string]$Key) {
    if ([string]::IsNullOrEmpty($Key)) { $Key = '<leer>' }
    $Table[$Key] = 1 + [int]$Table[$Key]
}

function Out-Counts([string]$Title, [hashtable]$Table, [string]$Indent = '    ') {
    Out-Line "$Indent$Title"
    if ($Table.Count -eq 0) { Out-Line "$Indent  (keine)"; return }
    foreach ($entry in ($Table.GetEnumerator() | Sort-Object Name)) {
        Out-Line ("{0}  {1}: {2}" -f $Indent, $entry.Name, $entry.Value)
    }
}

# Die in Windows vordefinierten Kulturen. GetCultureInfo allein genügt nicht: Windows nimmt
# jeden wohlgeformten Code wie "abc" an und meldet ihn als "Unknown Language".
$PredefinedCultures = New-Object 'System.Collections.Generic.HashSet[string]' (
    [string[]]([Globalization.CultureInfo]::GetCultures([Globalization.CultureTypes]::AllCultures) | ForEach-Object { $_.Name })),
    ([StringComparer]::OrdinalIgnoreCase)

function Test-LanguageCode([string]$Segment) {
    # Sprachordner wie de, en-US oder zh-Hans. Der erste Teil ist klein geschrieben, was
    # Namen von Models in der Regel ausschliesst, und der Code muss eine vordefinierte Kultur sein.
    if ($Segment -cnotmatch '^[a-z]{2,3}(-[A-Za-z]{2,4})?$') { return $false }
    return $PredefinedCultures.Contains($Segment)
}

function Get-SegmentShape([string]$Segment, [switch]$AllowLanguage) {
    if ($GenericSegments -contains $Segment) { return $Segment }
    if ($Segment -match '^\d+(\.\d+)+$') { return '<Version>' }
    if ($AllowLanguage -and (Test-LanguageCode $Segment)) { return $Segment }
    return '<Name>'
}

function Get-PathShape([string]$Path) {
    if ([string]::IsNullOrWhiteSpace($Path)) { return '<leer>' }
    $rest = $Path.TrimEnd('\')
    $head = $null
    foreach ($pair in @(@('%LOCALAPPDATA%', $env:LOCALAPPDATA), @('%APPDATA%', $env:APPDATA), @('%USERPROFILE%', $env:USERPROFILE))) {
        if ($pair[1] -and $rest.StartsWith($pair[1], [StringComparison]::OrdinalIgnoreCase)) {
            $head = $pair[0]
            $rest = $rest.Substring($pair[1].Length).TrimStart('\')
            break
        }
    }
    if (-not $head) {
        if ($rest -match '^([A-Za-z]):\\?(.*)$') { $head = $Matches[1].ToUpper() + ':'; $rest = $Matches[2] }
        elseif ($rest.StartsWith('\\')) { $head = '\\<Server>'; $rest = ($rest.TrimStart('\') -split '\\', 2)[1] }
        else { $head = '<relativ>' }
    }
    $shape = @($head)
    foreach ($segment in ($rest -split '\\' | Where-Object { $_ -ne '' })) { $shape += Get-SegmentShape $segment }
    return ($shape -join '\')
}

function Get-JsonType($Value) {
    if ($null -eq $Value) { return 'null' }
    if ($Value -is [string]) { return 'string' }
    if ($Value -is [bool]) { return 'bool' }
    if ($Value -is [int] -or $Value -is [long] -or $Value -is [double] -or $Value -is [decimal]) { return 'number' }
    if ($Value -is [System.Array]) { return "array, $($Value.Count) Einträge" }
    if ($Value -is [System.Management.Automation.PSCustomObject]) { return 'object' }
    return $Value.GetType().Name
}

function Test-IsPath([string]$Text) { return ($Text -match '^[A-Za-z]:\\' -or $Text -match '^\\\\') }

function Test-UnderPath([string]$Child, [string]$Parent) {
    if (-not $Child -or -not $Parent) { return $false }
    $p = $Parent.TrimEnd('\') + '\'
    return ($Child.TrimEnd('\') + '\').StartsWith($p, [StringComparison]::OrdinalIgnoreCase)
}

# ---------------------------------------------------------------------------------------
# Metadaten-Konfigurationen
# ---------------------------------------------------------------------------------------
$directories = New-Object System.Collections.ArrayList   # @{ Path; Role; ReadOnlyByLocation }

Out-Line '=== Metadaten-Konfigurationen (XPPConfig) ==='
$configDir = Join-Path $env:LOCALAPPDATA 'Microsoft\Dynamics365\XPPConfig'
if (-not (Test-Path -LiteralPath $configDir)) {
    Out-Line "Ordner $(Get-PathShape $configDir) nicht vorhanden"
}
else {
    $configs = @(Get-ChildItem -LiteralPath $configDir -Filter *.json -File -ErrorAction SilentlyContinue | Sort-Object LastWriteTime)
    Out-Line "Ordner: $(Get-PathShape $configDir), $($configs.Count) JSON-Dateien (ältere zuerst)"
    $index = 0
    foreach ($file in $configs) {
        $index++
        Out-Line ''
        Out-Line "Konfiguration $index (Dateiname ohne Endung = Name, hier nicht ausgegeben)"
        try { $json = Get-Content -LiteralPath $file.FullName -Raw -ErrorAction Stop | ConvertFrom-Json -ErrorAction Stop }
        catch { Out-Line "  nicht lesbar: $($_.Exception.GetType().Name)"; continue }

        foreach ($property in $json.PSObject.Properties) {
            $type = Get-JsonType $property.Value
            $detail = ''
            if ($property.Value -is [string]) {
                if (Test-IsPath $property.Value) { $detail = " = $(Get-PathShape $property.Value), vorhanden: $(Test-Path -LiteralPath $property.Value)" }
                else { $detail = " (kein Pfad, Länge $($property.Value.Length))" }
            }
            elseif ($property.Value -is [bool] -or $type -eq 'number') { $detail = " = $($property.Value)" }
            Out-Line "  $($property.Name): $type$detail"
            if ($property.Value -is [System.Array]) {
                $i = 0
                foreach ($item in $property.Value) {
                    if ($item -is [string] -and (Test-IsPath $item)) { Out-Line "    [$i] = $(Get-PathShape $item), vorhanden: $(Test-Path -LiteralPath $item)" }
                    else { Out-Line "    [$i]: $(Get-JsonType $item)" }
                    $i++
                }
            }
        }

        $store = [string]$json.ModelStoreFolder
        $framework = [string]$json.FrameworkDirectory
        $references = @($json.ReferencePackagesPaths | Where-Object { $_ -is [string] })
        $debugSource = [string]$json.DebugSourceFolder
        Out-Line "  Beziehungen:"
        Out-Line "    FrameworkDirectory steht auch in ReferencePackagesPaths: $(@($references | Where-Object { $_.TrimEnd('\') -ieq $framework.TrimEnd('\') }).Count -gt 0)"
        Out-Line "    ModelStoreFolder liegt unter FrameworkDirectory: $(Test-UnderPath $store $framework)"
        Out-Line "    ModelStoreFolder liegt unter einem ReferencePackagesPath: $(@($references | Where-Object { Test-UnderPath $store $_ }).Count -gt 0)"
        Out-Line "    DebugSourceFolder liegt unter ModelStoreFolder: $(Test-UnderPath $debugSource $store)"
        foreach ($candidate in @(@('ModelStoreFolder\XppSource', $(if ($store) { Join-Path $store 'XppSource' })), @('DebugSourceFolder', $debugSource))) {
            if ($candidate[1]) { Out-Line "    $($candidate[0]) vorhanden: $(Test-Path -LiteralPath $candidate[1])" }
        }

        if ($store) { [void]$directories.Add(@{ Path = $store; Role = "Konfiguration $index, ModelStoreFolder"; ReadOnlyByLocation = $false }) }
        if ($framework) { [void]$directories.Add(@{ Path = $framework; Role = "Konfiguration $index, FrameworkDirectory"; ReadOnlyByLocation = $true }) }
        $r = 0
        foreach ($reference in $references) { [void]$directories.Add(@{ Path = $reference; Role = "Konfiguration $index, ReferencePackagesPaths[$r]"; ReadOnlyByLocation = $true }); $r++ }

        # XppSource: Aufbau der Dateien, die der X++-Editor öffnet
        foreach ($xpp in @($(if ($store) { Join-Path $store 'XppSource' }), $debugSource)) {
            if (-not $xpp -or -not (Test-Path -LiteralPath $xpp)) { continue }
            $modelDirs = @(Get-ChildItem -LiteralPath $xpp -Directory -ErrorAction SilentlyContinue)
            $xppFiles = @(Get-ChildItem -LiteralPath $xpp -Filter *.xpp -File -Recurse -ErrorAction SilentlyContinue)
            $patternHits = @($xppFiles | Where-Object { $_.Name -match '^Ax[A-Za-z]+_.+\.xpp$' }).Count
            $depths = @{}
            foreach ($f in $xppFiles) { Add-Count $depths ([string](($f.FullName.Substring($xpp.TrimEnd('\').Length).TrimStart('\') -split '\\').Count - 1)) }
            Out-Line "  XppSource $(Get-PathShape $xpp): $($modelDirs.Count) Unterordner, $($xppFiles.Count) .xpp-Dateien, davon $patternHits im Muster Ax<Typ>_<Name>.xpp"
            Out-Counts 'Ordnertiefe der .xpp-Dateien unter XppSource:' $depths '    '
        }
    }
}

foreach ($classic in @('K:\AOSService\PackagesLocalDirectory', 'C:\AOSService\PackagesLocalDirectory')) {
    if (Test-Path -LiteralPath $classic) { [void]$directories.Add(@{ Path = $classic; Role = 'Standardverzeichnis der klassischen Entwicklungs-VM'; ReadOnlyByLocation = $false }) }
}
foreach ($extra in $PackagesDirectory) { [void]$directories.Add(@{ Path = $extra; Role = 'Parameter PackagesDirectory'; ReadOnlyByLocation = $false }) }

# Jedes Verzeichnis nur einmal, unter der ersten Rolle
$seen = @{}
$unique = @()
foreach ($d in $directories) {
    $key = $d.Path.TrimEnd('\').ToLowerInvariant()
    if ($seen.ContainsKey($key)) { $seen[$key].Role += "; $($d.Role)"; continue }
    $seen[$key] = $d
    $unique += $d
}

# ---------------------------------------------------------------------------------------
# Package-Verzeichnisse
# ---------------------------------------------------------------------------------------
foreach ($dir in $unique) {
    $root = $dir.Path.TrimEnd('\')
    Out-Line ''
    Out-Line "=== Package-Verzeichnis $(Get-PathShape $root) ==="
    Out-Line "Rolle: $($dir.Role)"
    if (-not (Test-Path -LiteralPath $root)) { Out-Line 'nicht vorhanden'; continue }

    $packages = @(Get-ChildItem -LiteralPath $root -Directory -ErrorAction SilentlyContinue)
    $descriptorFiles = 0; $unreadable = 0; $complete = 0; $incomplete = 0
    $modelDirFound = 0; $modelDirMissing = 0; $nameEqualsFile = 0; $nameDiffers = 0
    $rootElements = @{}; $childElements = @{}; $layers = @{}; $locked = @{}; $verdicts = @{}
    $labelLanguages = @{}; $labelNaming = @{}; $labelXml = @{}
    $labelTxtKeys = New-Object 'System.Collections.Generic.HashSet[string]'
    $sample = New-Object System.Collections.ArrayList
    $labelFileCount = 0; $extensionNamed = 0

    foreach ($package in $packages) {
        $descriptorDir = Join-Path $package.FullName 'Descriptor'
        if (-not (Test-Path -LiteralPath $descriptorDir)) { continue }
        foreach ($descriptor in @(Get-ChildItem -LiteralPath $descriptorDir -Filter *.xml -File -ErrorAction SilentlyContinue)) {
            $descriptorFiles++
            try { [xml]$xml = Get-Content -LiteralPath $descriptor.FullName -Raw -ErrorAction Stop }
            catch { $unreadable++; continue }

            $rootElement = $xml.DocumentElement
            Add-Count $rootElements $rootElement.LocalName
            $children = @{}
            foreach ($node in $rootElement.ChildNodes) { if ($node.NodeType -eq 'Element' -and -not $children.ContainsKey($node.LocalName)) { $children[$node.LocalName] = $node.InnerText } }
            foreach ($name in $children.Keys) { Add-Count $childElements $name }

            $missing = @('Id', 'ModelModule', 'DisplayName', 'Layer' | Where-Object { -not $children.ContainsKey($_) })
            if ($missing.Count -eq 0) { $complete++ } else { $incomplete++; Add-Count $verdicts "kein Model, es fehlt: $($missing -join ', ')"; continue }

            $layerText = $children['Layer']
            $layerNumber = -1
            if ([int]::TryParse($layerText, [ref]$layerNumber) -and $layerNumber -ge 0 -and $layerNumber -lt $LayerNames.Count) { Add-Count $layers ("{0,2} {1}" -f $layerNumber, $LayerNames[$layerNumber]) }
            else { Add-Count $layers "nicht numerisch oder ausserhalb 0 bis 15 (Länge $($layerText.Length))" }

            $lockedText = if ($children.ContainsKey('Locked')) { $children['Locked'].Trim() } else { '<fehlt>' }
            Add-Count $locked $lockedText

            $isLocked = $lockedText -ieq 'true'
            $layerWritable = $layerNumber -ge 10 -and $layerNumber -le 13
            if ($dir.ReadOnlyByLocation) { Add-Count $verdicts 'schreibgeschützt, weil Referenz-Verzeichnis' }
            elseif ($isLocked) { Add-Count $verdicts 'schreibgeschützt, weil Locked' }
            elseif (-not $layerWritable) { Add-Count $verdicts 'schreibgeschützt, weil Layer ausserhalb VAR bis CUP' }
            else { Add-Count $verdicts 'beschreibbar' }

            $modelName = [IO.Path]::GetFileNameWithoutExtension($descriptor.Name)
            if ($children.ContainsKey('Name')) { if ($children['Name'] -eq $modelName) { $nameEqualsFile++ } else { $nameDiffers++ } }
            $modelDir = Join-Path $package.FullName $modelName
            if (-not (Test-Path -LiteralPath $modelDir)) { $modelDirMissing++; continue }
            $modelDirFound++

            $labelFileDir = Join-Path $modelDir 'AxLabelFile'
            $resources = Join-Path $labelFileDir 'LabelResources'
            if (Test-Path -LiteralPath $resources) {
                foreach ($languageDir in @(Get-ChildItem -LiteralPath $resources -Directory -ErrorAction SilentlyContinue)) {
                    $language = if (Test-LanguageCode $languageDir.Name) { $languageDir.Name } else { '<kein Sprachcode>' }
                    foreach ($file in @(Get-ChildItem -LiteralPath $languageDir.FullName -Filter *.label.txt -File -ErrorAction SilentlyContinue)) {
                        $labelFileCount++
                        Add-Count $labelLanguages $language
                        $parts = $file.Name -split '\.'
                        $languageInName = if ($parts.Count -ge 4) { $parts[$parts.Count - 3] } else { '' }
                        $labelFileName = ($parts[0..([Math]::Max(0, $parts.Count - 4))] -join '.')
                        if ($parts.Count -lt 4) { Add-Count $labelNaming 'weniger als vier Teile' }
                        elseif ($languageInName -ieq $languageDir.Name) { Add-Count $labelNaming '<Label-Datei>.<Sprache des Ordners>.label.txt' }
                        else { Add-Count $labelNaming 'Sprache im Namen weicht vom Ordner ab' }
                        if ($labelFileName -match '_Extension$') { $extensionNamed++ }
                        [void]$labelTxtKeys.Add(("{0}|{1}" -f ($parts[0]), $languageDir.Name).ToLowerInvariant())
                        if ($sample.Count -lt $LabelFileSample) { [void]$sample.Add($file.FullName) }
                    }
                }
            }
            foreach ($descriptionFile in @(Get-ChildItem -LiteralPath $labelFileDir -Filter *.xml -File -ErrorAction SilentlyContinue)) {
                try {
                    [xml]$labelDescription = Get-Content -LiteralPath $descriptionFile.FullName -Raw -ErrorAction Stop
                    $hasLanguage = @($labelDescription.DocumentElement.ChildNodes | Where-Object { $_.LocalName -eq 'Language' }).Count -gt 0
                    # Nur ein echter Sprachcode erscheint, sonst könnte ein Namensrest wie bei Foo_abc in die Ausgabe gelangen.
                    $suffix = if ($descriptionFile.BaseName -match '_([a-z]{2,3}(-[A-Za-z]{2,4})?)$' -and (Test-LanguageCode $Matches[1])) { $Matches[1] } else { '<ohne Sprache im Namen>' }
                    Add-Count $labelXml ("Name endet auf {0}, Element Language {1}" -f $suffix, $(if ($hasLanguage) { 'vorhanden' } else { 'fehlt' }))
                }
                catch { Add-Count $labelXml 'nicht lesbar' }
            }
        }
    }

    Out-Line "Unterordner: $($packages.Count), Descriptor-Dateien: $descriptorFiles, nicht lesbar: $unreadable"
    Out-Line "Descriptor mit Id, ModelModule, DisplayName und Layer: $complete, ohne: $incomplete"
    Out-Line "Model-Ordner <Package>\<Name der Descriptor-Datei> vorhanden: $modelDirFound, fehlt: $modelDirMissing"
    Out-Line "Element Name gleich Name der Descriptor-Datei: $nameEqualsFile, verschieden: $nameDiffers"
    Out-Counts 'Wurzelelemente der Descriptor-Dateien:' $rootElements
    Out-Counts 'Kindelemente der Descriptor-Dateien (Anzahl Dateien mit dem Element):' $childElements
    Out-Counts 'Layer:' $layers
    Out-Counts 'Locked:' $locked
    Out-Counts 'Ergebnis der Regel zum Schreibschutz:' $verdicts
    Out-Line "Label-Dateien (*.label.txt unter AxLabelFile\LabelResources\<Sprache>): $labelFileCount, Name der Label-Datei endet auf _Extension: $extensionNamed"
    Out-Counts 'Label-Dateien je Sprachordner:' $labelLanguages
    Out-Counts 'Benennung der Label-Dateien:' $labelNaming
    Out-Counts 'XML-Beschreibungen in AxLabelFile:' $labelXml

    # Stichprobe: Inhalt der Label-Dateien, ausgegeben werden nur Anzahlen
    $stats = @{}
    foreach ($path in $sample) {
        try { $bytes = [IO.File]::ReadAllBytes($path) } catch { Add-Count $stats 'Datei nicht lesbar'; continue }
        $hasBom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
        Add-Count $stats $(if ($hasBom) { 'Kodierung: UTF-8 mit BOM' } else { 'Kodierung: ohne UTF-8-BOM' })
        $offset = if ($hasBom) { 3 } else { 0 }
        $text = [Text.Encoding]::UTF8.GetString($bytes, $offset, $bytes.Length - $offset)
        $crlf = ([regex]::Matches($text, "`r`n")).Count
        $lfOnly = ([regex]::Matches($text, "(?<!`r)`n")).Count
        Add-Count $stats $(if ($crlf -gt 0 -and $lfOnly -eq 0) { 'Zeilenenden: CRLF' } elseif ($crlf -eq 0 -and $lfOnly -gt 0) { 'Zeilenenden: nur LF' } elseif ($crlf -eq 0) { 'Zeilenenden: keine' } else { 'Zeilenenden: gemischt' })
        Add-Count $stats $(if ($text.EndsWith("`n")) { 'letzte Zeile mit Zeilenende' } else { 'letzte Zeile ohne Zeilenende' })

        $ids = New-Object 'System.Collections.Generic.HashSet[string]'
        $previousWasComment = $false; $seenLabel = $false; $duplicates = 0
        $counts = @{ Labels = 0; Leer = 0; KommentarSemikolon = 0; KommentarRaute = 0; KommentarVorErstemLabel = 0; WeitereKommentarzeile = 0; OhneGleichheitszeichen = 0; TextLeer = 0; TextEinLeerzeichen = 0 }
        $keyShapes = @{}
        foreach ($line in ($text -split "`r?`n")) {
            $trimmed = $line.TrimStart()
            if ($line.Trim().Length -eq 0) { $counts.Leer++; continue }
            if ($trimmed.StartsWith(';') -or $trimmed.StartsWith('#')) {
                if ($trimmed.StartsWith(';')) { $counts.KommentarSemikolon++ } else { $counts.KommentarRaute++ }
                if (-not $seenLabel) { $counts.KommentarVorErstemLabel++ } elseif ($previousWasComment) { $counts.WeitereKommentarzeile++ }
                $previousWasComment = $true
                continue
            }
            $previousWasComment = $false
            $equals = $line.IndexOf('=')
            if ($equals -lt 0) { $counts.OhneGleichheitszeichen++; continue }
            $seenLabel = $true
            $counts.Labels++
            $key = $line.Substring(0, $equals)
            $value = $line.Substring($equals + 1)
            if ($value.Length -eq 0) { $counts.TextLeer++ } elseif ($value -eq ' ') { $counts.TextEinLeerzeichen++ }
            if (-not $ids.Add($key)) { $duplicates++ }
            if ($key -cmatch '^@[A-Za-z]+\d+$') { $shape = 'alte Form @<Buchstaben><Ziffern>' }
            elseif ($key.StartsWith('@')) { $shape = 'beginnt mit @, andere Form' }
            elseif ($key -cmatch '^L[0-9A-F]{16}$') { $shape = 'L und 16 Hex-Ziffern' }
            elseif ($key -match '^[A-Za-z0-9_]+$') { $shape = 'Buchstaben, Ziffern, Unterstrich' }
            elseif ($key -match '\s') { $shape = 'enthält Leerzeichen' }
            else { $shape = 'andere Zeichen' }
            Add-Count $keyShapes $shape
        }
        foreach ($name in $counts.Keys) { if ($counts[$name] -gt 0) { $stats["Dateien mit $name"] = 1 + [int]$stats["Dateien mit $name"]; $stats["Zeilen $name"] = $counts[$name] + [int]$stats["Zeilen $name"] } }
        if ($duplicates -gt 0) { Add-Count $stats 'Dateien mit doppelten IDs' }
        foreach ($shape in $keyShapes.Keys) { $stats["IDs: $shape"] = $keyShapes[$shape] + [int]$stats["IDs: $shape"] }
    }
    Out-Counts "Stichprobe aus $($sample.Count) Label-Dateien:" $stats

    # Kompilierte Ressourcen, rekursiv wie im bestehenden Tool
    $watch = [Diagnostics.Stopwatch]::StartNew()
    $searchErrors = $null
    $dlls = @(Get-ChildItem -LiteralPath $root -Filter *.Resources.dll -File -Recurse -ErrorAction SilentlyContinue -ErrorVariable searchErrors)
    $watch.Stop()
    $dllShapes = @{}; $dllLanguages = @{}; $withText = 0
    foreach ($dll in $dlls) {
        $segments = $dll.FullName.Substring($root.Length).TrimStart('\') -split '\\'
        $shape = @('<Package>')
        for ($i = 1; $i -lt $segments.Count - 2; $i++) { $shape += Get-SegmentShape $segments[$i] }
        $languageFolder = if ($segments.Count -ge 3) { $segments[$segments.Count - 2] } else { '' }
        if ($segments.Count -ge 3) { $shape += $(if (Test-LanguageCode $languageFolder) { '<Sprache>' } else { Get-SegmentShape $languageFolder }) }
        $shape += '<Label-Datei>.Resources.dll'
        Add-Count $dllShapes ($shape -join '\')
        Add-Count $dllLanguages $(if (Test-LanguageCode $languageFolder) { $languageFolder } else { '<kein Sprachcode>' })
        if ($labelTxtKeys.Contains(("{0}|{1}" -f ($dll.Name -split '\.')[0], $languageFolder).ToLowerInvariant())) { $withText++ }
    }
    Out-Line "Kompilierte Ressourcen: $($dlls.Count) Dateien, rekursive Suche $([int]$watch.Elapsed.TotalSeconds) s, Fehler beim Durchsuchen: $(@($searchErrors).Count)"
    Out-Line "  davon mit .label.txt zur selben Label-Datei und Sprache (wird übersprungen): $withText"
    Out-Counts 'Ablage relativ zum Package-Verzeichnis:' $dllShapes
    Out-Counts 'Sprachordner der Ressourcen:' $dllLanguages

    # Inhalt einer Stichprobe: Name der Manifest-Ressource und Form der Einträge. Die
    # Assembly wird nur zum Lesen geöffnet (ReflectionOnly), es wird kein Code ausgeführt.
    # Nur verschiedene Dateinamen, je Name die erste Datei: ReflectionOnlyLoadFrom scheitert,
    # wenn eine Assembly gleicher Identität schon aus einem anderen Pfad geladen ist, etwa
    # dieselbe Label-Datei aus einem anderen Sprachordner.
    $resourceStats = @{}
    $sampleDlls = @($dlls | Group-Object { $_.Name.ToLowerInvariant() } | ForEach-Object { $_.Group[0] } | Select-Object -First 20)
    foreach ($dll in $sampleDlls) {
        $labelFile = ($dll.Name -split '\.')[0]
        $language = $dll.Directory.Name
        try { $assembly = [Reflection.Assembly]::ReflectionOnlyLoadFrom($dll.FullName) }
        catch [System.IO.FileLoadException] { Add-Count $resourceStats 'Assembly nicht lesbar: FileLoadException (gleiche Identität schon geladen)'; continue }
        catch { Add-Count $resourceStats "Assembly nicht lesbar: $($_.Exception.GetType().Name)"; continue }
        $names = @($assembly.GetManifestResourceNames())
        Add-Count $resourceStats "Anzahl Manifest-Ressourcen: $($names.Count)"
        if ($names.Count -eq 0) { continue }
        $shape = (($names[0] -split '\.') | ForEach-Object {
            if ($_ -ieq $labelFile) { '<Label-Datei>' } elseif ($_ -ieq $language) { '<Sprache>' } elseif ($_ -ieq 'resources') { $_ } else { '<Name>' }
        }) -join '.'
        Add-Count $resourceStats "Name der ersten Ressource: $shape"
        try {
            $stream = $assembly.GetManifestResourceStream($names[0])
            $reader = New-Object System.Resources.ResourceReader($stream)
            $entries = $reader.GetEnumerator()
            $entryCount = 0
            while ($entries.MoveNext()) {
                $entryCount++
                $key = [string]$entries.Key
                if ($key -cmatch '^@[A-Za-z]+\d+$') { $keyShape = 'alte Form @<Buchstaben><Ziffern>' }
                elseif ($key -cmatch '^@[A-Za-z0-9_]+:[A-Za-z0-9_]+$') { $keyShape = 'vollständige ID @<Datei>:<Label>' }
                elseif ($key.StartsWith('@')) { $keyShape = 'beginnt mit @, andere Form' }
                elseif ($key -match '^[A-Za-z0-9_]+$') { $keyShape = 'Buchstaben, Ziffern, Unterstrich' }
                else { $keyShape = 'andere Zeichen' }
                $resourceStats["Schlüssel: $keyShape"] = 1 + [int]$resourceStats["Schlüssel: $keyShape"]
                $valueType = if ($null -eq $entries.Value) { 'null' } else { $entries.Value.GetType().Name }
                $resourceStats["Werte vom Typ $valueType"] = 1 + [int]$resourceStats["Werte vom Typ $valueType"]
            }
            $reader.Close()
            Add-Count $resourceStats 'erste Ressource mit ResourceReader lesbar'
            $resourceStats['Einträge insgesamt'] = $entryCount + [int]$resourceStats['Einträge insgesamt']
        }
        catch { Add-Count $resourceStats "erste Ressource nicht mit ResourceReader lesbar: $($_.Exception.GetType().Name)" }
    }
    Out-Counts "Inhalt von $($sampleDlls.Count) Ressourcen-Dateien mit verschiedenen Namen:" $resourceStats

    if (-not $SkipElementFiles) {
        $watch = [Diagnostics.Stopwatch]::StartNew()
        $elementFiles = 0; $elementBytes = [long]0; $reportFiles = 0; $nested = 0
        foreach ($package in $packages) {
            foreach ($modelDir in @(Get-ChildItem -LiteralPath $package.FullName -Directory -ErrorAction SilentlyContinue)) {
                foreach ($typeDir in @(Get-ChildItem -LiteralPath $modelDir.FullName -Directory -Filter 'Ax*' -ErrorAction SilentlyContinue)) {
                    if ($typeDir.Name -eq 'AxLabelFile') { continue }
                    foreach ($file in @(Get-ChildItem -LiteralPath $typeDir.FullName -Filter *.xml -File -ErrorAction SilentlyContinue)) {
                        $elementFiles++
                        $elementBytes += $file.Length
                        if ($typeDir.Name -eq 'AxReport') { $reportFiles++ }
                    }
                    $nested += @(Get-ChildItem -LiteralPath $typeDir.FullName -Directory -ErrorAction SilentlyContinue).Count
                }
            }
        }
        $watch.Stop()
        Out-Line ("Element-Dateien (*.xml in <Package>\<Model>\Ax*, ohne AxLabelFile): {0}, zusammen {1:N0} MB, davon AxReport: {2}, Unterordner in Ax*-Ordnern: {3}, Aufzählung {4} s" -f $elementFiles, ($elementBytes / 1MB), $reportFiles, $nested, [int]$watch.Elapsed.TotalSeconds)
    }
}

Out-Line ''
Out-Line '=== Ende ==='
