# BE-LabelExtension, Quellcode

Visual-Studio-Extension, mit der sich die Labels von Dynamics 365 Finance and Operations direkt in Visual Studio verwalten lassen. Sie sucht, legt an, ändert, löscht, kopiert und verschiebt Labels, findet ihre Verwendungen, extrahiert Strings aus Code und Designer in Labels und schlägt Übersetzungen vor. Im X++-Editor zeigt sie die Texte einer Label-ID im Tooltip und über der Zeile.

Dieses README richtet sich an alle, die die Extension installieren, und an die Person, die sie weiterentwickelt.

## Voraussetzungen

| Was | Version |
| --- | --- |
| Visual Studio | 2026 (18.x) oder 2022 ab 17.14, jede Edition, 64 Bit |
| .NET Framework | 4.8 |
| Dynamics 365 Developer Tools | für Tooltip, Inline-Anzeige, Sprung an Fundstellen und den Bereich des Designers. Suche und Bearbeitung brauchen nur die Package-Verzeichnisse. |
| DeepL | ein API-Schlüssel, Free oder Pro, nur für Übersetzungsvorschläge |

Die Extension funktioniert auf der Unified Developer Experience und auf der klassischen Entwicklungs-VM.

## Installieren, aktualisieren, deinstallieren

### Installieren

1. Alle Instanzen von Visual Studio schliessen.
2. Doppelklick auf `BE.LabelExtension.vsix`, die Visual-Studio-Installation wählen, *Install*, danach *Close*.
3. Visual Studio starten, *Extensions > BE-LabelExtension Window*.
4. Beim ersten Mal im Tool Window *Settings* öffnen und die Sprachen prüfen. Auf der Unified Developer Experience nimmt die Extension die zuletzt geänderte Metadaten-Konfiguration. Auf der klassischen VM schlägt die Seite das gefundene `PackagesLocalDirectory` vor, *OK* übernimmt es. Für Übersetzungsvorschläge den API-Schlüssel eintragen.

### Aktualisieren

1. Alle Instanzen von Visual Studio schliessen.
2. Doppelklick auf das neue VSIX, *Install*, *Close*.

Das neue Paket braucht eine höhere Version als das installierte, sonst zuerst deinstallieren. Einstellungen und API-Schlüssel bleiben erhalten.

### Deinstallieren

1. *Extensions > Manage Extensions*, *Installed*, *BE-LabelExtension*, *Uninstall*.
2. Visual Studio schliessen, im VSIX Installer *Modify*, danach *Close*.

Die Label-Dateien bleiben unverändert. Die Einstellungen und der verschlüsselte API-Schlüssel liegen in `%LOCALAPPDATA%\BE.LabelExtension\settings.json`. Wer sie auch entfernen will, löscht diesen Ordner. Ohne Oberfläche deinstalliert `VSIXInstaller.exe /uninstall:BE.LabelExtension.7c41e2d9` aus dem Ordner `Common7\IDE` von Visual Studio.

### Shortcuts

Im Menü *Extensions* stehen das Tool Window und die Commands, im Kontextmenü einer `.xpp`-Datei die drei Commands für den Editor.

| Command | Shortcut |
| --- | --- |
| Save Labels | Ctrl+Shift+Alt+S |
| Insert Label ID | Ctrl+Shift+Alt+I |
| Save Labels and Insert ID | Ctrl+Shift+Alt+A |
| New Label | Ctrl+Shift+Alt+N |
| Search Label (Kontextmenü einer `.xpp`-Datei) | Ctrl+Shift+Alt+L |
| Open in Label Window (Kontextmenü einer `.xpp`-Datei) | Ctrl+Shift+Alt+E |
| Extract to Label (Kontextmenü einer `.xpp`-Datei, im Designer der Button Extract im Tool Window) | Ctrl+Shift+Alt+X |

Umbelegen lassen sie sich unter *Tools > Options > Environment > Keyboard*, die Commands heissen dort `BE.LabelExtension.Commands.*`.

