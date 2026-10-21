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
        [*Soll\ h*],
        [*Ist\ h*],
        [*Ab-\ weichung*],
        [*Begründung*],
      ),
      [AP1], [Projektinitialisierung], [56], [44], [−12], [],
      [AP2], [Konzept], [24], [21], [−3], [],
      [AP3], [Realisierung], [64], [48], [−16], [],
      [AP4], [Tests und Abschluss Realisierung], [24], [12], [−12], [],
      [AP5], [Dokumentation finalisieren], [24], [], [], [],
      [AP6], [Präsentation vorbereiten], [32], [], [], [],
      [], [*Total*], [*224*], [], [], [],
    ),
    caption: [Soll/Ist-Vergleich Aufwand (eigene Darstellung)]
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
    caption: [Lessons learned (eigene Darstellung)]
  ) <lessons_learned>
]
