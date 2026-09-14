#set text(lang: "de")
#set page("a4")
#set page(margin: 2cm)

#set text(lang: "de", size: 11pt)
#set page(margin: (left: 3cm, bottom: 2cm, top: 2cm))
#set par(leading: 0.5em)
#set text(font: "DM Sans")

// Lange Tabellen dürfen über Seiten umbrechen, sonst entstehen grosse Lücken.
#show figure: set block(breakable: true)

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

#include "chapters/06_reflexion.typ"

#pagebreak()

#include "chapters/07_schlusswort.typ"

#pagebreak()

#include "chapters/08_anhang.typ"

#pagebreak()

// ---------------------------------------------------------------------------
// Verzeichnisse
// ---------------------------------------------------------------------------
#bibliography("works.yml", title: [Literaturverzeichnis], style: "ieee")

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
      [Analysieren und Überarbeiten der Kapiteltexte nach inhaltlichen Vorgaben des
       Autors, Erstellen der Diagramme als PlantUML-Quelltext, Recherche zu den
       Schnittstellen von Visual Studio und Dynamics 365.],
      [Kapitel 1 und 2, sämtliche Abbildungen ausser den Screenshots],
      [Claude Code (Anthropic)],
      [Durchführung der Machbarkeitsstudie zum Extension-Modell in einer eigenen
       Session, Bau der drei Prototypen und Erstellen des Befundprotokolls.],
      [Abschnitt 2.8.2, @befundprotokoll],
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
      [CodeLens],
      [Einblendung oberhalb einer Codezeile, die zusätzliche Angaben zu dieser Zeile
       anzeigt.],
      [Content Type],
      [Kennzeichnung, mit der Visual Studio den Inhalt einer Datei einordnet. Sie
       bestimmt, welche Editor-Erweiterungen in einer Datei greifen.],
      [Cross-Reference-Datenbank],
      [Datenbank von Dynamics 365, die beim Build gefüllt wird und festhält, wo ein
       Element verwendet wird. Visual Studio nutzt sie für die Referenzsuche.],
      [Disk-Provider],
      [Zugangspunkt der Metadata-API. Er richtet den Zugriff auf ein
       Package-Verzeichnis auf der Festplatte, sodass sich die dort abgelegten
       Models lesen und schreiben lassen, ohne dass ein Anwendungsserver oder eine
       Datenbank läuft.],
      [Label],
      [Platzhalter mit einer eindeutigen ID, den die Anwendung zur Laufzeit durch die
       Übersetzung in der Sprache des Benutzers ersetzt.],
      [MEF],
      [Managed Extensibility Framework. Mechanismus, über den Visual Studio
       Erweiterungen zur Laufzeit einsammelt und einbindet.],
      [Metadata-API],
      [Von Microsoft ausgelieferte Assemblies, über die sich die Elemente eines
       Models lesen und schreiben lassen, ohne die Dateien selbst zu kennen.],
      [Model],
      [Einheit, in der Erweiterungen für Dynamics 365 ausgeliefert werden. Bündelt
       Code, Metadaten und Label-Dateien.],
      [QuickInfo],
      [Fenster im Editor, das beim Überfahren einer Stelle mit der Maus erscheint.],
      [Tagger],
      [Bestandteil einer Editor-Erweiterung, der Textbereiche mit
       Zusatzinformationen verknüpft, auf die andere Funktionen aufbauen.],
      [VSIX],
      [Paketformat, in dem eine Erweiterung für Visual Studio ausgeliefert und
       installiert wird.],
      [VSSDK],
      [Ursprüngliches Erweiterungspaket von Visual Studio mit Zugriff auf alle
       internen Dienste.],
    ),
    caption: [Glossar (eigene Darstellung)]
  ) <glossar>
]

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
