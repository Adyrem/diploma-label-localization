#set text(lang: "de")
#set page("a4")
#set page(margin: 2cm)

#set text(lang: "de", size: 11pt)
#set page(margin: (left: 3cm, bottom: 2cm, top: 2cm))
#set par(leading: 0.5em)
#set text(font: "DM Sans")

// Lange Tabellen dürfen über Seiten umbrechen, sonst entstehen grosse Lücken.
#show figure: set block(breakable: true)
#show figure.where(kind: image): set block(breakable: false)

// X++ darf nicht zwischen den Pluszeichen umbrechen.
#show "X++": it => box(it)

// Verweise auf ein Kapitel heissen «Kapitel», nicht «Abschnitt».
#show heading.where(level: 1): set heading(supplement: [Kapitel])

// ---------------------------------------------------------------------------
// Deckblatt
// ---------------------------------------------------------------------------
#include "titelblatt.typ"

#pagebreak()

// ---------------------------------------------------------------------------
// Inhaltsverzeichnis
// ---------------------------------------------------------------------------
// Vorspann mit römischer Seitennummerierung
#set page(numbering: "I")
#counter(page).update(1)

#heading(outlined: false, numbering: none)[Inhaltsverzeichnis]
#show outline.entry.where(
  level: 1
): set block(above: 1em)
#outline(
    title: none,
    indent: 2em,
    depth: 2
)

#pagebreak()

// ---------------------------------------------------------------------------
// Vorspann (nicht nummeriert)
// ---------------------------------------------------------------------------
#include "chapters/00_management_summary.typ"

#pagebreak()

#include "chapters/01_lebenslauf.typ"

#pagebreak()

// Qualifikationsprofil: gemäss Richtlinien nur «sofern verlangt» (max. 2 A4-Seiten).
// Bei Bedarf die beiden folgenden Zeilen wieder aktivieren.
// #include "chapters/02_qualifikationsprofil.typ"
//
// #pagebreak()

// ---------------------------------------------------------------------------
// Hauptteil (nummeriert)
// ---------------------------------------------------------------------------
#set page(numbering: "1")
#counter(page).update(1)
#set heading(numbering: "1.")

#include "chapters/03_initialisierung.typ"

#pagebreak()

#include "chapters/04_konzept.typ"

#pagebreak()

#include "chapters/05_realisierung.typ"

#pagebreak()

#bibliography("works.yml", title: [Literaturverzeichnis], style: "ieee")

#pagebreak()

#include "chapters/06_reflexion.typ"

#pagebreak()

#include "chapters/07_schlusswort.typ"

#pagebreak()

// ---------------------------------------------------------------------------
// Eigenständigkeitserklärung
// ---------------------------------------------------------------------------
#heading(numbering: none)[Eigenständigkeitserklärung]

Hiermit bestätige ich, dass ich die vorliegende Diplomarbeit selbstständig erstellt habe und nur die angegebenen Quellen und Hilfsmittel verwendet wurden.

#v(1cm)

Ort, Datum:

#v(1.5cm)

Unterschrift:

#v(1cm)

#align(left)[Adrian Aeschlimann]

#pagebreak()

// ---------------------------------------------------------------------------
// Verzeichnisse
// ---------------------------------------------------------------------------
#heading(numbering: none)[Tabellenverzeichnis]
#outline(title: none, target: figure.where(kind: table))

#heading(numbering: none)[Abbildungsverzeichnis]
#outline(title: none, target: figure.where(kind: image))

#heading(numbering: none)[Hilfsmittelverzeichnis]

#[
  #show figure: set align(left)
  #figure(
    table(
      align: left,
      columns: 3,
      table.header(
        [*Welches Hilfsmittel wurde eingesetzt?*],
        [*Wozu wurde das Hilfsmittel eingesetzt?*],
        [*Betroffene Stellen*],
      ),
      [Claude Code (Anthropic)],
      [Analysieren und Überarbeiten der Kapiteltexte nach meinen inhaltlichen
       Vorgaben, Erstellen der Diagramme als PlantUML-Quelltext, Recherche zu den
       Schnittstellen von Visual Studio und Dynamics 365.],
      [@initialisierung, @konzept, Abkürzungsverzeichnis, Glossar, sämtliche
       Abbildungen ausser den Screenshots],
      [Claude Code (Anthropic)],
      [Durchführung der Machbarkeitsstudie zum Extension-Modell in einer eigenen
       Session, Bau der fünf Prototypen und Erstellen des Befundprotokolls.],
      [@machbarkeitsbeurteilung, @befundprotokoll],
      [Claude (Anthropic)],
      [Review der Projektinitialisierung gegen Themeneingabe und
       Richtlinien sowie Review des Konzepts gegen die Projektinitialisierung,
       das Befundprotokoll und die getroffenen Entscheide. Die Befunde wurden
       geprüft und anschliessend eingearbeitet.],
      [@initialisierung, @konzept, Lebenslauf, Verzeichnisse, Anhang],
      [Typst],
      [Formatierung und Erzeugung des Dokuments.],
      [Gesamtes Dokument],
      [PlantUML],
      [Rendern der Diagramme aus dem Quelltext.],
      [Abbildungen],
    ),
    caption: [Hilfsmittel (eigene Darstellung)]
  ) <hilfsmittelverzeichnis>
]

