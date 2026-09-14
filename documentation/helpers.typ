// Gemeinsame Hilfsfunktionen für alle Kapitel.
// Einbinden mit: #import "../helpers.typ": todo

// Sichtbarer Platzhalter für offene Punkte. Erscheint im PDF, damit nichts
// untergeht. Vor der Abgabe müssen alle weg sein.
// Auffinden mit: grep -rn "todo\[" chapters/
#let todo(body) = block(
  width: 100%,
  fill: rgb("#fff3cd"),
  stroke: (left: 3pt + rgb("#d39e00")),
  inset: 8pt,
  radius: 2pt,
  [#text(weight: "bold")[TODO ] #body]
)
