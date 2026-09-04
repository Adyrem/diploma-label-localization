= Reflexion und Lessons learned

== Reflexion Weg zum Ziel

=== Projektinitialisierung und -planung

=== Realisierung

=== Dokumentation

== Soll/Ist-Vergleich

// Der Vergleichsplan wird laufend nachgeführt. Quelle für die Ist-Daten ist die
// Tabelle "Detailplanung und Aufwanderfassung" im Anhang.

#page(flipped: true, margin: (x: 2cm, y: 2cm))[
  #figure(
    image("../diagrams/Terminplan_SollIst.png", width: 100%),
    caption: [Soll/Ist-Vergleich der Arbeitspakete (eigene Darstellung)]
  ) <terminplan_soll_ist_chart>
]

Graue Balken zeigen die Planung, farbige den effektiven Verlauf.

#[
  #show figure: set align(left)
  #figure(
    table(
      align: left,
      columns: (auto, 1fr, auto, auto, auto, 1.6fr),
      table.header(
        [*Nr.*],
        [*Arbeitspaket*],
        [*Soll\ Tage*],
        [*Ist\ Tage*],
        [*Ab-\ weichung*],
        [*Begründung*],
      ),
      [AP1], [Projektinitialisierung], [7], [], [], [],
      [AP2], [Konzept], [5], [], [], [],
      [AP3], [Realisierung], [6], [], [], [],
      [AP4], [Endspurt Realisierung und Test], [3], [], [], [],
      [AP5], [Dokumentation finalisieren], [3], [], [], [],
      [AP6], [Präsentation vorbereiten], [4], [], [], [],
      [], [*Total*], [*28*], [], [], [],
    ),
    caption: [Soll/Ist-Vergleich Aufwand]
  ) <soll_ist_vergleich>
]

== Lessons learned

#[
  #show figure: set align(left)
  #figure(
    table(
      align: left,
      columns: (auto, 1fr, 1fr),
      table.header(
        [*Nr.*], [*Erkenntnis*], [*Konsequenz für künftige Projekte*],
      ),
      [], [], [],
      [], [], [],
      [], [], [],
      [], [], [],
    ),
    caption: [Lessons learned]
  ) <lessons_learned>
]