## Aufbau

| Projekt | Ziel | Inhalt |
| --- | --- | --- |
| `BE.LabelExtension.Core` | .NET Standard 2.0 | Kernlogik ohne Abhängigkeit zu Visual Studio |
| `BE.LabelExtension` | .NET Framework 4.8 | Extension, VisualStudio.Extensibility im Prozess von Visual Studio zusammen mit MEF und einem AsyncPackage, ein VSIX-Paket |
| `BE.LabelExtension.Tests` | .NET Framework 4.8 | Unit Tests der Kernlogik mit xUnit, auf der Runtime, auf der Visual Studio die Extension ausführt |

Gemeinsame Einstellungen stehen in `Directory.Build.props`. Der Build bricht bei jeder Warnung ab, und öffentliche Typen und Member, also Klassen, Methoden, Eigenschaften und Member-Variablen, brauchen eine XML-Dokumentation.

### Kernlogik, `BE.LabelExtension.Core`

| Ordner | Inhalt |
| --- | --- |
| `Labels` | Das Modell mit `LabelId` in beiden Formen, `Label`, `Translation` und `LabelFile`. `LabelFileFormat` liest und schreibt `.label.txt` Byte für Byte, `LabelKeyGenerator` erzeugt neue IDs. `CodeLine` und `FormatCall` lesen Strings und `strFmt` aus einer Codezeile. |
| `Sources` | Die Quellen der Label-Dateien hinter `ILabelSource`, also `LabelFileSource` für `.label.txt` und `CompiledLabelSource` für kompilierte Ressourcen, die `ResourceAssemblyReader` liest, ohne die Assembly zu laden. |
| `Models` | `ModelDiscovery` findet Package-Verzeichnisse und Models aus einer Metadaten-Konfiguration oder auf der klassischen VM. `ModelAccessRule` entscheidet, welche Models schreibgeschützt sind. |
| `Store` | `LabelStore` hält alle geladenen Labels, `LabelChanges` ändert, legt an, löscht und speichert, `LabelOperations` kopiert, verschiebt und ersetzt samt Referenzen. `LabelFileWatcher` meldet Änderungen von aussen. `LabelSettings` und `LabelSettingsFile` sind die Einstellungen. |
| `Search` | `LabelSearch` mit den Suchmodi aus `SearchMode`, je mit und ohne Beachtung der Gross- und Kleinschreibung, und der Rangfolge der Treffer. Die acht Modi des Tool Window stellt `LabelWindowController` daraus zusammen. |
| `Usages` | `LabelReferences` erkennt Verwendungen in den XML-Dateien der Elemente, im X++-Code und in Reports und ersetzt sie. `LabelUsageSearch` sucht über alle Models, `UnusedLabels` findet unbenutzte Labels. |
| `Elements` | Lesen und Schreiben in den XML-Dateien der Elemente, etwa eine einzelne Label-Eigenschaft in `ElementPropertyEditor`. |
| `TranslationServices` | `ITranslationService`, die Umsetzung für DeepL und `TranslationServiceDiscovery`, die alle Dienste findet. |
| `Display` | `LabelSummary`, der Text für Tooltip und Inline-Anzeige. |
| `Diagnostics` | `IMessageSink` für Meldungen, `ErrorBoundary` fängt Fehler an jedem Einstiegspunkt ab. |
| `Files` | Lange Pfade über 260 Zeichen, und `TextFile` liest und schreibt Dateien in ihrer Kodierung samt Byte Order Mark. |

### Extension, `BE.LabelExtension`

