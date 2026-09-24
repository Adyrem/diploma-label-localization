#import "../helpers.typ": todo

= Anhang

== Aufgabenstellung / Themeneingabe

== Controlling-Berichte

== Detailplanung und Aufwanderfassung

Die Tabelle führt die tagesgenaue Soll-Planung und den effektiven Verlauf zusammen.
Sie wird während des Projekts laufend nachgeführt und bildet die Grundlage für den
Soll/Ist-Vergleich in der Reflexion. Der Soll-Aufwand beträgt pro Arbeitstag acht
Stunden.

#[
  #show figure: set align(left)
  #set text(size: 9pt)
  #figure(
    table(
      align: left,
      columns: (auto, auto, 1.4fr, 2.4em, 2.4em, 1.4fr, auto),
      table.header(
        [*Nr.*], [*Datum*], [*Tätigkeit (Soll)*], [*Soll\ h*], [*Ist\ h*],
        [*Tätigkeit (Ist) und Abweichung*], [*Status*],
      ),
      table.cell(colspan: 7)[*AP1 Projektinitialisierung*],
      [1.1], [04.09.], [Setup Toolchain, Dokumentationsgerüst, Terminplanung], [8], [4], [Richtlinien ausgewertet, Typst-Gerüst und Repository aufgesetzt, Soll-Terminplan erstellt], [Erledigt],
      [1.2], [10.09.], [Ausgangslage, Situationsanalyse, Aufgabenstellung], [8], [4], [Kapitel geschrieben, IST-Zustand mit sechs Abbildungen belegt. Titel und Klasse ergänzt, Rahmenbedingungen und Konfigurationsmanagement vorgezogen], [Erledigt],
      [1.3], [11.09.], [Zieldefinition, Abgrenzung, Stakeholder-Analyse], [8], [3], [Ziele mit Messkriterien, Abgrenzung und Stakeholder-Analyse geschrieben. Auf den 10.09. vorgezogen], [Erledigt],
      [1.4], [12.09.], [Rahmenbedingungen, grobe Anforderungen], [8], [5], [Grobe Anforderungen FA, NFA und OA mit Zielbezug erfasst, produktbezogene Rahmenbedingungen ausgebaut, Model-Begriff eingeführt. Auf den 10.09. vorgezogen], [Erledigt],
      [1.5], [24.09.], [Varianten, Machbarkeit, Variantenentscheid], [8], [16], [Sechs Variantenvergleiche, Machbarkeitsbeurteilung mit Prototypen-Spike und Laufzeitprobe auf der Entwicklungsumgebung, alle Varianten entschieden, Befundprotokoll als Anhang. Auf den 14.09. vorgezogen, doppelter Aufwand wegen des Spikes], [Erledigt],
      [1.6], [25.09.], [Risikoanalyse, Qualitäts- und Konfigurationsmanagement], [8], [6], [Neun Risiken mit Matrix und Bewertungsschema, Qualitätsmanagement mit sieben Massnahmen. Konfigurationsmanagement bereits in AP1.2 erledigt. Englische Fachbegriffe im Kapitel vereinheitlicht. Auf den 24.09. vorgezogen], [Erledigt],
      [1.7], [26.09.], [Review Initialisierung], [8], [], [], [],
      table.cell(colspan: 7)[*AP2 Konzept*],
      [2.1], [01.10.], [Kontextdiagramm, Geschäftsprozessanalyse], [8], [], [], [],
      [2.2], [02.10.], [Detailanforderungen], [8], [], [], [],
      [2.3], [08.10.], [Use Cases, Sequenzdiagramme], [8], [], [], [],
      [2.4], [09.10.], [Klassenmodell, Datenmodell, Systemarchitektur], [8], [], [], [],
      [2.5], [10.10.], [Testkonzept, GUI-Design, Review Konzept], [8], [], [], [],
      table.cell(colspan: 7)[*AP3 Realisierung*],
      [3.1], [15.10.], [Setup Projektstruktur, Build- und Test-Toolchain], [8], [], [], [],
      [3.2], [16.10.], [Datenmodell und Persistenz], [8], [], [], [],
      [3.3], [17.10.], [Business Logic Teil 1], [8], [], [], [],
      [3.4], [19.10.], [Business Logic Teil 2], [8], [], [], [],
      [3.5], [20.10.], [GUI implementieren], [8], [], [], [],
      [3.6], [21.10.], [Integration und externe Anbindung], [8], [], [], [],
      table.cell(colspan: 7)[*AP4 Tests und Abschluss Realisierung*],
      [4.1], [22.10.], [Restarbeiten und Bugfixing], [8], [], [], [],
      [4.2], [23.10.], [Unit- und Integration-Tests, Testprotokoll], [8], [], [], [],
      [4.3], [24.10.], [Code Freeze, Deployment], [8], [], [], [],
      table.cell(colspan: 7)[*AP5 Dokumentation finalisieren*],
      [5.1], [29.10.], [Kapitel Realisierung und Testprotokoll], [8], [], [], [],
      [5.2], [30.10.], [Management Summary, Reflexion, Schlusswort], [8], [], [], [],
      [5.3], [31.10.], [Gesamtreview, Verzeichnisse, Korrektorat, PDF], [8], [], [], [],
      table.cell(colspan: 7)[*AP6 Präsentation vorbereiten*],
      [6.1], [05.11.], [Storyline und Foliengerüst], [8], [], [], [],
      [6.2], [06.11.], [Folien ausarbeiten], [8], [], [], [],
      [6.3], [07.11.], [Onlinepublikation], [8], [], [], [],
      [6.4], [12.11.], [Generalprobe], [8], [], [], [],
    ),
    caption: [Detailplanung und Aufwanderfassung Soll/Ist]
  ) <detailplanung>
]

== Befundprotokoll der Machbarkeitsstudie <befundprotokoll>

Das folgende Protokoll dokumentiert den Versuchsaufbau zum Extension-Modell und zu
den technischen Fragen aus @variantenentscheid. Es wurde von einem KI-System
erstellt und ist inhaltlich unverändert übernommen, angepasst wurden nur
Rechtschreibung und Gliederung.

Erstellt am 2026-09-12 und ergänzt am 2026-09-14 durch ein KI-System (Claude,
Anthropic) im Auftrag von Adrian Aeschlimann. Alle Prototypen, Messungen und
Meldungen stammen aus tatsächlich durchgeführten Vorgängen auf den unten
genannten Geräten. Nutzerspezifische Pfade sind durch Umgebungsvariablen ersetzt,
Projekt-, Modell-, Element- und Labelbezeichnungen aus der Entwicklungsumgebung
durch Platzhalter in spitzen Klammern. Die Analyse wurde am 2026-09-14 abgeschlossen;
die in Abschnitt 12 genannten offenen Punkte werden nicht weiter untersucht.


=== Zur Belegkraft dieses Protokolls


Das Protokoll stützt sich auf vier Arten von Belegen, die unterschiedlich weit
tragen. Jeder Abschnitt nennt, welche Art gilt.

Kompilieren und Paketieren (P1, P2, P3, P5). Positive Aussagen bedeuten: der
Compiler akzeptiert die Registrierung und es entsteht eine installierbare
VSIX-Datei. Negative Aussagen stützen sich auf die Schnittstellen des SDK und
auf fehlgeschlagene Kompilierversuche, deren Meldungen im Wortlaut
wiedergegeben sind.

Statische Auswertung der D365-Werkzeuge (Abschnitt 7). Aus den Assemblies der
Dynamics-365-Erweiterung wurden Typen, Signaturen und MEF-Attribute ausgelesen.
Die D365-Werkzeuge selbst wurden dabei nicht ausgeführt. Belegt ist, was dort
registriert ist, nicht wie es sich zur Laufzeit verhält.

Ausführung auf dem privaten Gerät (Abschnitte 8, 9.2 und 9.3). Der Prototyp P4
wurde gegen die Metadata-API tatsächlich ausgeführt. Die Laufzeitprobe P5 wurde
in einer laufenden Instanz von Visual Studio 2026 geladen, dort aber ohne
D365-Werkzeuge.

Laufzeitbeobachtung auf der D365-Entwicklungsumgebung (Abschnitt 9.4). Die Probe
P5 lief in Visual Studio 2026 mit geladenen D365-Werkzeugen. Grundlage sind ihre
Protokolle aus zwei Sitzungen und drei Bildschirmfotos. Belegt ist, was dabei
tatsächlich geschah; wo aus dem Fehlen eines Ereignisses geschlossen wird, ist
das ausdrücklich vermerkt.


=== Umgebung



==== Privates Entwicklungsgerät


