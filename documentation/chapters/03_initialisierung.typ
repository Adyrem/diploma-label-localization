= Abkürzungsverzeichnis

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
      [], [],
      [], [],
      [], [],
    ),
    caption: [Abkürzungsverzeichnis]
  ) <abkuerzungsverzeichnis>
]

= Projektinitialisierung

== Ausgangslage

== Situationsanalyse (IST-Zustand)

== Aufgabenstellung

// Der Auftrag in eigenen Worten. Die formale Aufgabenstellung (Themeneingabe)
// und ein allfälliges Lastenheft liegen im Anhang.

== Zieldefinition

// Die Richtlinien nennen «Aufgabenstellung / Pflichtenheft / Zieldefinition» als
// eine Position. Der Pflichtenheft-Anteil wird hier durch die Ziele, die
// Abgrenzung, die groben Anforderungen sowie die Detailanforderungen im Kapitel
// Konzept abgedeckt. Wünscht die betreuende Person ein eigenständiges
// Pflichtenheft, kann an dieser Stelle ein Abschnitt «== Pflichtenheft» ergänzt
// werden.

=== Ziele

#[
  #show figure: set align(left)
  #figure(
    table(
      align: left,
      columns: (auto, 1fr, 1fr),
      table.header(
        [*ID*], [*Ziel*], [*Messkriterium*],
      ),
      [], [], [],
      [], [], [],
      [], [], [],
    ),
    caption: [Projektziele]
  ) <projektziele>
]

=== Nichtziele / Abgrenzung

== Rahmenbedingungen

=== Prozessbezogene Rahmenbedingungen

Die Arbeit entsteht im Umfeld von Microsoft Dynamics 365 und der Programmiersprache
X++. Quellcode und Dokumentation werden in einem öffentlichen Repository
versioniert. Daraus ergibt sich die Auflage, dass weder Bestandteile der
Standardanwendung von Microsoft noch Betriebsdaten des Arbeitgebers veröffentlicht
werden dürfen. Der Umfang der Arbeit beschränkt sich deshalb auf selbst erstellte
Erweiterungen. Das Vorgehen ist in @konfigurationsmanagement beschrieben.

=== Produktbezogene Rahmenbedingungen

== Stakeholder-Analyse

#[
  #show figure: set align(left)
  #figure(
    table(
      align: left,
      columns: (1fr, 1fr, auto),
      table.header(
        [*Stakeholder*],
        [*Interesse am Projekt*],
        [*Einfluss*],
      ),
      [], [], [],
      [], [], [],
      [], [], [],
      [], [], [],
    ),
    caption: [Stakeholderanalyse]
  ) <stakeholderanalyse>
]

== Grobe Anforderungen an das neue System

#[
  #show figure: set align(left)
  #figure(
    table(
      align: left,
      columns: (auto, 1fr, 1fr),
      table.header(
        [*ID*], [*Anforderung*], [*Beschreibung*],
      ),
      [], [], [],
      [], [], [],
      [], [], [],
    ),
    caption: [Grobe Anforderungen]
  ) <grobe_anforderungen>
]

== Lösungskonzept mit Varianten und Beurteilung

=== Varianten

#[
  #show figure: set align(left)
  #figure(
    table(
      align: left,
      columns: (1fr, 1fr, 1fr, auto),
      table.header(
        [*Kriterium*],
        [*Variante A*],
        [*Variante B*],
        [*Bewertung*],
      ),
      [], [], [], [],
      [], [], [], [],
      [], [], [], [],
      [], [], [], [],
    ),
    caption: [Variantenvergleich]
  ) <variantenvergleich>
]

=== Machbarkeitsbeurteilung

=== Variantenentscheid

== Projektmanagement

=== Projektorganisation

#[
  #show figure: set align(left)
  #figure(
    table(
      align: left,
      columns: (1fr, 1fr, 1fr),
      table.header(
        [*Rolle*], [*Person*], [*Aufgaben / Verantwortung*],
      ),
      [], [], [],
      [], [], [],
      [], [], [],
    ),
    caption: [Projektorganisation]
  ) <projektorganisation>
]

=== Projektplanung

=== Verfügbarkeit und Kapazität

Die Diplomarbeit wird berufsbegleitend erstellt. Von Montag bis Mittwoch steht die
Erwerbstätigkeit im Vordergrund, diese Tage werden nicht für die Diplomarbeit
eingeplant. Der Sonntag ist ebenfalls blockiert. Als Arbeitstage stehen damit
regulär Donnerstag, Freitag und Samstag zur Verfügung.