#heading(numbering: none)[Abkürzungsverzeichnis]

#[
  #show figure: set align(left)
  #figure(
    table(
      align: left,
      columns: (auto, 1fr),
      table.header(
        [*Abkürzung*],
        [*Bedeutung*],
      ),
      [AP], [Arbeitspaket],
      [API], [Application Programming Interface],
      [CI/CD], [Continuous Integration / Continuous Deployment],
      [D365], [Microsoft Dynamics 365],
      [EDT], [Extended Data Type],
      [EFZ], [Eidgenössisches Fähigkeitszeugnis],
      [FA], [Funktionale Anforderung],
      [GUI], [Graphical User Interface],
      [HF], [Höhere Fachschule],
      [ID], [Identifier, Bezeichner],
      [IDE], [Integrated Development Environment],
      [JSON], [JavaScript Object Notation],
      [KA], [Kann-Anforderung],
      [KI], [Künstliche Intelligenz],
      [MEF], [Managed Extensibility Framework],
      [NFA], [Nicht-funktionale Anforderung],
      [OA], [Organisatorische Anforderung],
      [Regex], [Regular Expression],
      [REST], [Representational State Transfer],
      [SDK], [Software Development Kit],
      [TC], [Test Case, Testfall],
      [TO], [Testobjekt],
      [UC], [Use Case],
      [UI], [User Interface],
      [UTF-8], [Unicode Transformation Format, 8 Bit],
      [VM], [Virtuelle Maschine],
      [VSIX], [Visual Studio Extension],
      [VSSDK], [Visual Studio Software Development Kit],
      [WPF], [Windows Presentation Foundation],
      [XML], [Extensible Markup Language],
    ),
    caption: [Abkürzungsverzeichnis (eigene Darstellung)]
  ) <abkuerzungsverzeichnis>
]

#heading(numbering: none)[Glossar]