| Ort | Inhalt |
| --- | --- |
| `LabelExtension.cs` | Einstieg für VisualStudio.Extensibility. `InitializeServices` setzt die Dienste zusammen, im Debug-Build kommt das Untermenü mit den Probes dazu. |
| `LabelExtensionPackage.cs` | Das AsyncPackage auf Basis des VSSDK mit Optionsseite, Rückfrage beim Beenden von Visual Studio und der Auswahl im Designer. |
| `SharedServices.cs` | Reicht Dienste von VisualStudio.Extensibility an die Teile auf Basis des VSSDK und an MEF weiter. |
| `Commands` | Die Commands der Menüs, des Kontextmenüs und der Shortcuts. |
| `ToolWindow` | Das Tool Window mit Remote UI, bestehend aus `LabelToolWindowContent.xaml`, dem Datenkontext `LabelToolWindowData`, `LabelDetailData` und `LabelWindowController`, aufgeteilt nach Bereichen. |
| `Editor` | MEF-Teile für den X++-Editor, also Tooltip, Inline-Anzeige, `LabelRecognizer` für die Label-IDs und `XppDocumentListener`, der das Laden startet. |
| `Elements` | `DesignerSelection` folgt der Auswahl des Designers, `ElementNavigator` springt an Fundstellen, `UnsavedElements` erkennt ungespeicherte Elemente. |
| `Labels` | `LabelLoader` lädt im Hintergrund, wendet geänderte Einstellungen an und bietet das Neuladen an. |
| `Settings` | Optionsseite, Vorgaben und `ApiKeyProtection` für den Schlüssel mit der Data Protection API. |
| `TranslationServices` | `TranslationServiceCatalog` stellt die gefundenen Dienste bereit, im Debug-Build zusätzlich `SimulatedTranslationService`. |
| `Diagnostics`, `Threading` | Der Bereich *BE-LabelExtension* des Output Window und die Verwaltung der Hintergrundarbeit. |
| `Probes` | Probe-Commands für Diagnostics und Testing, nur im Debug-Build. |
| `.vsextension\string-resources.json` | Die Anzeigenamen der Commands. |

Ein Command ruft den `LabelWindowController` auf, der Controller arbeitet mit `LabelStore`, `LabelChanges` und `LabelOperations` aus der Kernlogik und zeigt das Ergebnis im Tool Window. Jeder Einstiegspunkt läuft durch die `ErrorBoundary`, Meldungen gehen über `IMessageSink` ins Output Window.

## Erweiterungspunkte

### Übersetzungsdienst

Ein weiterer Dienst ist eine eigene Klasse in der Kernlogik oder in der Extension. Bestehende Klassen ändern sich nicht. `TranslationServiceDiscovery` findet jede Klasse, die `ITranslationService` umsetzt, nicht abstrakt ist und einen öffentlichen Konstruktor einer dieser Formen hat:

- erster Parameter `Func<string?>`, der den API-Schlüssel liest, alle weiteren optional
- ohne Parameter

Der Name des Dienstes erscheint danach in den Optionen unter *Translation service*.

```csharp
public sealed class ExampleTranslationService : ITranslationService
{
    private readonly Func<string?> apiKey;

    public ExampleTranslationService(Func<string?> apiKey) => this.apiKey = apiKey;

    public string Name => "Example";

    public bool IsConfigured => !string.IsNullOrEmpty(this.apiKey());

    public Task<TranslationResult> TranslateAsync(string text, string sourceLanguage, IReadOnlyList<string> targetLanguages, CancellationToken cancellationToken)
    {
        // Sprachen kommen so, wie Dynamics 365 sie nennt, etwa de-CH. Probleme gehen als
        // TranslationResult.Failed("Grund") zurück, nicht als Exception.
        return Task.FromResult(TranslationResult.Failed("Not implemented yet."));
    }
}
```

Den Schlüssel liest die Funktion bei jedem Aufruf neu und entschlüsselt ihn erst dann. Der Dienst soll ihn nicht in einem Feld halten und nie in eine Meldung schreiben. `TranslationServiceDiscoveryTests` zeigt einen zweiten Dienst in der Test-Assembly.

### Quelle für Label-Dateien