#[
  #show figure: set align(left)
  #figure(
    table(
      align: left,
      columns: (auto, 1fr),
      table.header(
        [*Zeitraum*], [*Verfügbarkeit*],
      ),
      [Montag -- Mittwoch], [Erwerbstätigkeit, erreichbar, keine Planung],
      [Donnerstag -- Samstag], [Reguläre Arbeitstage Diplomarbeit],
      [Sonntag], [Blockiert, erreichbar],
      [05.09. -- 06.09.2026], [Harter Blocker, nicht erreichbar],
      [17.09. -- 20.09.2026], [Harter Blocker, nicht erreichbar],
      [03.10. -- 04.10.2026], [Harter Blocker, nicht erreichbar],
      [26.10. -- 28.10.2026], [Ferien, zusätzlich verfügbar (Endspurt)],
    ),
    caption: [Verfügbarkeit im Projektzeitraum]
  ) <verfuegbarkeit>
]

Daraus ergeben sich im Zeitraum vom 04.09.2026 bis zur Abgabe am 02.11.2026
insgesamt 24 verfügbare Arbeitstage. Bei einem angenommenen Tagespensum von rund
acht Stunden entspricht dies einem Gesamtaufwand von etwa 192 Stunden und liegt
damit innerhalb der in den Richtlinien genannten Bandbreite von 150 bis 250 Stunden.

=== Terminplan (Soll)

#page(flipped: true, margin: (x: 2cm, y: 2cm))[
  #figure(
    image("../diagrams/Terminplan_Soll.png", height: 15cm),
    caption: [Soll-Terminplan (eigene Darstellung)]
  ) <terminplan_soll>
]

Schraffierte Bereiche im Terminplan kennzeichnen nicht verfügbare Tage. Die
Meilensteine M0, M1, M3 und M5 sind aus den Balken direkt ablesbar und daher nicht
zusätzlich eingezeichnet. Die
Dokumentation läuft bewusst parallel zu den übrigen Arbeitspaketen, damit die
Ergebnisse direkt festgehalten werden und am Schluss keine geschlossene
Schreibphase notwendig ist.

=== Arbeitspakete (Soll/Ist)

#[
  #show figure: set align(left)
  #figure(
    table(
      align: left,
      columns: (auto, 1fr, auto, auto, auto, auto, auto, auto),
      table.header(
        [*Nr.*],
        [*Arbeitspaket*],
        [*Soll-\ Start*],
        [*Soll-\ Ende*],
        [*Soll-\ Tage*],
        [*Ist-\ Start*],
        [*Ist-\ Ende*],
        [*Ab-\ weichung*],
      ),
      [AP0], [Dokumentation laufend], [04.09.], [31.10.], [--], [], [], [],
      [AP1], [Projektinitialisierung], [04.09.], [26.09.], [7], [], [], [],
      [AP2], [Konzept], [01.10.], [10.10.], [5], [], [], [],
      [AP3], [Realisierung], [15.10.], [24.10.], [6], [], [], [],
      [AP4], [Endspurt Realisierung und Test], [26.10.], [28.10.], [3], [], [], [],
      [AP5], [Dokumentation finalisieren und Review], [29.10.], [31.10.], [3], [], [], [],
      [AP6], [Präsentation vorbereiten], [05.11.], [12.11.], [4], [], [], [],
    ),
    caption: [Arbeitspakete Soll/Ist]
  ) <arbeitspakete_soll_ist>
]

=== Meilensteine (Soll/Ist)

#[
  #show figure: set align(left)
  #figure(
    table(
      align: left,
      columns: (auto, 1fr, auto, auto, auto),
      table.header(
        [*Nr.*],
        [*Meilenstein*],
        [*Soll-Termin*],
        [*Ist-Termin*],
        [*Abweichung*],
      ),
      [M0], [Start Diplomarbeit], [04.09.2026], [], [],
      [M1], [Projektinitialisierung abgeschlossen], [26.09.2026], [], [],
      [M2], [1. Vorzeigetermin Betreuung], [07.10.2026], [], [],
      [M3], [Konzept abgeschlossen], [10.10.2026], [], [],
      [M4], [2. Vorzeigetermin Betreuung (zu vereinbaren)], [22.10.2026], [], [],
      [M5], [Code Freeze], [28.10.2026], [], [],
      [M6], [Abgabe Diplomarbeit, 17.00 Uhr], [02.11.2026], [], [],
      [M7], [Präsentation], [13.11.2026], [], [],
    ),
    caption: [Meilensteine Soll/Ist]
  ) <meilensteine_soll_ist>
]

