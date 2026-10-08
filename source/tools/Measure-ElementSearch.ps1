<#
.SYNOPSIS
    Misst, wie lange eine Textsuche über die XML-Dateien der Elemente dauert, als Grundlage
    für die Verwendungssuche (FA04) und ihre Beschränkung nach Layer.

.DESCRIPTION
    Das Skript liest nur. Es schreibt keine Datei und ändert nichts. Die Ausgabe enthält nur
    Anzahlen, Grössen und Zeiten, keine Namen von Models, Elementen, Benutzern oder Rechnern.

    Package-Verzeichnisse: aus der neuesten Metadaten-Konfiguration in
    %LOCALAPPDATA%\Microsoft\Dynamics365\XPPConfig (ModelStoreFolder, FrameworkDirectory,
    ReferencePackagesPaths), sonst K:\AOSService\PackagesLocalDirectory oder
    C:\AOSService\PackagesLocalDirectory, oder die mit -PackagesDirectory angegebenen.

    Element-Dateien: alle *.xml in den Ordnern Ax* eines Models, auch in Unterordnern, ohne
    AxLabelFile. So sucht auch die Extension.

    Gesucht wird ordinal nach einem Begriff, der nicht vorkommt. So wird jede Datei ganz
    gelesen, der ungünstigste Fall. Die Suche läuft in C# wie in der Extension, nicht in
    PowerShell, damit die Zeiten vergleichbar sind.

    Gemessen wird je Gruppe von Models:
    - alle Models
    - Models mit Layer ab VAR und Models mit Layer ab CUS, die Beschränkung aus der
      fachlichen Regel zur Verwendungssuche
    - die beschreibbaren Models nach der Regel zum Schreibschutz, in denen die Extension
      Referenzen umstellt
    Je Gruppe zwei Durchgänge nacheinander, der erste meist ohne Dateicache, und einer
    parallel über alle Prozessorkerne.

    Läuft unter Windows PowerShell 5.1.

.PARAMETER PackagesDirectory
    Package-Verzeichnisse statt der automatisch gefundenen, etwa auf der klassischen
    Entwicklungs-VM. Sie gelten als eigene, nicht als Referenz.

.PARAMETER Term
    Der gesuchte Begriff. Die Vorgabe kommt in keiner Datei vor.

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File .\Measure-ElementSearch.ps1 > "$env:USERPROFILE\Desktop\element-suche.txt"
#>
[CmdletBinding()]
param(
    [string[]]$PackagesDirectory = @(),
    [string]$Term = '@BELabelExtensionD1:NotThere'
)

