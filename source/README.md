# BE-LabelExtension, Quellcode

Visual-Studio-Extension, die die Labels von Dynamics 365 Finance and Operations direkt in Visual Studio verwaltet.

## Aufbau

| Projekt | Ziel | Inhalt |
| --- | --- | --- |
| `BE.LabelExtension.Core` | .NET Standard 2.0 | Kernlogik ohne Abhängigkeit zu Visual Studio |
| `BE.LabelExtension` | .NET Framework 4.8 | Extension, VisualStudio.Extensibility im Prozess von Visual Studio zusammen mit MEF, ein VSIX-Paket |
| `BE.LabelExtension.Tests` | .NET Framework 4.8 | Unit Tests der Kernlogik mit xUnit, auf der Runtime, auf der Visual Studio die Extension ausführt |

Gemeinsame Einstellungen stehen in `Directory.Build.props`. Der Build bricht bei jeder Warnung ab, und öffentliche Member brauchen eine XML-Dokumentation.

## Voraussetzungen

- Windows mit Visual Studio 2026 und der Workload *Visual Studio extension development*
- .NET SDK, für `dotnet test`

## Bauen

Die Extension baut nur mit dem MSBuild von Visual Studio, nicht mit `dotnet build`. Restore und Build laufen als zwei getrennte Aufrufe. In einem gemeinsamen Aufruf scheitert der Build mit VSEXT0008.

```powershell
$msbuild = "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe"
& $msbuild BE.LabelExtension.sln -t:Restore
& $msbuild BE.LabelExtension.sln -t:Build -p:Configuration=Release
```

Das Paket liegt danach unter `BE.LabelExtension\bin\Release\net48\BE.LabelExtension.vsix`.

## Testen

```powershell
dotnet test BE.LabelExtension.Tests\BE.LabelExtension.Tests.csproj
```

Die Tests laufen unter .NET Framework 4.8.

## In der experimentellen Instanz ausprobieren

```powershell
$ide = "C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE"
& "$ide\VSIXInstaller.exe" /quiet /rootSuffix:Exp BE.LabelExtension\bin\Debug\net48\BE.LabelExtension.vsix
& "$ide\devenv.exe" /rootsuffix Exp /updateconfiguration
& "$ide\devenv.exe" /rootsuffix Exp
```

Ohne `/updateconfiguration` registriert Visual Studio die Menüs, Commands und das Tool Window der Extension nicht, auch nach einer Installation per Doppelklick. Danach erscheint das Menü *Extensions > BE-LabelExtension*.

Der Debug-Build enthält dort zusätzlich Probe-Commands für den ersten Durchgang auf der Testumgebung. Sie lesen nur und schreiben ihr Ergebnis in den Bereich *BE-LabelExtension* des Output Window.

## Ohne Dynamics 365 entwickeln

Unter `BE.LabelExtension.Tests\TestData\PackagesLocalDirectory` liegt ein synthetischer Datensatz: vier Models mit beschreibbaren, gesperrten und schreibgeschützten Layern, beide Formen der Label-ID, Kommentare, leere Texte, fehlende Übersetzungen und eine `_Extension`-Datei. Die Unit Tests arbeiten auf einer Kopie davon.

Der Debug-Build lädt statt der Metadaten-Konfiguration ein beliebiges Package-Verzeichnis, wenn die Umgebungsvariable `BELABELEXTENSION_PACKAGES_DIRECTORY` gesetzt ist. Am besten zeigt sie auf eine Kopie des Datensatzes, weil die Extension in die Label-Dateien schreibt:

```powershell
Copy-Item BE.LabelExtension.Tests\TestData\PackagesLocalDirectory $env:TEMP\pld-demo -Recurse
$env:BELABELEXTENSION_PACKAGES_DIRECTORY = "$env:TEMP\pld-demo"
& "$ide\devenv.exe" /rootsuffix Exp
```

Die Label-Dateien des Datensatzes sind UTF-8 mit BOM und Windows-Zeilenenden. `.gitattributes` schützt sie vor einer Umwandlung durch Git, weil TC01 sie Byte für Byte vergleicht.

## Werkzeuge

| Skript | Zweck |
| --- | --- |
| `tools\Get-LabelEnvironmentStructure.ps1` | Liest auf der Testumgebung den Aufbau von Metadaten-Konfiguration, Descriptor, Label-Dateien und kompilierten Ressourcen. Gibt nur Feldnamen, Typen, Anzahlen und Pfade mit Platzhaltern aus. |
| `tools\Capture-ExistingToolResults.ps1` | Erfasst die Trefferlisten des bestehenden BE-LabelEditor für die Begriffe in `tools\ExistingToolSearchTerms.txt` in allen acht Suchmodi. Das Ergebnis `BE.LabelExtension.Tests\TestData\ExpectedSearchResults.json` ist die Vorgabe für TC04. Die Vorbereitung steht im Kopf des Skripts. |

Beide laufen unter Windows PowerShell 5.1.
