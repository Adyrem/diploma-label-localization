= Realisierung

== Programmierumgebung / Programmierrichtlinien

=== Programmierumgebung

#[
  #show figure: set align(left)
  #figure(
    table(
      align: left,
      columns: (1fr, auto, 1fr),
      table.header(
        [*Tool / Technologie*], [*Version*], [*Verwendungszweck*],
      ),
      [], [], [],
      [], [], [],
      [], [], [],
    ),
    caption: [Programmierumgebung]
  ) <programmierumgebung>
]

=== Projektstruktur

=== Programmierrichtlinien

=== Testbarkeit und Qualität

== Softwareaufbau

=== Modulstruktur

// #figure(
//   image("../diagrams/Moduluebersicht.png", width: 100%),
//   caption: [Modulübersicht]
// ) <moduluebersicht>

=== Geschäftslogik

=== Serviceschicht

== GUI-Implementierung

// #figure(
//   image("../screenshots/screenshot_01.png", width: 100%),
//   caption: [GUI]
// ) <gui_01>

== Datenzugriff und externe Anbindung

=== Designentscheidungen

#[
  #show figure: set align(left)
  #figure(
    table(
      align: left,
      columns: (1fr, 1fr, 1fr),
      table.header(
        [*Entscheidung*], [*Begründung*], [*Alternativen*],
      ),
      [], [], [],
      [], [], [],
      [], [], [],
    ),
    caption: [Designentscheidungen]
  ) <designentscheidungen>
]

=== Datenzugriff

=== Externe Systemanbindung

== Deployment / Inbetriebnahme

== Testprotokoll

=== Teststrategie

=== Testergebnisse

#[
  #show figure: set align(left)
  #figure(
    table(
      align: left,
      columns: (auto, 1fr, auto, auto),
      table.header(
        [*ID*], [*Beschreibung*], [*Status*], [*Referenz*],
      ),
      [], [], [], [],
      [], [], [], [],
      [], [], [], [],
      [], [], [], [],
      [], [], [], [],
    ),
    caption: [Übersicht Testergebnisse]
  ) <testergebnisse_uebersicht>
]

#[
  #show figure: set align(left)
  #figure(
    table(
      columns: (auto, 1fr),
      [*ID*], [TC-01],
      [*Beschreibung*], [],
      [*Vorgehen*], [],
      [*Erwartetes Ergebnis*], [],
      [*Tatsächliches Ergebnis*], [],
      [*Status*], [],
      [*Referenz*], [],
    ),
    caption: [Testergebnis TC-01]
  ) <tc_01>
]

#[
  #show figure: set align(left)
  #figure(
    table(
      columns: (auto, 1fr),
      [*ID*], [TC-02],
      [*Beschreibung*], [],
      [*Vorgehen*], [],
      [*Erwartetes Ergebnis*], [],
      [*Tatsächliches Ergebnis*], [],
      [*Status*], [],
      [*Referenz*], [],
    ),
    caption: [Testergebnis TC-02]
  ) <tc_02>
]

== Zielerreichung

#[
  #show figure: set align(left)
  #figure(
    table(
      align: left,
      columns: (auto, 1fr, auto, 1fr),
      table.header(
        [*ID*], [*Ziel*], [*Erreicht*], [*Bemerkung*],
      ),
      [], [], [], [],
      [], [], [], [],
      [], [], [], [],
    ),
    caption: [Zielerreichung]
  ) <zielerreichung>
]

== Empfehlungen / Ausblick
