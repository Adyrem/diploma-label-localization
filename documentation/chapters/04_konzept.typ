= Konzept

== Kontextdiagramm

// #figure(
//   image("../diagrams/kontextdiagramm.png", width: 100%),
//   caption: [Kontextdiagramm]
// ) <kontextdiagramm>

== Geschäftsprozessanalyse

// #figure(
//   image("../diagrams/geschaeftsprozess.png", width: 100%),
//   caption: [Geschäftsprozess]
// ) <geschaeftsprozess>

== Detailanforderungen an das neue System <detailanforderungen>

=== Funktionale Anforderungen

#[
  #show figure: set align(left)
  #figure(
    table(
      align: left,
      columns: (auto, 1fr, 2fr, auto),
      table.header(
        [*ID*],
        [*Anforderung*],
        [*Beschreibung*],
        [*Priorität*],
      ),
      [], [], [], [],
      [], [], [], [],
      [], [], [], [],
      [], [], [], [],
      [], [], [], [],
    ),
    caption: [Funktionale Anforderungen]
  ) <funktionale_anforderungen>
]

=== Nicht-funktionale Anforderungen

#[
  #show figure: set align(left)
  #figure(
    table(
      align: left,
      columns: (auto, 1fr, 2fr, auto),
      table.header(
        [*ID*],
        [*Anforderung*],
        [*Beschreibung*],
        [*Priorität*],
      ),
      [], [], [], [],
      [], [], [], [],
      [], [], [], [],
      [], [], [], [],
    ),
    caption: [Nicht-funktionale Anforderungen]
  ) <nicht_funktionale_anforderungen>
]

=== Organisatorische Anforderungen

#[
  #show figure: set align(left)
  #figure(
    table(
      align: left,
      columns: (auto, 1fr, 2fr, auto),
      table.header(
        [*ID*],
        [*Anforderung*],
        [*Beschreibung*],
        [*Priorität*],
      ),
      [], [], [], [],
      [], [], [], [],
      [], [], [], [],
    ),
    caption: [Organisatorische Anforderungen]
  ) <organisatorische_anforderungen>
]

== Use-Case-Diagramm

// #figure(
//   image("../diagrams/use_case_diagramm.png", width: 100%),
//   caption: [Use-Case-Diagramm]
// ) <use_case_diagramm>

== Use-Case-Beschreibungen

#[
  #show figure: set align(left)
  #figure(
    table(
      columns: (auto, 1fr),
      [*Name*], [],
      [*Nummer*], [UC-01],
      [*Kurzbeschreibung*], [],
      [*Stakeholder*], [],
      [*Fachverantwortliche Person*], [],
      [*Referenzen*], [],
      [*Vorbedingungen*], [],
      [*Nachbedingungen*], [],
      [*Typischer Ablauf*], [],
      [*Alternative Abläufe*], [],
      [*Kritikalität*], [],
      [*Verknüpfungen*], [],
      [*Funktionale Anforderungen*], [],
      [*Nicht-funktionale Anforderungen*], [],
    ),
    caption: [Use Case UC-01]
  ) <uc_01>
]

#[
  #show figure: set align(left)
  #figure(
    table(
      columns: (auto, 1fr),
      [*Name*], [],
      [*Nummer*], [UC-02],
      [*Kurzbeschreibung*], [],
      [*Stakeholder*], [],
      [*Fachverantwortliche Person*], [],
      [*Referenzen*], [],
      [*Vorbedingungen*], [],
      [*Nachbedingungen*], [],
      [*Typischer Ablauf*], [],
      [*Alternative Abläufe*], [],
      [*Kritikalität*], [],
      [*Verknüpfungen*], [],
      [*Funktionale Anforderungen*], [],
      [*Nicht-funktionale Anforderungen*], [],
    ),
    caption: [Use Case UC-02]
  ) <uc_02>
]

#[
  #show figure: set align(left)
  #figure(
    table(
      columns: (auto, 1fr),
      [*Name*], [],
      [*Nummer*], [UC-03],
      [*Kurzbeschreibung*], [],
      [*Stakeholder*], [],
      [*Fachverantwortliche Person*], [],
      [*Referenzen*], [],
      [*Vorbedingungen*], [],
      [*Nachbedingungen*], [],
      [*Typischer Ablauf*], [],
      [*Alternative Abläufe*], [],
      [*Kritikalität*], [],
      [*Verknüpfungen*], [],
      [*Funktionale Anforderungen*], [],
      [*Nicht-funktionale Anforderungen*], [],
    ),
    caption: [Use Case UC-03]
  ) <uc_03>
]

== Sequenzdiagramme

// #figure(
//   image("../diagrams/sequenz_01.png", width: 100%),
//   caption: [Sequenzdiagramm]
// ) <sequenz_01>

== Modellierung der Klassen

=== Klassendiagramm

// #figure(
//   image("../diagrams/class_diagram.png", width: 100%),
//   caption: [Klassendiagramm]
// ) <klassendiagramm>

=== Beschreibung der Fachklassen

#[
  #show figure: set align(left)
  #figure(
    table(
      align: left,
      columns: (1fr, 2fr, 1fr),
      table.header(
        [*Klasse*], [*Beschreibung*], [*Wichtige Attribute / Methoden*],
      ),
      [], [], [],
      [], [], [],
      [], [], [],
    ),
    caption: [Fachklassen]
  ) <fachklassen>
]

== Systemarchitektur

// #figure(
//   image("../diagrams/Architekturdiagramm.png", width: 100%),
//   caption: [Systemarchitektur]
// ) <systemarchitektur>

== Testkonzept

=== Vorgehen

=== Testobjekte

#[
  #show figure: set align(left)
  #figure(
    table(
      align: left,
      columns: (auto, 1fr, 1fr),
      table.header(
        [*ID*], [*Testobjekt*], [*Teststufe*],
      ),
      [], [], [],
      [], [], [],
      [], [], [],
    ),
    caption: [Testobjekte]
  ) <testobjekte>
]

=== Testfälle

#[
  #show figure: set align(left)
  #figure(
    table(
      align: left,
      columns: (auto, 1fr, 1fr, auto),
      table.header(
        [*ID*],
        [*Beschreibung*],
        [*Erwartetes Ergebnis*],
        [*Referenz*],
      ),
      [], [], [], [],
      [], [], [], [],
      [], [], [], [],
      [], [], [], [],
      [], [], [], [],
    ),
    caption: [Testfälle]
  ) <testfaelle>
]

== Einführung und Betrieb

// Einführungs-, Migrations- und Betriebskonzept bewusst in einem kurzen
// Abschnitt. Eine Datenmigration entfällt, weil die Label-Dateien unverändert
// bleiben. Die produktive Einführung entscheidet BE-terna nach Projektabschluss.

== GUI-Design