Die tagesgenaue Detailplanung mit der laufenden Aufwanderfassung befindet sich im
Anhang unter @detailplanung.

=== Risikoanalyse

#[
  #show figure: set align(left)
  #figure(
    table(
      align: left,
      columns: (auto, 1.1fr, 1.6fr, 3.2em, 3.2em, 3.2em, 1.6fr),
      table.header(
        [*ID*],
        [*Risiko*],
        [*Beschreibung*],
        [*Ein-\ tritt*],
        [*Aus-\ wir-\ kung*],
        [*Risi-\ ko-\ wert*],
        [*Massnahme*],
      ),
      [], [], [], [], [], [], [],
      [], [], [], [], [], [], [],
      [], [], [], [], [], [], [],
      [], [], [], [], [], [], [],
      [], [], [], [], [], [], [],
    ),
    caption: [Risikoanalyse]
  ) <risikoanalyse>
]

=== Risikomatrix

#[
  #show figure: set align(left)
  #figure(
    table(
      align: horizon + center,
      columns: (auto, 1fr, 1fr, 1fr, 1fr, 1fr),
      rows: (auto, 2.2em, 2.2em, 2.2em, 2.2em, 2.2em),
      table.header(
        table.cell(rowspan: 1, colspan: 1)[],
        table.cell(colspan: 5)[*Auswirkung*],
      ),
      [*Eintrittswahrscheinlichkeit*],
      [*1 -- sehr gering*],
      [*2 -- gering*],
      [*3 -- mittel*],
      [*4 -- hoch*],
      [*5 -- sehr hoch*],

      [*5 -- sehr hoch*], [], [], [], [], [],
      [*4 -- hoch*],      [], [], [], [], [],
      [*3 -- mittel*],    [], [], [], [], [],
      [*2 -- gering*],    [], [], [], [], [],
      [*1 -- sehr gering*], [], [], [], [], [],
    ),
    caption: [Risikomatrix]
  ) <risikomatrix>
]

#[
  #show figure: set align(left)
  #figure(
    table(
      align: left,
      columns: (auto, auto, 1fr),
      table.header(
        [*Risikowert*], [*Klassierung*], [*Umgang / Eskalation*],
      ),
      [], [], [],
      [], [], [],
      [], [], [],
    ),
    caption: [Bewertungsschema Risikomatrix]
  ) <risikomatrix_schema>
]

=== Qualitätsmanagement

=== Konfigurationsmanagement <konfigurationsmanagement>

Für die Versionskontrolle wird Git eingesetzt. Das Repository liegt öffentlich auf
GitHub unter #link("https://github.com/Adyrem/diploma-label-localization").
Versioniert werden der Quellcode, die Typst-Quellen der Dokumentation sowie die
PlantUML-Quellen der Diagramme. Generierte Artefakte wie das fertige PDF sind von
der Versionierung ausgenommen und werden bei Bedarf neu gebaut.

Änderungen werden in nachvollziehbaren Commits festgehalten, damit der Verlauf der
Arbeit für die Betreuung jederzeit einsehbar ist.

Die Arbeit entsteht im Umfeld von Microsoft Dynamics 365 und X++. Der Quellcode
und die Metadaten der Standardanwendung sind in der Entwicklungsumgebung zwar
einsehbar, bleiben aber Eigentum von Microsoft. Weil das Repository öffentlich ist,
gelten daraus abgeleitet die folgenden Einschränkungen.

- Quellcode und Metadaten der Standardanwendung werden nicht veröffentlicht. Im
  Repository liegen ausschliesslich selbst erstellte Erweiterungen.
- Auf Standardobjekte wird über ihren Namen und die öffentliche Dokumentation von
  Microsoft verwiesen, nicht über kopierten Quellcode.
- Betriebsdaten des Arbeitgebers werden nicht veröffentlicht. Für Beispiele und
  Screenshots dient der Demodatensatz Contoso.
- Zugangsdaten, Umgebungs-URLs und Lizenzangaben werden nicht abgelegt.

Arbeitsstände, die nicht veröffentlicht werden sollen, bleiben ausserhalb des
Repositories in einem lokalen Ordner.