Eine weitere Quelle setzt `ILabelSource` um und liefert die Label-Dateien und je Sprache ein Dokument. Probleme einzelner Dateien meldet sie an `LabelLoadRequest.Messages` und überspringt die Datei. Eingetragen wird sie in `LabelExtension.InitializeServices` in der Liste, die der `LabelStore` erhält. Store und Tool Window ändern sich nicht.

### Command

Ein Command ist eine Klasse, die von `Command` erbt und `[VisualStudioContribution]` trägt. `CommandConfiguration` legt Platz im Menü, Icon und Shortcut fest, den Anzeigenamen trägt `.vsextension\string-resources.json`. Die Dienste kommen über den Konstruktor, die Arbeit läuft durch die `ErrorBoundary`. Vorbild ist `OpenLabelWindowCommand`.

### Tool Window

Das Tool Window nutzt Remote UI. Die Extension schickt `LabelToolWindowContent.xaml` als Text an Visual Studio, und Visual Studio stellt sie selbst dar. Anders als bei WPF üblich gehört zur XAML deshalb keine Klasse mit Code, kein Code-Behind. Es gibt keine Event Handler wie `Click`, und die XAML kann keine Klassen der Extension verwenden, etwa eigene Controls. Die XAML bindet nur an den Datenkontext `LabelToolWindowData`, Buttons an dessen `AsyncCommand`s, Texte und Listen an Eigenschaften mit `DataMember`.

Für eine Änderung heisst das:

- Aussehen und Anordnung ändern sich in der XAML. Implizite Styles wirken dabei nicht in Templates hinein.
- Neues Verhalten kommt als Command oder Eigenschaft in `LabelToolWindowData` und als Methode in `LabelWindowController`, die XAML bindet daran.
- Was die XAML abhängig vom Zustand zeigt, steuert hier ein `DataTrigger` auf einer Eigenschaft des Datenkontexts, etwa `IsReadOnly` oder `IsDraft`.

Ein neuer Button braucht also eine Eigenschaft im Datenkontext, einen Eintrag in der XAML und eine Methode im Controller.

### Verwendungen

Die Muster, nach denen Verwendungen gesucht und ersetzt werden, stehen in `LabelReferences`, getrennt für Elemente, X++-Code und Reports. Die Unit Tests in `LabelReferencesTests` zeigen jedes Muster.

### Einstellungen

Eine neue Einstellung braucht eine Eigenschaft in `LabelSettings`, auch in `Clone`, ein Feld mit `DataMember` in `LabelSettingsFile.SettingsData` samt `Load` und `Save` und eine Eigenschaft auf der Optionsseite `LabelOptionsPage`. Ob eine Änderung neu laden muss, entscheidet `LabelLoader`.

## Build

Voraussetzungen für die Entwicklung:

- Windows mit Visual Studio 2026 und der Workload *Visual Studio extension development*
- .NET SDK, für `dotnet test`

Die Extension baut nur mit dem MSBuild von Visual Studio, nicht mit `dotnet build`. Restore und Build laufen als zwei getrennte Aufrufe. In einem gemeinsamen Aufruf scheitert der Build mit VSEXT0008.

```powershell
$msbuild = "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe"
& $msbuild BE.LabelExtension.sln -t:Restore
& $msbuild BE.LabelExtension.sln -t:Build -p:Configuration=Release
```

Das Paket liegt danach unter `BE.LabelExtension\bin\Release\net48\BE.LabelExtension.vsix`.

Für eine neue Version steigt die Version an zwei Stellen gleich, in `<Version>` in `Directory.Build.props` und in `Version` im Element `Identity` von `BE.LabelExtension\source.extension.vsixmanifest`.

## Testen

```powershell
dotnet test BE.LabelExtension.Tests\BE.LabelExtension.Tests.csproj
```

Die Tests laufen unter .NET Framework 4.8.

## Ohne Dynamics 365 entwickeln

Unter `BE.LabelExtension.Tests\TestData\PackagesLocalDirectory` liegt ein synthetischer Datensatz mit vier Models mit beschreibbaren, gesperrten und schreibgeschützten Layern, beiden Formen der Label-ID, Kommentaren, leeren Texten, fehlenden Übersetzungen und einer `_Extension`-Datei. Die Unit Tests arbeiten auf einer Kopie davon.