Betriebssystem: Windows 11 Home N, Version 10.0.26200
Visual Studio: Community 2026, Version 18.9.2, Build 18.9.12120.119,
Produktlinie Dev18, Kanal VisualStudio.18.Release
Installierte Workload: Microsoft.VisualStudio.Workload.VisualStudioExtension
Installierte Komponente: Microsoft.VisualStudio.Component.VSSDK
Weitere Komponente: Microsoft.VisualStudio.Extensibility.Diagnostics
.NET SDK: 10.0.400
Laufzeiten: Microsoft.NETCore.App 8.0.30 und 10.0.11
MSBuild: aus der Visual-Studio-Installation, MSBuild/Current/Bin/MSBuild.exe


==== D365-Entwicklungsumgebung (erhoben am 2026-09-14)


Die Angaben stammen aus einem Sammelskript, das auf der Umgebung nur gelesen und
kopiert hat (Abschnitt 3.3).

Visual Studio Professional 2026, Version 18.5.11709.299
Visual Studio Professional 2022, Version 17.14.36623.8
D365-Erweiterung: "Finance and Operations (Dynamics 365)",
Identität DynamicsFnO.DeveloperTools, Version 7.0.7778.118 (Platform Update 70),
in beiden Visual-Studio-Installationen im gleichen Erweiterungsordner vorhanden
Aufbau: Unified Developer Experience. PackagesLocalDirectory liegt unter
%LOCALAPPDATA%\\Microsoft\\Dynamics365\\\<Version\>\\PackagesLocalDirectory, und zwar
in drei Versionen: 10.0.2428.95, 10.0.2428.188 und 10.0.2527.197
Laufzeitprobe: ausgeführt in Visual Studio Professional 2026, 18.5.11709.299


=== Paketversionen (NuGet, Stand 2026-09-12)


Microsoft.VSSDK.BuildTools ................ höchste Version 18.9.820
Microsoft.VisualStudio.SDK ................ höchste Version 17.14.40265
Microsoft.VisualStudio.Extensibility.Sdk .. höchste Version 17.14.40608

Nur die Build-Werkzeuge tragen eine 18er-Version. Weder das klassische
SDK-Metapaket noch das SDK des neuen Modells besitzen überhaupt eine Version
mit Hauptnummer 18; eine gezielte Abfrage nach Versionen, die mit "18." beginnen,
liefert für beide Pakete null Treffer, für die Build-Werkzeuge dagegen mehrere
(18.4.33 bis 18.9.820).

Gegenprobe an der mit VS2026 ausgelieferten Projektvorlage: Die Datei
VSIXProject.csproj der Vorlage "VSIX Project" referenziert
Microsoft.VisualStudio.SDK in Version 17.14.40265 mit ExcludeAssets="runtime"
und Microsoft.VSSDK.BuildTools in Version 18.9.820, bei Zielframework net472 und
LangVersion 14. Die Mischung aus 17.14-Referenzen und 18.9-Buildwerkzeugen ist
also der von Microsoft selbst vorgesehene Zustand.

Werkzeuglage: Die in dieser Installation enthaltenen Extensibility-Vorlagen sind
ausschliesslich klassische VSIX-Vorlagen, nämlich EmptyVSIXProject, VSIXProject,
ExtensionPackProjectTemplate, ItemTemplate und ProjectTemplate. Eine Vorlage für
das neue Modell VisualStudio.Extensibility ist nicht enthalten. Projekte für das
neue Modell lassen sich dennoch von Hand anlegen, was hier geschehen ist. Auch
über "dotnet new" stehen keine Vorlagen für Visual-Studio-Erweiterungen bereit.


=== Prototypen und Werkzeuge



==== Die drei Modell-Prototypen


Alle drei wurden nach vollständigem Löschen der Verzeichnisse bin und obj neu
kompiliert und liefern reproduzierbar eine VSIX-Datei ohne Fehler.

P1  Klassisch VSSDK, gleicher Prozess
Zielframework net472, Pakete Microsoft.VisualStudio.SDK 17.14.40265 und
Microsoft.VSSDK.BuildTools 18.9.820
Inhalt: MEF-Editor-Erweiterungen (siehe 4.1)
VSIX-Grösse 16'485 Byte, Inhalt: extension.vsixmanifest, P1.dll,
[Content\_Types].xml, manifest.json, catalog.json

P2  VisualStudio.Extensibility, eigener Prozess
Zielframework net8.0, Pakete Microsoft.VisualStudio.Extensibility.Sdk und
Microsoft.VisualStudio.Extensibility.Build, je 17.14.40608
Inhalt: Kommando im Werkzeugmenü, CodeLens über Tagger
VSIX-Grösse 11'530'427 Byte

P3  VisualStudio.Extensibility, gleicher Prozess
Zielframework net472, Pakete des neuen Modells zusammen mit
Microsoft.VisualStudio.SDK 17.14.40265 und Microsoft.VSSDK.BuildTools 18.9.820
Inhalt: Kommando des neuen Modells plus unveränderter MEF-Quelltext aus P1
VSIX-Grösse 3'328'061 Byte

Der Grössenunterschied rührt daher, dass das neue Modell seine Laufzeit
mitliefert. Im Paket von P2 finden sich unter anderem
Microsoft.VisualStudio.Extensibility.dll, .Contracts.dll, .Framework.dll,
Microsoft.VisualStudio.Composition.dll und Microsoft.VisualStudio.CoreUtility.dll.
Die klassische Erweiterung P1 referenziert dagegen nur gegen die im Produkt
bereits vorhandenen Assemblies und liefert ausser der eigenen DLL nichts mit.


==== Die Prototypen der zweiten Runde (2026-09-14)


P4  Metadata-API gegen direkten Dateizugriff (Frage 5)
Konsolenprogramm, Zielframework net48, referenziert die Metadata-Assemblies
aus der D365-Umgebung. Wird ausgeführt, siehe Abschnitt 8.