$ErrorActionPreference = 'Stop'

Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public static class ElementSearch
{
    // All *.xml in the Ax* folders of the models, without AxLabelFile.
    public static string[] Enumerate(string[] modelDirectories, out int skippedFolders)
    {
        var files = new List<string>();
        int skipped = 0;
        foreach (string model in modelDirectories)
        {
            string[] folders;
            try { folders = Directory.GetDirectories(model, "Ax*"); }
            catch (IOException) { skipped++; continue; }
            catch (UnauthorizedAccessException) { skipped++; continue; }

            foreach (string folder in folders)
            {
                if (string.Equals(Path.GetFileName(folder), "AxLabelFile", StringComparison.OrdinalIgnoreCase)) { continue; }
                try { files.AddRange(Directory.GetFiles(folder, "*.xml", SearchOption.AllDirectories)); }
                catch (IOException) { skipped++; }
                catch (UnauthorizedAccessException) { skipped++; }
            }
        }

        skippedFolders = skipped;
        return files.ToArray();
    }

    // Returns the number of files containing the term and the number of unreadable files.
    public static int[] Search(string[] files, string term, bool parallel)
    {
        int hits = 0;
        int errors = 0;
        Action<string> scan = delegate(string file)
        {
            try
            {
                if (File.ReadAllText(file).IndexOf(term, StringComparison.Ordinal) >= 0) { Interlocked.Increment(ref hits); }
            }
            catch (IOException) { Interlocked.Increment(ref errors); }
            catch (UnauthorizedAccessException) { Interlocked.Increment(ref errors); }
        };

        if (parallel) { Parallel.ForEach(files, scan); }
        else { foreach (string file in files) { scan(file); } }
        return new[] { hits, errors };
    }

    public static long TotalLength(string[] files)
    {
        long total = 0;
        foreach (string file in files)
        {
            try { total += new FileInfo(file).Length; }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        return total;
    }
}
'@

function Out-Line([string]$Text = '') { Write-Output $Text }

# ---------------------------------------------------------------------------------------
# Package-Verzeichnisse
# ---------------------------------------------------------------------------------------
$directories = New-Object System.Collections.ArrayList   # @{ Path; IsReference }
$source = 'keine gefunden'
if ($PackagesDirectory.Count -gt 0) {
    foreach ($path in $PackagesDirectory) { [void]$directories.Add(@{ Path = $path; IsReference = $false }) }
    $source = 'Parameter PackagesDirectory'
}
else {
    $configDir = Join-Path $env:LOCALAPPDATA 'Microsoft\Dynamics365\XPPConfig'
    $newest = @(Get-ChildItem -LiteralPath $configDir -Filter *.json -File -ErrorAction SilentlyContinue | Sort-Object LastWriteTime | Select-Object -Last 1)
    if ($newest.Count -eq 1) {
        $json = Get-Content -LiteralPath $newest[0].FullName -Raw | ConvertFrom-Json
        if ($json.ModelStoreFolder) { [void]$directories.Add(@{ Path = [string]$json.ModelStoreFolder; IsReference = $false }) }
        if ($json.FrameworkDirectory) { [void]$directories.Add(@{ Path = [string]$json.FrameworkDirectory; IsReference = $true }) }
        foreach ($reference in @($json.ReferencePackagesPaths | Where-Object { $_ -is [string] })) { [void]$directories.Add(@{ Path = $reference; IsReference = $true }) }
        $source = 'neueste Metadaten-Konfiguration'
    }
    else {
        foreach ($classic in @('K:\AOSService\PackagesLocalDirectory', 'C:\AOSService\PackagesLocalDirectory')) {
            if (Test-Path -LiteralPath $classic) { [void]$directories.Add(@{ Path = $classic; IsReference = $false }); $source = 'Standardverzeichnis der klassischen Entwicklungs-VM'; break }
        }
    }
}

# Jedes Verzeichnis nur einmal
$seen = @{}
$unique = @()
foreach ($d in $directories) {
    $key = $d.Path.TrimEnd('\').ToLowerInvariant()
    if (-not $seen.ContainsKey($key) -and (Test-Path -LiteralPath $d.Path)) { $seen[$key] = $true; $unique += $d }
}

Out-Line '=== Textsuche über die Element-Dateien ==='
Out-Line "Package-Verzeichnisse: $($unique.Count) vorhanden (Quelle: $source)"
Out-Line "Logische Prozessoren: $([Environment]::ProcessorCount)"

# ---------------------------------------------------------------------------------------
# Models mit Layer und Schreibschutz, wie die Extension sie erkennt
# ---------------------------------------------------------------------------------------
$models = @()
foreach ($dir in $unique) {
    foreach ($package in @(Get-ChildItem -LiteralPath $dir.Path -Directory -ErrorAction SilentlyContinue)) {
        $descriptorDir = Join-Path $package.FullName 'Descriptor'
        if (-not (Test-Path -LiteralPath $descriptorDir)) { continue }
        foreach ($descriptor in @(Get-ChildItem -LiteralPath $descriptorDir -Filter *.xml -File -ErrorAction SilentlyContinue)) {
            try { [xml]$xml = Get-Content -LiteralPath $descriptor.FullName -Raw -ErrorAction Stop } catch { continue }
            $children = @{}
            foreach ($node in $xml.DocumentElement.ChildNodes) { if ($node.NodeType -eq 'Element' -and -not $children.ContainsKey($node.LocalName)) { $children[$node.LocalName] = $node.InnerText } }
            $layer = -1
            if (-not [int]::TryParse([string]$children['Layer'], [ref]$layer)) { continue }
            # Wie in der Extension: ein unlesbarer Wert gilt als gesperrt, ein fehlender nicht.
            $locked = $children.ContainsKey('Locked') -and ([string]$children['Locked']).Trim() -ine 'false'
            $modelDir = Join-Path $package.FullName ([IO.Path]::GetFileNameWithoutExtension($descriptor.Name))
            if (-not (Test-Path -LiteralPath $modelDir)) { continue }
            $writable = (-not $dir.IsReference) -and (-not $locked) -and $layer -ge 10 -and $layer -le 13
            $models += @{ Directory = $modelDir; Layer = $layer; Writable = $writable }
        }
    }
}
Out-Line "Models: $($models.Count), davon beschreibbar: $(@($models | Where-Object { $_.Writable }).Count)"

# ---------------------------------------------------------------------------------------
# Messung je Gruppe
# ---------------------------------------------------------------------------------------
$groups = @(
    @{ Name = 'alle Models'; Models = $models },
    @{ Name = 'Layer ab VAR'; Models = @($models | Where-Object { $_.Layer -ge 10 }) },
    @{ Name = 'Layer ab CUS'; Models = @($models | Where-Object { $_.Layer -ge 12 }) },
    @{ Name = 'beschreibbare Models'; Models = @($models | Where-Object { $_.Writable }) }
)

foreach ($group in $groups) {
    Out-Line ''
    $modelDirs = [string[]]@($group.Models | ForEach-Object { $_.Directory })
    $watch = [Diagnostics.Stopwatch]::StartNew()
    $skipped = 0
    $files = [ElementSearch]::Enumerate($modelDirs, [ref]$skipped)
    $watch.Stop()
    $megabytes = [Math]::Round([ElementSearch]::TotalLength($files) / 1MB)
    Out-Line ("Gruppe '{0}': {1} Models, {2} Dateien, {3} MB, Aufzählung {4:0.0} s, nicht lesbare Ordner: {5}" -f $group.Name, $modelDirs.Count, $files.Count, $megabytes, $watch.Elapsed.TotalSeconds, $skipped)
    if ($files.Count -eq 0) { continue }

    foreach ($run in @(@{ Name = 'Durchgang 1, nacheinander'; Parallel = $false }, @{ Name = 'Durchgang 2, nacheinander'; Parallel = $false }, @{ Name = 'Durchgang 3, parallel'; Parallel = $true })) {
        $watch = [Diagnostics.Stopwatch]::StartNew()
        $result = [ElementSearch]::Search($files, $Term, $run.Parallel)
        $watch.Stop()
        Out-Line ("  {0}: {1:0.0} s, Dateien mit Treffer: {2}, nicht lesbar: {3}" -f $run.Name, $watch.Elapsed.TotalSeconds, $result[0], $result[1])
    }
}

Out-Line ''
Out-Line '=== Ende ==='