#[
  #show figure: set align(left)
  #figure(
    table(
      align: left,
      columns: (1fr, 3fr),
      table.header(
        [*Fachwort*], [*Bedeutung*],
      ),
      [Add-in],
      [Komponente, die Visual Studio lädt und die an fest vorgegebenen Stellen der
       Oberfläche erscheint. Dynamics 365 stellt dafür ein eigenes Modell bereit.],
      [Classifier],
      [Komponente des Editors, die Spans eine Classification zuweist, etwa
       Schlüsselwort oder Label. Die Developer Tools bringen einen eigenen
       Classifier für X++ mit.],
      [CodeLens],
      [Einblendung oberhalb einer Codezeile, die zusätzliche Angaben zu dieser Zeile
       anzeigt.],
      [Content Type],
      [Kennzeichnung, mit der Visual Studio den Inhalt einer Datei einordnet. Sie
       bestimmt, welche Editor-Erweiterungen in einer Datei greifen.],
      [Cross-Reference-Datenbank],
      [Datenbank von Dynamics 365, die beim Build gefüllt wird und festhält, wo ein
       Element verwendet wird. Visual Studio nutzt sie für die Referenzsuche.],
      [Designer],
      [Fenster der Developer Tools, das ein Element als Baum seiner Knoten zeigt,
       etwa die Felder einer Table. Die Eigenschaften des gewählten Knotens stehen
       im Properties Window.],
      [Developer Tools],
      [Erweiterung von Microsoft, die Visual Studio für die Entwicklung mit
       Dynamics 365 ausstattet, unter anderem mit dem X++-Editor, den Designern
       und dem Build.],
      [Disk-Provider],
      [Zugangspunkt der Metadata-API. Er richtet den Zugriff auf ein
       Package-Verzeichnis auf der Festplatte, sodass sich die dort abgelegten
       Models lesen und schreiben lassen, ohne dass ein Anwendungsserver oder eine
       Datenbank läuft.],
      [Feature Parity],
      [Gleicher Funktionsumfang wie ein bestehendes System. In dieser Arbeit der
       Funktionsumfang des BE-LabelEditors.],
      [Klassische Entwicklungs-VM],
      [Virtuelle Maschine mit Dynamics 365, Visual Studio und den Developer Tools.
       Eigene Models und die von Microsoft liegen dort in einem gemeinsamen
       `PackagesLocalDirectory`, meist unter `K:\AOSService`.],
      [Label],
      [Platzhalter mit einer eindeutigen ID, den die Anwendung zur Laufzeit durch die
       Übersetzung in der Sprache des Benutzers ersetzt.],
      [Label-ID],
      [Eindeutiger Bezeichner eines Labels aus Label-Datei und Label, etwa
       `@BDM1:BDM110000003`.],
      [Margin],
      [Leiste am Rand des Editors, in der eine Extension Symbole oder Angaben zu
       einer Zeile anzeigen kann.],
      [MEF],
      [Managed Extensibility Framework. Mechanismus, über den Visual Studio
       Erweiterungen zur Laufzeit einsammelt und einbindet.],
      [Metadata-API],
      [Von Microsoft ausgelieferte Assemblies, über die sich die Elemente eines
       Models lesen und schreiben lassen, ohne die Dateien selbst zu kennen.],
      [Metadaten-Konfiguration],
      [Einstellung der Developer Tools in der Unified Developer Experience, die
       festlegt, in welchen Ordnern die eigenen Models und die Referenz-Metadaten
       liegen. Es kann mehrere geben, aktiv ist immer eine.],
      [Model],
      [Gruppe von Elementen wie Code, Metadaten und Label-Dateien, die eine
       auslieferbare Lösung bildet. Gehört immer zu einem Package.],
      [Package],
      [Einheit aus einem oder mehreren Models, die kompiliert und ausgeliefert
       wird.],
      [Package-Verzeichnis],
      [Ordner, in dem eine Installation von Dynamics 365 ihre Models mit Code,
       Metadaten und Label-Dateien ablegt.],
      [Properties Window],
      [Fenster von Visual Studio, das die Eigenschaften des gewählten Elements
       anzeigt und bearbeiten lässt.],
      [QuickInfo],
      [Fenster im Editor, das beim Überfahren einer Stelle mit der Maus erscheint.],
      [Selection Tracking],
      [Mechanismus von Visual Studio, über den eine Extension erfährt, welches
       Element gerade gewählt ist, etwa im Designer.],
      [Span],
      [Zusammenhängender Textbereich im Editor mit Anfang und Länge, etwa ein
       einzelnes Token.],
      [Spike],
      [Zeitlich begrenzte Wegwerfarbeit, die eine offene technische Frage
       beantwortet. Das Ergebnis ist Wissen und nicht Software, der dabei
       entstandene Code wird nicht weiterverwendet.],
      [String Literal],
      [Text im Code zwischen Anführungszeichen, etwa `"Lieferadresse"`. In X++
       steht auch eine Label-ID in einem String Literal.],
      [Tagger],
      [Bestandteil einer Editor-Erweiterung, der Textbereiche mit
       Zusatzinformationen verknüpft, auf die andere Funktionen aufbauen.],
      [Tool Window],
      [Fenster einer Extension, das sich in Visual Studio wie das Properties
       Window andocken lässt.],
      [Unified Developer Experience],
      [Entwicklung für Dynamics 365 mit Visual Studio auf dem eigenen Rechner,
       verbunden mit einer Umgebung in der Power Platform. Die Metadaten von
       Microsoft liegen im Benutzerprofil, die eigenen Models in einem Ordner nach
       Wahl.],
      [VSIX],
      [Paketformat, in dem eine Erweiterung für Visual Studio ausgeliefert und
       installiert wird.],
      [VSSDK],
      [Ursprüngliches Erweiterungspaket von Visual Studio mit Zugriff auf alle
       internen Dienste.],
      [X++-Editor],
      [Code-Fenster von Visual Studio, in dem die Developer Tools den Code eines
       Elements anzeigen, etwa nach View code auf einer Klasse, Table oder Form.
       Es arbeitet auf einer `.xpp`-Datei im Ordner XppSource und bringt die
       Einfärbung und den Tooltip der Developer Tools mit. Nicht dazu gehören der
       Designer mit den Knoten eines Elements und die `.xpp`-Textdateien, die auf
       dem privaten Gerät den X++-Editor im Debug-Build ersetzen.],
    ),
    caption: [Glossar (eigene Darstellung)]
  ) <glossar>
]

#pagebreak()

#include "chapters/08_anhang.typ"