Der Debug-Build lädt statt der Metadaten-Konfiguration ein beliebiges Package-Verzeichnis, wenn die Umgebungsvariable `BELABELEXTENSION_PACKAGES_DIRECTORY` gesetzt ist. Am besten zeigt sie auf eine Kopie des Datensatzes, weil die Extension in die Label-Dateien schreibt. Die folgenden Befehle installieren den Debug-Build in die experimentelle Instanz von Visual Studio, legen die Kopie an und starten die Instanz.

```powershell
$ide = "C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE"
& "$ide\VSIXInstaller.exe" /quiet /rootSuffix:Exp BE.LabelExtension\bin\Debug\net48\BE.LabelExtension.vsix
& "$ide\devenv.exe" /rootsuffix Exp /updateconfiguration
Copy-Item BE.LabelExtension.Tests\TestData\PackagesLocalDirectory $env:TEMP\pld-demo -Recurse
$env:BELABELEXTENSION_PACKAGES_DIRECTORY = "$env:TEMP\pld-demo"
& "$ide\devenv.exe" /rootsuffix Exp
```

Ohne `/updateconfiguration` registriert die experimentelle Instanz die Menüs, Commands und das Tool Window der Extension nicht.

Tooltip und Inline-Anzeige hängen sich an den X++-Editor der Developer Tools. Ohne ihn zeigt der Debug-Build beides auch in Textdateien mit der Endung `.xpp` und erkennt die Label-IDs dort über eine Regex statt über die Classification. Eine solche Datei, etwa mit `return "@BDM1:BDM110000003";`, genügt zum Ausprobieren. Für Übersetzungsvorschläge ohne Schlüssel gibt es im Debug-Build den Dienst *Simulated (Debug build)*.

Die Label-Dateien des Datensatzes sind UTF-8 mit BOM und Windows-Zeilenenden. `.gitattributes` schützt sie vor einer Umwandlung durch Git, weil `LabelFileFormatTests` sie Byte für Byte vergleicht.

## Tools

| Skript | Zweck |
| --- | --- |
| `tools\Get-LabelEnvironmentStructure.ps1` | Liest auf einem System mit Dynamics 365 den Aufbau von Metadaten-Konfiguration, Descriptor, Label-Dateien und kompilierten Ressourcen. Gibt nur Feldnamen, Typen, Anzahlen und Pfade mit Platzhaltern aus. |
| `tools\Measure-ElementSearch.ps1` | Misst auf einem System mit Dynamics 365, wie lange eine Textsuche über die XML-Dateien der Elemente dauert, für alle Models, ab Layer VAR, ab Layer CUS und für die beschreibbaren. Gibt nur Anzahlen, Grössen und Zeiten aus. |
| `tools\Capture-ExistingToolResults.ps1` | Erfasst die Trefferlisten des bisherigen Tools BE-LabelEditor für die Begriffe in `tools\ExistingToolSearchTerms.txt` in allen acht Suchmodi. Das Ergebnis `BE.LabelExtension.Tests\TestData\ExpectedSearchResults.json` ist die Vorgabe für `ExistingToolComparisonTests`. Die Vorbereitung steht im Kopf des Skripts. |

Alle laufen unter Windows PowerShell 5.1.

## Lizenz

Der Code steht unter der MIT-Lizenz, siehe `LICENSE` im Wurzelverzeichnis. Das VSIX-Paket enthält die Datei, und der VSIX Installer zeigt sie an. Daneben enthält das Paket Bibliotheken aus NuGet unter ihren eigenen Lizenzen. Die Bibliotheken von .NET stehen unter MIT, die von VisualStudio.Extensibility unter den Lizenzbedingungen von Microsoft für Add-ons und Extensions von Visual Studio. Das SDK packt sie so in jede Extension.
