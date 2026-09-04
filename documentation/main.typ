#set text(lang: "de")
#set page("a4")
#set page(margin: 2cm)

#set text(lang: "de", size: 11pt)
#set page(margin: (left: 3cm, bottom: 2cm, top: 2cm))
#set par(leading: 0.5em)
#set text(font: "DM Sans")

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
      [], [], [],
      [], [], [],
      [], [], [],
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
      [], [],
      [], [],
      [], [],
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