P5  Laufzeitprobe (Fragen 1, 2, 3 und FA05)
Klassische VSIX mit AsyncPackage und MEF-Teilen, Zielframework net48,
Installationsbereich [17.14, 19.0). Schreibt ein Protokoll und verändert
nichts. Siehe Abschnitt 9.
Version 1.0 (35'288 Byte): Grundfassung, ausgeführt am 2026-09-14.
Version 1.1 (40'408 Byte): liest zusätzlich das Objekt hinter ModelElement
aus (Abschnitt 9.1), ausgeführt am 2026-09-14 (Abschnitt 9.5).


==== Werkzeuge


apiscan. Ein eigenes Kommandozeilenwerkzeug auf Basis von MetadataLoadContext.
Es liest Assemblies, ohne sie auszuführen, und listet Typen, Member,
Basisschnittstellen, alle MEF-Exporte mit ihren Attributen sowie MEF-Importe.
Damit wurden die Abschnitte 4.2 und 7 ermittelt.

Sammelskript für die D365-Umgebung. Ein PowerShell-Skript, das auf der Umgebung
nur liest und in einen neuen Ordner kopiert: Visual-Studio-Instanzen, Manifest,
pkgdef-Dateien, Assemblies und Vorlagen der D365-Erweiterung, eine vollständige
Dateiliste davon, sowie die Metadata-Assemblies samt ihrer statischen
Abhängigkeiten aus PackagesLocalDirectory\\bin. Es wurde vorab gegen einen
nachgebildeten Aufbau getestet; dieser Test fand zwei Fehler, die das Skript auf
der Umgebung zum Absturz gebracht hätten. Beim echten Lauf blieben drei Lücken,
die in Abschnitt 12 aufgeführt sind.


=== Welche Schnittstellen kompilieren, welche nicht


Belegart: Kompilieren und Paketieren.


==== P1, klassisch VSSDK, gleicher Prozess


Kompilieren fehlerfrei, alle vier über MEF exportiert und mit ContentType
gefiltert:

- IWpfTextViewCreationListener, mit [Export], [ContentType] und [TextViewRole]
- AdornmentLayerDefinition, also eine eigene WPF-Einblendungsschicht, zusammen mit IAdornmentLayer.AddAdornment und AdornmentPositioningBehavior.TextRelative
- IAsyncQuickInfoSourceProvider mit IAsyncQuickInfoSource, also Tooltip beim Ueberfahren von Text
- IAsyncCodeLensDataPointProvider mit IAsyncCodeLensDataPoint

Damit ist belegt, dass im klassischen Modell alle vier für die Projektziele
benötigten Editor-Mechanismen registrierbar sind.


==== P2, eigener Prozess


Nicht vorhanden. Derselbe MEF-Quelltext, der unter P1 fehlerfrei kompiliert,
scheitert hier. Wortlaut der Meldungen:

```
error CS0246: The type or namespace name 'IWpfTextViewCreationListener' could
not be found (are you missing a using directive or an assembly reference?)

error CS0246: The type or namespace name 'TextViewRoleAttribute' could not be
found (are you missing a using directive or an assembly reference?)

error CS0246: The type or namespace name 'IWpfTextView' could not be found
(are you missing a using directive or an assembly reference?)

error CS0103: The name 'PredefinedTextViewRoles' does not exist in the current
context
```


Das ist kein Referenz- oder Berechtigungsproblem: Die Typen existieren in der
Oberfläche dieses Modells nicht.

Vorhanden sind stattdessen, ermittelt durch Auslesen der Typen aus den
Assemblies des gebauten Pakets:

- ITextViewOpenClosedListener
- ITextViewChangedListener
- ITextViewExtension
- ITextViewMarginProvider, mit CreateVisualElementAsync(ITextViewSnapshot, ...)
- ITextViewTaggerProvider\<T\>, mit CreateTaggerAsync(ITextViewSnapshot, ...)

Verfügbare Tag-Arten: ClassificationTag, TextMarkerTag und CodeLensTag.

Daraus folgt:

CodeLens ist im eigenen Prozess erreichbar, nämlich über CodeLensTag am
Tagger-Mechanismus. Ein Prototyp, der eine Ableitung von TextViewTagger\<CodeLensTag\>
registriert und in RequestTagsAsync Tags vom Typ
TaggedTrackingTextRange\<CodeLensTag\> liefert, kompiliert fehlerfrei. MEF ist
dafür nicht erforderlich.

QuickInfo ist im eigenen Prozess nicht erreichbar. Eine Suche über alle 51
Assemblies des gebauten Pakets ergibt keinen einzigen Typ, dessen Name QuickInfo
enthält.

Zur Präzision bei Tooltips: Eine Tooltip-Eigenschaft gibt es sehr wohl, nämlich
CodeLensLabel.Tooltip beziehungsweise CodeLensLabelContract.Tooltip. Sie hängt
jedoch an einem CodeLens-Label. TextMarkerTag trägt keinen Tooltip, sondern nur
eine Markierungsart vom Typ TextMarkerType. Präzise formuliert: Im eigenen
Prozess gibt es keinen Tooltip beim Überfahren eines Tokens im Editortext; der
einzige verfügbare Tooltip verlangt vom Benutzer, die CodeLens-Zeile zu
überfahren.

Was überhaupt am Text selbst ansetzen kann, ist damit abschliessend: zwei
Tag-Arten ohne eigenen Text (ClassificationTag färbt ein, TextMarkerTag
markiert), CodeLens oberhalb der Zeile und die Marginalspalte am Rand. Keines
davon zeigt beim Überfahren eines Tokens Text an.


==== P3, gleicher Prozess, neues Modell


Der MEF-Quelltext aus P1, also Einblendungsschicht und QuickInfo, wurde
unverändert übernommen und kompiliert dort zusammen mit einem Kommando des
neuen Modells fehlerfrei. Beide Welten lassen sich in einer einzigen Assembly
verbinden.

Die erzeugte VSIX-Datei trägt folgerichtig beides: einen Asset-Eintrag

```
<Asset Type="Microsoft.VisualStudio.MefComponent" Path="P3.dll" />
```


und daneben die Dateien .vsextension/extension.json und
.vsextension/string-resources.json des neuen Modells.


=== Die beiden Schalter für den Betrieb im gleichen Prozess


Der Betrieb im gleichen Prozess verlangt zwei Schalter, die zwingend gemeinsam
gesetzt sein müssen. Der Compiler erzwingt die Paarung in beide Richtungen.

Fehlt RequiresInProcessHosting in der Extension-Klasse:

```
error VSEXT0007: Extension projects with
'<VssdkCompatibleExtension>true</VssdkCompatibleExtension>' must have
RequiresInProcessHosting set to true in their Extension class configuration.
```


Fehlt VssdkCompatibleExtension in der Projektdatei:

```
error VSEXT0008: When configuring an extension to run in the devenv.exe process
by enabling RequiresInProcessHosting, '<VssdkCompatibleExtension>true</VssdkCompatibleExtension>'
must be set in the extension's project file.
```


Sobald RequiresInProcessHosting gesetzt ist, muss die Metadata-Eigenschaft des
neuen Modells leer bleiben:

```
error CEE0028: An issue was encountered when evaluating the compile-time
constant P3.P3Extension.ExtensionConfiguration. Invalid object initializer
values: The 'Metadata' property must be null when the 'RequiresInProcessHosting'
property is true, it must have a value otherwise.

error VSEXT0004: VisualStudio.Extensibility projects must contribute one class
extending Microsoft.VisualStudio.Extensibility.Extension
```


Die Erweiterung bezieht ihre Identität dann aus einer klassischen
source.extension.vsixmanifest. Die Variante im gleichen Prozess ist damit
strukturell ein Zwitter aus beiden Modellen und kein reiner Vertreter des neuen.

Microsofts eigene D365-Erweiterung ist genau so aufgebaut: ihr Manifest
deklariert ExtensionType="VSSDK+VisualStudio.Extensibility" (Abschnitt 7.1).


=== Vorschau-Schleusen im neuen Modell


Tagger und CodeLens sind ausdrücklich als Vorschau gekennzeichnet. Der Compiler
verweigert die Kompilierung, Wortlaut am Beispiel CodeLensTag:

```
error VSEXTPREVIEW_TAGGERS: 'Microsoft.VisualStudio.Extensibility.Editor.CodeLensTag'
is for evaluation purposes only and is subject to change or removal in future
updates. Suppress this diagnostic to proceed.

error VSEXTPREVIEW_CODELENS: 'Microsoft.VisualStudio.Extensibility.Editor.CodeElementKind'
is for evaluation purposes only and is subject to change or removal in future
updates. Suppress this diagnostic to proceed.
```


Die Meldungen lassen sich über die NoWarn-Eigenschaft unterdrücken, der
Prototyp tut das auch. Es bleibt eine bewusst eingegangene Abhängigkeit von
einer als instabil deklarierten Schnittstelle.

In dieser SDK-Fassung finden sich insgesamt sieben solcher Schleusen:

```
VSEXTPREVIEW_CODELENS
VSEXTPREVIEW_TAGGERS
VSEXTPREVIEW_LSP
VSEXTPREVIEW_OUTPUTWINDOW
VSEXTPREVIEW_SETTINGS
VSEXTPREVIEW_PROJECTQUERY_TRACKING
VSEXTPREVIEW_DEBUGGERVISUALIZERS_EXPRESSION
```



=== D365-Werkzeuge, statisch ausgewertet (Fragen 1 bis 3)


Belegart: statische Auswertung. Grundlage sind 45 Assemblies, das Manifest, drei
pkgdef-Dateien und die Projektvorlagen der D365-Erweiterung aus Abschnitt 1.2.


==== Manifest und Installationslage


Das Manifest deklariert als Installationsziel und als Voraussetzung jeweils den
Bereich [17.0, 18.0), also ausschliesslich Visual Studio 2022. Trotzdem liegt
dieselbe Erweiterung auf der Umgebung auch im Erweiterungsordner von Visual
Studio 2026. Aus den Dateien allein liess sich nicht ablesen, ob sie dort
funktioniert; zur Laufzeit zeigte sich, dass sie läuft (Abschnitt 9.4).

Die Erweiterung deklariert ExtensionType="VSSDK+VisualStudio.Extensibility" und
enthält eine Datei .vsextension\\extension.json. Microsoft verwendet für die
eigenen D365-Werkzeuge also dieselbe Mischform wie P3.

Als MEF-Komponenten meldet das Manifest vier Assemblies an Visual Studio:
Microsoft.Dynamics.Framework.Tools.LanguageService.17.0.dll, .GitHubCopilot.17.0.dll,
.SemanticSearch.17.0.dll und Microsoft.Dynamics.TestTools.17.0.TestAdapter.dll.


==== Frage 1: Content Type des X++-Editors


X++ ist ein gewöhnlich registrierter Content Type und nicht abgeschottet. Die
Registrierung in der Sprachdienst-Assembly lautet:

```
[Name("X++")]  [BaseDefinition("code")]  [BaseDefinition("code-languageserver-preview")]
[FileExtension(".xpp")]  [ContentType("X++")]
```


Die zweite Basisdefinition zeigt, dass X++ mindestens teilweise über den
Language-Server-Client von Visual Studio läuft.

Die D365-Werkzeuge hängen ihre eigenen Editorfunktionen über genau die
Verträge an, die auch P1 und P3 verwenden, jeweils mit [ContentType("X++")]:
IAsyncQuickInfoSourceProvider (Name "X++ QuickInfo Source"),
IWpfTextViewCreationListener (zweimal, mit TextViewRole "DOCUMENT"),
IViewTaggerProvider (viermal), ITaggerProvider, IClassifierProvider,
ICompletionSourceProvider, ISignatureHelpSourceProvider,
IIntellisenseControllerProvider, IMouseProcessorProvider, ISmartIndentProvider,
ITextStructureNavigatorProvider und IVsTextViewCreationListener.

Für FA05 und FA06 bedeutsam: Der D365-Klassifizierer kennzeichnet Label-Token
bereits selbst. Unter den 13 Klassifizierungstypen finden sich
"X++ Modern Label", "X++ Legacy Label" und "X++ Temporary Label". Eine
Erweiterung könnte Label-IDs damit über die vorhandene Klassifizierung finden,
statt X++ selbst zu parsen. Zur Laufzeit hat sich das bestätigt (Abschnitt 9.4).


==== Frage 2: CodeLens auf X++


Die D365-Werkzeuge verwenden für X++ nicht die CodeLens-Infrastruktur von
Visual Studio, sondern eine eigene Nachbildung:

- CodeLensAdornmentFactory, ein IWpfTextViewCreationListener für "X++"
- CodeLensAdornmentTagProvider, ein IViewTaggerProvider mit den Tag-Arten SpaceNegotiatingAdornmentTag und einem eigenen CodeLensAdornmentTag
- eine eigene Einblendungsschicht mit dem Namen "CodeLensAdornment"
- eine eigene Editoroption "XppCodeLensOptionsKey"

Daraus folgt zweierlei. Ob ein IAsyncCodeLensDataPointProvider von Visual Studio
überhaupt ein X++-Element angeboten bekommt, ist zweifelhaft; zur Laufzeit wurde
kein einziger solcher Aufruf protokolliert (Abschnitt 9.4). Und die von
D365 verwendete Technik, Platz oberhalb einer Zeile über
SpaceNegotiatingAdornmentTag zu reservieren und darin zu zeichnen, beruht auf
öffentlichen Typen des Editors und steht damit auch einer eigenen Erweiterung
im gleichen Prozess offen.


==== Frage 3: Eigenschaftsfenster und Add-in-Modell


Die öffentliche Oberfläche des Add-in-Modells in
Microsoft.Dynamics.Framework.Tools.Extensibility.17.0.dll umfasst 15 Typen:
AddinDesignerEventArgs, AddinEventArgs, AddinsEnvironmentHelper,
DesignerMenuBase, DesignerMenuExportMetadataAttribute, IAddin,
IAutomationObjectService, IDesignerMenu, IDesignerMenuMetadata,
IDynamicsProjectService, IMainMenu, IMenu, IMetaModelProviders, MainMenuBase
und MenuBase.

Erweiterungspunkte sind genau zwei: IMainMenu für das Menü und IDesignerMenu
für das Kontextmenü im Element-Designer, gefiltert über
DesignerMenuExportMetadata mit AutomationNodeType und CanSelectMultiple. Beim
Klick liefert AddinDesignerEventArgs das gewählte Element, die gewählten
Elemente und das übergeordnete Element. Einen Erweiterungspunkt für das
Eigenschaftsfenster enthält die öffentliche Oberfläche nicht. Was das
Eigenschaftsfenster zur Laufzeit erhält, steht in Abschnitt 9.4.

Ein Add-in ist keine VSIX-Datei. Die mitgelieferte Projektvorlage erzeugt eine
gewöhnliche Klassenbibliothek, bindet
Microsoft.Dynamics.Framework.Tools.Extensibility.17.0.targets ein und kopiert
die Ausgabe nach dem Bau in den Ordner \$(DynamicsVSToolsHintPath)\\AddinExtensions,
also in den Installationsordner der D365-Erweiterung. Geladen wird das Add-in
damit von den D365-Werkzeugen und nicht von der Erweiterungsverwaltung von
Visual Studio.

Zum Dienst IMetaModelProviders, der den aktuellen IMetadataProvider und einen
Querverweis-Provider liefert: Exportiert wird er von
Microsoft.Dynamics.Framework.Tools.MetaModel.Core.17.0.dll. Diese Assembly gehört
nicht zu den vier MEF-Komponenten, die das Manifest an Visual Studio meldet, und
keine der 45 Assemblies importiert den Dienst deklarativ. Ob eine eigene
VSIX-Erweiterung ihn importieren kann oder ob er nur Add-ins zur Verfügung
steht, ist damit statisch nicht zu entscheiden.


=== Frage 5: Metadata-API gegen direkten Dateizugriff


Belegart: Ausführung von P4 auf dem privaten Gerät. Verwendet wurden die neun
aus der D365-Umgebung kopierten Assemblies (27,9 MB):
Microsoft.Dynamics.AX.Metadata, .Metadata.Core, .Metadata.Storage,
.Metadata.Management.Core, .Management.Delta, .Management.Diff,
.Management.Merge, Microsoft.Dynamics.ApplicationPlatform.PerformanceCounters und
.XppServices.Instrumentation.


==== Zielframework


Die Metadata-Assemblies sind für .NET Framework 4.8 gebaut. Ein Projekt mit dem
Zielframework net472, wie es die VSIX-Vorlage von Visual Studio vorgibt, kann sie
nicht referenzieren:

```
warning MSB3274: The primary reference "Microsoft.Dynamics.AX.Metadata" could
not be resolved because it was built against the ".NETFramework,Version=v4.8"
framework. This is a higher version than the currently targeted framework
".NETFramework,Version=v4.7.2".
```


Eine Erweiterung, die die Metadata-API verwendet, muss also net48 anvisieren.


==== Oberfläche


Einstieg ist MetadataProviderFactory.CreateDiskProvider(Pfad) mit dem Ergebnis
IDiskMetadataProvider. Dessen Eigenschaft LabelFiles bietet unter anderem
ListObjectsForModel(Modell), Read(Name) mit dem Ergebnis AxLabelFile,
GetContent(Labeldatei, Modell) sowie zum Schreiben Create und PutContent.
AxLabelFile trägt LabelFileId, Language, LabelContentFileName und
RelativeUriInModelStore. ModelManifest.Create(ModelInfo) legt ein Modell an.

GetContent liefert den Inhalt der .label.txt-Datei als unverarbeiteten Stream.
Die Metadata-API zerlegt die Datei nicht in einzelne Labels; das Parsen ist bei
beiden Zugriffsarten eigene Arbeit.


==== Testdaten


Die Testdaten wurden mit der Metadata-API selbst erzeugt, also mit Microsofts
eigenem Schreibpfad und mit eigenen Inhalten: ein Modell "SpikeLabels" mit einer
Labeldatei in den Sprachen en-US, de-CH und fr-CH. Die API legte diesen Aufbau an:

```
SpikeLabels\Descriptor\SpikeLabels.xml
SpikeLabels\SpikeLabels\AxLabelFile\SpikeLabels_<Sprache>.xml
SpikeLabels\SpikeLabels\AxLabelFile\LabelResources\<Sprache>\SpikeLabels.<Sprache>.label.txt
```


Eine Falle für direkten Dateizugriff: In der XML-Datei für en-US fehlt das
Element Language, weil en-US der Vorgabewert ist. Wer die Sprache aus der
XML-Datei liest, erhält für Englisch keinen Wert.

Bemerkenswert ist ausserdem, dass die API auf dem privaten Gerät ohne
Anwendungsserver, Datenbank oder D365-Werkzeuge lief.


==== Messung


Beide Zugriffsarten lieferten in allen Läufen dasselbe Ergebnis. Zeiten in
Millisekunden, jeweils Einzelläufe:

```
Labels            Lauf         Metadata-API                  direkter Zugriff
3 x 1'000         kalt         492 (davon 319 Provider)      35
3 x 20'000        1, kalt      556 (davon 322 Provider)      77
3 x 20'000        2, warm      113 (davon 57 Provider)       112
```


Der Unterschied besteht damit nur beim ersten Aufruf und ist Startaufwand. Im
warmen Zustand sind beide gleich schnell, weil das Parsen den Aufwand bestimmt.
Die Zahlen sind Einzelmessungen an einem Speicher mit nur einem Modell; ein
echtes PackagesLocalDirectory mit vielen Modellen verlangsamt beide Zugriffsarten.


==== Label-Auflösung


Die D365-Werkzeuge enthalten mit
Microsoft.Dynamics.Framework.Tools.Labels.Resolvers.dll (22 KB) einen fertigen
Label-Auflöser: LabelResolverFactory.CreateLabelResolver(CultureInfo,
IMetaLabelFileProvider) liefert ein ILabelResolver mit GetLabelText(LabelId).
Das entspricht fast genau der Abfrage, die FA05 braucht. Er steht zur Laufzeit in
Visual Studio zur Verfügung, sofern die D365-Werkzeuge installiert sind; zur
Weitergabe mit einer eigenen Erweiterung ist er nicht bestimmt.

Ausserhalb von Visual Studio scheiterte er an einer Abhängigkeit, die nicht in
den kopierten Dateien lag:

```
System.IO.FileNotFoundException: Could not load file or assembly
'Microsoft.Dynamics.AX.Framework.Diagnostics, Version=7.0.0.0, Culture=neutral,
PublicKeyToken=31bf3856ad364e35' or one of its dependencies. The system cannot
find the file specified.
```


Die Assembly liegt im Ordner der D365-Erweiterung und ist für die nächste
Übergabe angefordert (Abschnitt 13).

Zur Einordnung: Die D365-Werkzeuge enthalten mit
Microsoft.Dynamics.Framework.Tools.LabelEditor.17.0.dll ausserdem einen eigenen
Label-Editor von Microsoft.


=== Laufzeitprobe P5



==== Zweck und Aufbau


P5 beantwortet die Laufzeitfragen, die sich aus Dateien nicht klären lassen.
Die Probe protokolliert nach %LOCALAPPDATA%\\VsSpikeProbe:

- beim Start die Visual-Studio-Version und welche D365-Assemblies geladen sind
- jede geöffnete Textansicht mit Content Type, Basistypen, Rollen und Datei
- ob ein eigener IWpfTextViewCreationListener mit [ContentType("X++")] angehängt wird (Frage 1), welche Klassifizierungen dort vorkommen und welche Token als Label klassifiziert sind; Label-Token erhalten eine kleine graue Markierung
- ob eine eigene QuickInfo-Quelle für X++ beim Ueberfahren gefragt wird und ob sie das Token als Label erkennt (FA05); bei Labels ergänzt sie eine Zeile
- jeden Aufruf eines IAsyncCodeLensDataPointProvider mit Datei und Element (Frage 2); ein Datenpunkt wird nur für .xpp-Dateien geliefert
- was das Eigenschaftsfenster bei einer Auswahl erhält: Typ, Klassenname, Anzahl und Namen der Eigenschaften, eigene Eigenschaftseditoren (Frage 3)
- ab Version 1.1, sobald das Auswahlobjekt eine Eigenschaft ModelElement hat: den Typ des Objekts dahinter, seine Eigenschaften über TypeDescriptor und über Reflexion, und für Label, HelpText, DeveloperDocumentation, Name und Extends den aktuellen Wert, den Eigenschaftseditor, den Typkonverter und ob die Eigenschaft schreibgeschützt ist. Die Probe liest nur und setzt nie einen Wert. Scheitert das Lesen über den Eigenschaftsdeskriptor, versucht sie es über Reflexion und vermerkt den verwendeten Weg. Jede Kombination aus Typ und Element wird nur einmal protokolliert.

Die Auswertung von ModelElement wurde vor der Übergabe ohne Visual Studio gegen
nachgebildete Objekte geprüft, die dem beobachteten Aufbau entsprechen: ein
Auswahlobjekt, das nur ModelElement offenlegt, dahinter ein Element mit Label,
HelpText und einem eigenen Editor am Label. Beide Lesewege, die Erkennung des
Editors, die Einmalprotokollierung und das Schweigen bei Objekten ohne
ModelElement verhielten sich wie vorgesehen.


==== Nachweis auf dem privaten Gerät


Die Probe wurde in einer Experimentalinstanz von Visual Studio Community 2026,
Version 18.9.2, geladen. Damit ist zum ersten Mal in diesem Spike belegt, dass
eine Erweiterung mit AsyncPackage und MEF-Editorteilen auf Basis von net48 in
Visual Studio 2026 lädt und ausgeführt wird. Auszüge aus dem Protokoll:

```
[package]  Visual Studio release version: 18.9.12120.119 stable
[view]  opened: contentType=CSharp; bases=[Roslyn Languages > code-languageserver-base
        > code > languageserver-base > text > any]; roles=[DEBUGGABLE,PRIMARYDOCUMENT,
        ANALYZABLE,DOCUMENT,EDITABLE,INTERACTIVE,STRUCTURED,ZOOMABLE,UBIDIFF,UBIRIGHTDIFF]
[selection]  type=Microsoft.VisualStudio.ProjectSystem.VS.Implementation.PropertyPages.
        DynamicTypeBrowseObject; className=File Properties; properties=18; customEditors=[]
```


Die X++-Teile der Probe konnten hier nicht auslösen, da auf dem privaten Gerät
kein X++ registriert ist. Sie wurden ohne Kompositionsfehler in den MEF-Katalog
von Visual Studio aufgenommen.


==== Befunde zur Bereitstellung


Beim Nachweis traten zwei Probleme auf, die für jede eigene Erweiterung gelten.

Registrierung nach Installation über die Kommandozeile. Nach
VSIXInstaller.exe /quiet war die Erweiterung in Visual Studio 2026 als aktiviert
geführt und ihre MEF-Teile standen im Katalog, das Paket wurde aber nicht
registriert und nie geladen. Die GUID des Pakets fehlte in der privaten
Registry der Instanz. Das Aktivitätsprotokoll zeigte den Grund:

```
PkgDefCache fast check: timestamps are current.
CPkgDefCacheNonVolatileBase: PkgDef cache is current.
```


Erst devenv.exe /updateconfiguration führte die pkgdef-Datei zusammen. Danach
lud das Paket beim nächsten Start.

TextViewRole ist Pflicht. Ein IWpfTextViewCreationListener ohne mindestens ein
[TextViewRole]-Attribut wird vom Editor ohne jede Meldung ignoriert: Er steht im
MEF-Katalog, es gibt keinen Kompositionsfehler, er wird aber nie aufgerufen.
Die Microsoft-Beispiele und die D365-Werkzeuge setzen das Attribut stets.


==== Beobachtungen auf der D365-Entwicklungsumgebung (2026-09-14)


Belegart: Laufzeitbeobachtung. Die Probe wurde nach Abschnitt 13 in Visual Studio
Professional 2026, Version 18.5.11709.299, installiert und in zwei Sitzungen
verwendet. Grundlage sind die beiden Protokolle und drei Bildschirmfotos.

D365-Werkzeuge in Visual Studio 2026. Beim Laden der Probe waren in beiden
Sitzungen bereits 16 D365-Assemblies geladen, nach dem Oeffnen eines Projekts 52
und nach dem Oeffnen des X++-Editors 57, darunter der Sprachdienst, die
Metadata-Assemblies und der X++-Parser. Projekt, Designer und X++-Editor liessen
sich bedienen, und der D365-eigene Tooltip löste Labels auf. Die D365-Werkzeuge
laufen in Visual Studio 2026 also, obwohl ihr Manifest nur den Bereich
[17.0, 18.0) deklariert. Auf welchem Weg Visual Studio 2026 die Erweiterung
trotzdem lädt, wurde nicht untersucht.

Eigene Erweiterung neben den D365-Werkzeugen. Die Probe, eine klassische
Erweiterung mit AsyncPackage und MEF-Teilen auf Basis von net48, lud nach der
Installation mit anschliessendem /updateconfiguration und lief neben den
D365-Werkzeugen, ohne diese erkennbar zu stören.

Frage 1, zur Laufzeit bestätigt. Protokoll beim Oeffnen einer X++-Klasse:

```
[view]  opened: contentType=X++; bases=[code > code-languageserver-preview > text >
        code-languageserver-base > code-languageserver-textmate-color >
        code-languageserver-textmate-structure > code-languageserver-textmate-brace >
        code-languageserver-textmate-indentation > code-textmate-commentselection >
        any > languageserver-base]; roles=[DEBUGGABLE,PRIMARYDOCUMENT,ANALYZABLE,
        DOCUMENT,EDITABLE,INTERACTIVE,STRUCTURED,ZOOMABLE,UBIDIFF,UBIRIGHTDIFF];
        file=<Metadatenordner>\XppSource\<Modell>\AxClass_<Klasse>.xpp
[xpp]  X++ listener ATTACHED to a view
```


Ein eigener IWpfTextViewCreationListener mit [ContentType("X++")] und
[TextViewRole("DOCUMENT")] wird also an die X++-Ansicht angehängt. Die
X++-Ansicht trägt dieselben Rollen wie eine C\#-Ansicht. Der Editor arbeitet auf
einer echten .xpp-Datei im Ordner XppSource des Metadatenverzeichnisses. Die
eigene Einblendungsschicht zeichnete ihre graue Markierung sichtbar hinter die
Label-Token (Bildschirmfoto).

Label-Erkennung über die D365-Klassifizierung, zur Laufzeit bestätigt. Im
X++-Editor traten die Klassifizierungen "X++ Keyword", "X++ Operator",
"X++ String", "X++ Metadata Type" und "X++ Modern Label" auf. Die als Label
klassifizierten Spannen hatten die Form "\@\<Datei\>:\<Kennung\>, das heisst sie
beginnen mit dem öffnenden Anführungszeichen des Zeichenkettenliterals. Eine
Erweiterung muss dieses Zeichen vor der Auflösung entfernen. Die Klassifizierung
"X++ Legacy Label" wurde nicht beobachtet, weil kein Code mit der alten Label-Form
geöffnet war. Vorhanden ist die alte Form ohne Doppelpunkt (\@\<Datei\>\<Nummer\>)
auf der Umgebung aber, nämlich als Eigenschaftswert im Eigenschaftsfenster; eine
Erweiterung muss beide Formen verarbeiten.

FA05, Mechanismus zur Laufzeit bestätigt. Protokoll:

```
[quickinfo]  QuickInfo source CREATED for an X++ buffer
[quickinfo]  hover: classification="(none)"; token=
[quickinfo]  hover: classification="X++ Modern Label"; token="@<Datei>:<Kennung>
```


Die eigene QuickInfo-Quelle wird beim Ueberfahren von X++-Code gefragt und erkennt
Label-Token über die Klassifizierung. Ihr Eintrag erschien im selben Tooltip wie
der D365-eigene Eintrag, der den Labeltext bereits in einer Sprache anzeigte
(Bildschirmfoto). Beide Quellen bestehen nebeneinander. Aufgelöst hat die Probe
das Label nicht; das Anzeigen aller konfigurierten Übersetzungen ist damit noch
nicht erprobt.

Frage 2, zur Laufzeit gestützt. In beiden Sitzungen wurde kein einziger Aufruf
eines IAsyncCodeLensDataPointProvider protokolliert, und es lagen ausschliesslich
Protokolle des Prozesses devenv vor, keines aus dem Prozess, in dem Visual Studio
CodeLens ausführt. Die sichtbaren Zeilen "1 reference" über X++-Methoden stammen
von der D365-eigenen Nachbildung aus Abschnitt 7.3. Zusammen mit dem statischen
Befund spricht das deutlich dagegen, dass die CodeLens-Infrastruktur von Visual
Studio X++-Elemente erreicht. Ein Beweis ist das Fehlen von Aufrufen nicht: Es
wurde nicht geprüft, ob der CodeLens-Dienst in diesen Sitzungen für eine andere
Sprache gestartet war.

Frage 3, zur Laufzeit teilweise beantwortet. Die Probe protokollierte, welches
Objekt das Eigenschaftsfenster bei einer Auswahl erhält:

```
Projektknoten im Projektmappen-Explorer:
  ProjectNodeProperties (Tools.ProjectSupport), Klassenname "Project Properties",
  7 Eigenschaften, keine eigenen Editoren
Elementknoten im Projektmappen-Explorer:
  VSFileNodeProperties (Tools.ProjectSystem), etwa "Class Item Properties" mit 21
  und "Table Extension Properties" mit 65 Eigenschaften; eigener Editor nur bei
  der Klasse: Implements -> CollectionEditor
Knoten im Element-Designer (Tabellenerweiterung, Feldliste, Feld, EDT):
  DynamicsPropertyCollection (Tools.MetaModel); Klassenname ist der Elementname,
  Komponente die Knotenart (TableExtension, FieldCollectionNodeForExtension,
  FieldString, EdtString); genau eine Eigenschaft, ModelElement; keine eigenen
  Editoren
```


Für einen EDT-Knoten zeigte das Eigenschaftsfenster zahlreiche Eigenschaften,
darunter Label und Help Text mit Label-Kennungen, und bot beim Label eine
Schaltfläche "..." an (Bildschirmfoto). Die Probe 1.0 fragte das ausgewählte
Objekt mit TypeDescriptor.GetProperties ohne Attributfilter ab und sah dabei nur
die eine Eigenschaft ModelElement und keinen eigenen Editor. Die hier zunächst
geäusserte Vermutung, die angezeigten Eigenschaften kämen über das Objekt hinter
ModelElement, hat die Probe 1.1 widerlegt; die Aufklärung steht in Abschnitt 9.5.

Folge für Frage 3: Eine eigene Erweiterung erfährt über die Selection Tracking
von Visual Studio, welcher Designerknoten ausgewählt ist, einschliesslich
Elementname und Knotenart. Ob und wie sich die Eigenschaften des Elements lesen
lassen, beantwortet Abschnitt 9.5.

Fehlermeldung. In Visual Studio 2026 erschien die Meldung "Failed to add assembly
redirects to test platform configuration files. Please open the output window
pane 'Dynamics 365 for Finance and Operations' to see more details." Der genannte
Ausgabebereich war leer. Die Ursache ist nicht geklärt; in Frage kommt eine andere
installierte Erweiterung. Ein Zusammenhang mit der Probe ist nicht erkennbar,
wurde aber nicht gezielt ausgeschlossen. Der Punkt wurde mit niedriger Priorität
zurückgestellt.


==== Nachtrag: das Objekt hinter ModelElement (Probe 1.1, 2026-09-14)


Belegart: Laufzeitbeobachtung, eine Sitzung in Visual Studio Professional 2026,
Version 18.5.11709.299. Im Designer wurde ein einzelner Knoten ausgewählt, ein
erweiterter Datentyp der Art EdtString, bei dem Label und Help Text gesetzt sind.
Ein Knoten ohne Label wurde in dieser Sitzung nicht ausgewählt, ebenso wenig ein
Formular- oder Tabellenknoten.

Das Auswahlobjekt. DynamicsPropertyCollection implementiert ICustomTypeDescriptor
und beantwortet die beiden Abfragen unterschiedlich:

```
TypeDescriptor.GetProperties(Objekt)                              1 Eigenschaft (ModelElement)
TypeDescriptor.GetProperties(Objekt, BrowsableAttribute.Yes)     28 Eigenschaften
```


Ein PropertyGrid wendet den Filter BrowsableAttribute.Yes standardmässig an. Das
Eigenschaftsfenster erhält seine Eigenschaften also vom Auswahlobjekt selbst,
über die gefilterte Abfrage. Das erklärt, weshalb die Probe 1.0 ohne Filter nur
eine Eigenschaft sah.

Das Objekt hinter ModelElement. Gelesen über den Eigenschaftsdeskriptor, ohne
Ausnahme:

```
type=Microsoft.Dynamics.Framework.Tools.MetaModel.BaseTypes.EdtString
base=Microsoft.Dynamics.Framework.Tools.MetaModel.BaseTypes.EdtBase
customTypeDescriptor=False
interfaces=[IMergeElements, ICommandProvider, IDisposable, ITaggable,
  ITableBrowsable, IPropertyBehavior, IViewDefinitionProvider, IRenameable,
  INamedObject, IMetaElement, IRootElement, IAccessible, IObsoletable,
  ICompositeCollectionNodeContainer]
TypeDescriptor: 37 Eigenschaften, davon 33 mit BrowsableAttribute.Yes
Reflexion: 77 oeffentliche Eigenschaften
```


Es handelt sich um einen Entwurfszeit-Typ der D365-Werkzeuge aus der Assembly
Microsoft.Dynamics.Framework.Tools.MetaModel.17.0, nicht um einen Typ der
Metadata-API aus Abschnitt 8.

Eigenschaften mit Labelbezug, über TypeDescriptor gelesen:

```
Label            String   Wert @<Datei><Nummer>   readOnly=False  kein Editor  StringConverter        Appearance
HelpText         String   Wert @<Datei><Nummer>   readOnly=False  kein Editor  StringConverter        Appearance
CollectionLabel  String   leer                    readOnly=False  kein Editor  StringConverter        Appearance
Extends          String   Wert <Basistyp>         readOnly=False  kein Editor  EdtByTypeTypeConverter Data
Name             String   Wert <EDT>              readOnly=False  kein Editor  StringConverter        Data
```


Folgen für Frage 3:

- Lesen gelingt. Eine eigene Erweiterung im gleichen Prozess erhält über die Selection Tracking von Visual Studio das im Designer ausgewählte Element und kann über ModelElement dessen Label und Help Text als Label-Kennung lesen, ohne das Add-in-Modell zu verwenden. Das genügt, um ausgewählte Designerelemente für die Anzeige von Übersetzungen oder für die Textextraktion heranzuziehen. Die Label- Kennung hat dort die alte Form ohne Doppelpunkt.
- Schreiben ist nicht erprobt. Die Deskriptoren melden readOnly=False. Ob ein Wert, der auf diesem Weg gesetzt wird, vom Designer korrekt übernommen wird, also mit Speichern, Rückgängig und Aenderungsmarkierung, wurde bewusst nicht versucht. Nach dem Entscheid vom 2026-09-14 wird das auch nicht weiter verfolgt: Aenderungen können bei Bedarf direkt in den Dateien vorgenommen werden und brauchen keine Schnittstelle der Entwicklungsumgebung.
- Der Label-Editor, also die Schaltfläche "...", hängt nicht an der Label-Eigenschaft von ModelElement; dort ist kein Editor eingetragen. Er muss an den 28 Deskriptoren des Auswahlobjekts oder an anderer Stelle hängen; deren Editoren hat die Probe nicht protokolliert. Ein öffentlicher Weg, eigene Editoren oder Schaltflächen in das Eigenschaftsfenster einzuhängen, ist damit weiterhin nicht gefunden.
- EdtString, EdtBase und die Typkonverter sind interne Typen der D365-Werkzeuge. Wer die Eigenschaften, wie die Probe, über TypeDescriptor und Eigenschaftsnamen liest, braucht keine Referenz auf diese Typen, bleibt aber vom Aufbau der jeweiligen Version der D365-Werkzeuge abhängig.


=== Zum Zielkonflikt in der Aufgabenstellung


Die nicht-funktionale Anforderung, dass ein Fehler Visual Studio nicht abstürzen
lassen darf, spricht für den eigenen Prozess. Die Editor-Anzeigen brauchen MEF
und damit denselben Prozess. Der Spike zeigt, dass dieser Konflikt echt ist und
mit der heutigen Oberfläche nicht innerhalb einer einzigen Erweiterung
aufgelöst werden kann:

- Der eigene Prozess kauft die Prozesstrennung mit dem Verlust von QuickInfo und freien Einblendungen und mit einer Vorschau-Abhängigkeit für CodeLens.
- Der gleiche Prozess, ob klassisch oder mit dem neuen Modell, gibt die Prozesstrennung vollständig auf. P3 hat gegenüber P1 in dieser Hinsicht keinen Vorteil: gleicher Prozess, gleiches Absturzrisiko.

Ein Weg, der beides teilweise erfüllt, wäre eine Aufteilung in zwei
Erweiterungen: die inhaltliche Arbeit, also Label-Verwaltung, Dateizugriff und
Suche, im eigenen Prozess, und eine bewusst dünn gehaltene MEF-Anzeigeschicht
im gleichen Prozess. Der Preis ist ein zusätzlicher Kommunikationsweg zwischen
beiden Teilen und doppelte Auslieferung. Diese Variante wurde im Spike nicht
gebaut und ist damit nicht belegt; sie ist eine Ableitung aus den Befunden.


=== Stolpersteine beim Bau und bei der Bereitstellung


Diese Punkte kosteten im Spike Zeit und sind für eine Aufwandsschätzung
relevant.

MEF-Referenz. In Projekten im SDK-Stil mit Zielframework net472 wird die
Assembly System.ComponentModel.Composition nicht automatisch referenziert. Ohne
einen expliziten Eintrag \<Reference Include="System.ComponentModel.Composition" /\>
scheitert jede MEF-Registrierung:

```
error CS0234: The type or namespace name 'Composition' does not exist in the
namespace 'System.ComponentModel' (are you missing an assembly reference?)

error CS0246: The type or namespace name 'ExportAttribute' could not be found
(are you missing a using directive or an assembly reference?)
```


GeneratePkgDefFile. Solange eine Erweiterung reines MEF ohne VSPackage ist, muss
GeneratePkgDefFile auf false stehen. Andernfalls bricht der Bau nach erfolgreicher
Kompilierung im Paketierungsschritt ab:

```
CreatePkgDef : error : ArgumentException: No Visual Studio registration
attribute found in this assembly.
The assembly should contain an instance of the attribute
'Microsoft.VisualStudio.Shell.RegistrationAttribute' defined in assembly
'Microsoft.VisualStudio.Shell.Framework' version '18.0.0.0'
```


Bemerkenswert an dieser Meldung ist die genannte Version 18.0.0.0: Die
Buildwerkzeuge erwarten Shell-Assemblies der Hauptversion 18, während das
referenzierte SDK-Metapaket bei 17.14 steht.

Reihenfolge von Restore und Build. Die Ziele Restore und Build dürfen nicht in
einem einzigen MSBuild-Aufruf zusammengefasst werden. Geschieht das doch, wird
das Projekt ausgewertet, bevor die Ziele des SDK wiederhergestellt sind, und der
Bau scheitert mit VSEXT0008, obwohl der Schalter in der Projektdatei korrekt
gesetzt ist. Zwei getrennte Aufrufe lösen das.

Pfadlänge. Das verwendete Arbeitsverzeichnis war rund 200 Zeichen lang, womit
MSBuild an die Pfadgrenze von Windows stiess:

```
error : ... exceeds the OS max path limit. The fully qualified file name must
be less than 260 characters.

MSBUILD : error : This is an unhandled exception in MSBuild
System.NullReferenceException: Object reference not set to an instance of an object.
```


Dieselbe Grenze gilt für Windows PowerShell 5.1. Das Projekt liegt deshalb
inzwischen unter einem kurzen Pfad.

Zugriffsmodifikator beim Ueberschreiben. Beim Ableiten von TextViewTagger\<T\> ist
RequestTagsAsync als protected internal deklariert. Ueber Assembly-Grenzen hinweg
muss die Ableitung protected verwenden, sonst:

```
error CS0507: cannot change access modifiers when overriding 'protected internal'
inherited member
```


Zielframework bei Verwendung der Metadata-API: net48 statt net472 (Abschnitt 8.1).

Registrierung nach Installation über die Kommandozeile: devenv.exe
/updateconfiguration ausführen (Abschnitt 9.3).

TextViewRole bei Textansicht-Listenern immer setzen (Abschnitt 9.3).

devenv.exe /log nimmt optional einen Dateinamen entgegen. Folgt auf /log direkt
ein Dateipfad, schreibt Visual Studio sein Aktivitätsprotokoll in diese Datei
und überschreibt sie. Im Spike betraf das eine Quelldatei von P1, die aus einer
Kopie wiederhergestellt wurde.


=== Was offen geblieben ist


Frage 1, Content Type. Beantwortet, statisch (Abschnitt 7.2) und zur Laufzeit
(Abschnitt 9.4).

Frage 2, CodeLens. Weitgehend beantwortet: Die D365-Werkzeuge verwenden eine
eigene Nachbildung (Abschnitt 7.3), und zur Laufzeit erreichte kein Aufruf der
CodeLens-Infrastruktur von Visual Studio die Probe (Abschnitt 9.4). Das Fehlen
von Aufrufen ist kein Beweis.

Frage 3, Eigenschaftsfenster. Weitgehend beantwortet: Das Add-in-Modell bietet
keinen Erweiterungspunkt (Abschnitt 7.4). Das im Designer ausgewählte Element und
seine Label-Eigenschaften lassen sich zur Laufzeit über ModelElement lesen
(Abschnitt 9.5). Offen sind zwei Punkte: woher der Label-Editor im
Eigenschaftsfenster stammt, und wie sich Formular- und Tabellenknoten verhalten, da
mit Probe 1.1 nur ein EDT-Knoten geprüft wurde. Das Schreiben über den Designer
wird nicht weiter verfolgt, weil Aenderungen bei Bedarf direkt in den Dateien
erfolgen können (Entscheid vom 2026-09-14).

Frage 5, Metadata-API. Beantwortet (Abschnitt 8). Offen ist nur, ob Microsofts
Label-Auflöser mit der fehlenden Abhängigkeit läuft.

FA05, Tooltip mit Übersetzungen. Der Mechanismus ist zur Laufzeit bestätigt
(Abschnitt 9.4). Nicht erprobt ist die eigentliche Auflösung eines Labels in
alle konfigurierten Sprachen innerhalb des Tooltips.

D365-Werkzeuge in Visual Studio 2026. Beantwortet: Sie laufen dort (Abschnitt 9.4).
Nicht untersucht ist, weshalb Visual Studio 2026 die Erweiterung trotz des
deklarierten Bereichs [17.0, 18.0) lädt.

IMetaModelProviders aus einer eigenen VSIX-Erweiterung. Statisch nicht
entscheidbar (Abschnitt 7.4) und zur Laufzeit nicht geprüft.

Laufzeitverhalten der Modell-Prototypen. Zur Laufzeit belegt ist P5, der
dieselben Mechanismen wie P1 verwendet, und zwar auf dem privaten Gerät in
Version 18.9 und auf der D365-Umgebung in Version 18.5. P2 und P3, also das neue
Modell im eigenen und im gleichen Prozess, wurden weiterhin nicht in einer
laufenden Instanz geladen. Damit ist auch nicht geprüft, ob eine mit dem SDK
17.14 gebaute Erweiterung des neuen Modells in Version 18 lädt; belegt ist nur
der deklarierte Bereich [17.14, ).

Fehlermeldung zu den Test-Plattform-Konfigurationsdateien. Ursache nicht geklärt,
mit niedriger Priorität zurückgestellt (Abschnitt 9.4).

Ausfallsicherheit. Dass ein Fehler im eigenen Prozess Visual Studio nicht
beeinträchtigt, wurde nicht durch einen herbeigeführten Fehler nachgewiesen.
Die Aussage, dass der Betrieb im gleichen Prozess diesen Schutz nicht bietet,
folgt aus dem Prozessmodell und nicht aus einer Messung.

Lücken der Datenerhebung vom 2026-09-14. Das Sammelskript kopierte die Datei
.vsextension\\extension.json und die Assembly Microsoft.Dynamics.VSExtension.Shared.dll
nicht mit, ebenso wenig die Abhängigkeit des Label-Auflösers. Es wählte zudem
das erste gefundene PackagesLocalDirectory (10.0.2428.188) statt des neuesten.
Label-Dateien eines echten Modells wurden nicht erhoben; stattdessen dienten die
selbst erzeugten Testdaten aus Abschnitt 8.3. Die drei Dateien aus Abschnitt 13.4
liegen noch nicht vor.


=== Anleitung für die Laufzeitprobe auf der Entwicklungsumgebung


Am 2026-09-14 mit Version 1.0 (Ergebnis Abschnitt 9.4) und mit Version 1.1
(Ergebnis Abschnitt 9.5) in Visual Studio 2026 durchgeführt. Für eine
Wiederholung mit Version 1.1 zuerst Version 1.0 nach Abschnitt
13.5 deinstallieren und dann nach Abschnitt 13.1 installieren; dieser Weg wurde
auf dem privaten Gerät erprobt, das direkte Ueberinstallieren nicht. Wer nur die
Frage zu ModelElement klären will, braucht aus Abschnitt 13.2 nur die Schritte
1, 4 und 5.

Dauer etwa zehn Minuten. Die Probe verändert keine Dateien, Einstellungen oder
Quelltexte. Solange sie installiert ist, setzt sie hinter Label-Token eine kleine
graue Markierung und ergänzt bei Labels eine Zeile im Tooltip. Die Datei
P5.RuntimeProbe.vsix liegt im Ordner handoff-devenv des Spike-Verzeichnisses.


==== Installieren (Visual Studio geschlossen)


Eine normale PowerShell im Ordner mit der VSIX-Datei öffnen (im Explorer in den
Ordner wechseln, Datei, Windows PowerShell öffnen). Auf der Umgebung ist die
Edition Professional installiert:

```
$ide = 'C:\Program Files\Microsoft Visual Studio\18\Professional\Common7\IDE'
Start-Process "$ide\VSIXInstaller.exe" -ArgumentList '/quiet', "`"$PWD\P5.RuntimeProbe.vsix`"" -Wait
Start-Process "$ide\devenv.exe" -ArgumentList '/updateconfiguration' -Wait
```


Start-Process -Wait ist nötig, weil beide Programme ohne Konsole laufen und
PowerShell sonst nicht auf ihr Ende wartet. Der zweite Befehl ist notwendig
(Abschnitt 9.3); er öffnet kein Fenster, dauert etwa 20 Sekunden und kehrt
danach zur Eingabeaufforderung zurück. Auch nach einer Installation per
Doppelklick ausführen.

Ob die Installation geklappt hat, zeigt der Rückgabewert beider Befehle, wenn
man ihnen -PassThru anhängt und (...).ExitCode abfragt; 0 bedeutet Erfolg.
Meldet Windows "Zugriff verweigert", PowerShell als Administrator starten.


==== In Visual Studio 2026 durchspielen


1. Visual Studio 2026 starten und ein D365-Projekt öffnen.
2. Eine X++-Klasse öffnen, deren Code Label-Verweise enthält, etwa \@SYS12345
oder \@Datei:Label, und etwas darin blättern.
3. Mit der Maus etwa zwei Sekunden auf einer Label-ID verweilen, danach auf
etwas anderem.
4. Einen Tabellen- oder Formular-Designer öffnen. Bei sichtbarem
Eigenschaftsfenster drei bis vier verschiedene Knoten anklicken: Wurzel, ein
Feld, eine Methode. Für Version 1.1 wichtig: mindestens einen Knoten wählen,
bei dem im Eigenschaftsfenster ein Label gesetzt ist, etwa ein Feld oder einen
erweiterten Datentyp, und zum Vergleich einen ohne Label.
5. Visual Studio schliessen.

Wahlweise die Schritte 2 bis 5 zum Vergleich in Visual Studio 2022 wiederholen.


==== Zurückschicken


```
Compress-Archive "$env:LOCALAPPDATA\VsSpikeProbe\*.log" "$env:USERPROFILE\Desktop\probe-logs.zip"
```


Es können mehrere Dateien entstehen, weil CodeLens in einem eigenen Prozess
läuft. Alle zurücksenden. Die Protokolle enthalten Dateipfade, kurze
Ausschnitte von höchstens 50 Zeichen aus dem geöffneten X++-Code, Namen der
ausgewählten Elemente und ab Version 1.1 auch Eigenschaftswerte wie
Label-Kennungen. Sie gehören deshalb nicht in das öffentliche Repository.

Vorher die Protokolle von Version 1.0 aus dem Ordner entfernen oder beiseite
legen, damit die neue Sitzung eindeutig zuzuordnen ist.


==== Zusätzlich drei Dateien kopieren


```
$ext = 'C:\Program Files\Microsoft Visual Studio\18\Professional\Common7\IDE\Extensions\cfc5q1kn.cht'
$out = "$env:USERPROFILE\Desktop\d365-followup"
New-Item -ItemType Directory $out -Force | Out-Null
Copy-Item "$ext\Microsoft.Dynamics.AX.Framework.Diagnostics.dll" $out
Copy-Item "$ext\Microsoft.Dynamics.VSExtension.Shared.dll" $out
Copy-Item "$ext\.vsextension\extension.json" $out
```


Die erste Datei lässt den Label-Auflöser aus Abschnitt 8.5 laufen, die beiden
anderen zeigen, was der Anteil des neuen Modells in den D365-Werkzeugen beiträgt.
Auch diese Dateien gehören nicht in das öffentliche Repository.


==== Deinstallieren


```
Start-Process 'C:\Program Files\Microsoft Visual Studio\18\Professional\Common7\IDE\VSIXInstaller.exe' -ArgumentList '/quiet', '/u:P5.RuntimeProbe.b3e1f7c2' -Wait
```


Alternativ: Erweiterungen, Erweiterungen verwalten, Installiert,
"Spike Runtime Probe", Deinstallieren.

== Besprechungsprotokolle

#[
  #show figure: set align(left)
  #figure(
    table(
      columns: (auto, 1fr),
      [*Datum*], [],
      [*Teilnehmende*], [],
      [*Traktanden*], [],
      [*Beschlüsse*], [],
      [*Pendenzen*], [],
    ),
    caption: [Besprechungsprotokoll Vorzeigetermin 1]
  ) <protokoll_1>
]

#[
  #show figure: set align(left)
  #figure(
    table(
      columns: (auto, 1fr),
      [*Datum*], [],
      [*Teilnehmende*], [],
      [*Traktanden*], [],
      [*Beschlüsse*], [],
      [*Pendenzen*], [],
    ),
    caption: [Besprechungsprotokoll Vorzeigetermin 2]
  ) <protokoll_2>
]

== Onlinepublikation

// Inhalte gemäss Richtlinien: Kurzbeschreibung des Ergebnisses resp. des Produktes,
// Nutzen / Mehrwert für den Auftraggeber, Veranschaulichung mittels frei wählbarer
// Elemente (z.B. Fotos, Filme).

=== Kurzbeschreibung des Produkts

=== Nutzen / Mehrwert für den Auftraggeber

=== Veranschaulichung
