#import "../helpers.typ": todo

= Anhang

== Aufgabenstellung / Themeneingabe <anhang_themeneingabe>

#todo[Themeneingabe vom 26.05.2026 hier einbinden. Die Richtlinien verlangen die
Abgabe in einer einzigen PDF-Datei, ein separates Dokument genügt nicht. Vorher
Adresse und Telefonnummer im öffentlichen Repo entfernen und die Screenshots auf interne
Model-Kürzel und Label-IDs prüfen, weil das Repository öffentlich ist.]

== Controlling-Berichte <controlling>

=== Bericht zu M1, Projektinitialisierung abgeschlossen

#[
  #show figure: set align(left)
  #figure(
    table(
      align: left,
      columns: (auto, 1fr, 1fr, 1.4fr),
      table.header(
        [*Bereich*], [*Soll*], [*Ist*], [*Beurteilung und Massnahme*],
      ),
      [Termin], [M1 am 26.09.2026], [], [],
      [Aufwand], [56 h für AP1], [], [],
      [Arbeitspakete], [AP1.1 bis AP1.7 abgeschlossen], [], [],
      [Risiken], [Bewertung aus @risikoanalyse], [], [],
      [Qualität], [Review der Initialisierung durchgeführt], [], [],
      [Nächste Schritte], [AP2 Konzept ab 01.10.2026], [], [],
    ),
    caption: [Controlling-Bericht M1 (eigene Darstellung)]
  ) <controlling_m1>
]

== Detailplanung und Aufwanderfassung

Der Soll-Aufwand beträgt pro Arbeitstag acht Stunden.

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
      [1.5], [24.09.], [Varianten, Machbarkeit, Variantenentscheid], [8], [16], [Fünf Variantenvergleiche, Machbarkeitsbeurteilung mit Prototypen-Spike und Laufzeitprobe auf der Testumgebung, alle Varianten entschieden, Befundprotokoll als Anhang. Auf den 11.09. und 14.09. vorgezogen, doppelter Aufwand wegen des Spikes], [Erledigt],
      [1.6], [25.09.], [Risikoanalyse, Qualitäts- und Konfigurationsmanagement], [8], [6], [Neun Risiken mit Matrix und Bewertungsschema, Qualitätsmanagement mit Massnahmen je Gegenstand. Konfigurationsmanagement bereits in AP1.2 erledigt. Englische Fachbegriffe im Kapitel vereinheitlicht. Auf den 24.09. vorgezogen], [Erledigt],
      [1.7], [26.09.], [Review Initialisierung], [8], [4], [Zwei Reviews gegen Themeneingabe und Richtlinien eingearbeitet. Terminplan überarbeitet, FA12 bis FA17 und NFA06 ergänzt, Abweichungen von der Themeneingabe erfasst, Gliederung nach Richtlinien umgestellt. Auf den 24.09. vorgezogen], [Erledigt],
      table.cell(colspan: 7)[*AP2 Konzept*],
      [2.1], [01.10.], [Kontextdiagramm, Geschäftsprozessanalyse, Detailanforderungen], [8], [], [], [],
      [2.2], [02.10.], [Use Cases, Sequenzdiagramme, Klassenmodell, Systemarchitektur], [8], [], [], [],
      [2.3], [08.10.], [Testkonzept, GUI-Design, Review Konzept], [8], [], [], [],
      table.cell(colspan: 7)[*AP3 Realisierung*],
      [3.1], [09.10.], [Grundgerüst, Build- und Test-Toolchain, Ausgabe im Output Window], [8], [], [], [],
      [3.2], [10.10.], [Parser, Laden der Label-Dateien, Dateiüberwachung (Stufe 1)], [8], [], [], [],
      [3.3], [15.10.], [Suche mit allen Suchmodi und Ranking (Stufe 1)], [8], [], [], [],
      [3.4], [16.10.], [Tool Window mit Trefferliste und Detailansicht (Stufe 1)], [8], [], [], [],
      [3.5], [17.10.], [Anlegen, Bearbeiten, Kopieren, Verschieben, Ersetzen, Label-ID einfügen, Einstellungen, Tastenkürzel (Stufe 1)], [8], [], [], [],
      [3.6], [19.10.], [QuickInfo, Inline-Anzeige, Inline-Suche (Stufe 2)], [8], [], [], [],
      [3.7], [20.10.], [Öffnen im Panel, Verwendungssuche mit Navigation (Stufe 2)], [8], [], [], [],
      [3.8], [21.10.], [Extraktion hardcodierter Texte, automatische Übersetzung (Stufe 3)], [8], [], [], [],
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
    caption: [Detailplanung und Aufwanderfassung Soll/Ist (eigene Darstellung)]
  ) <detailplanung>
]

#include "08_anhang_befundprotokoll.typ"

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
    caption: [Besprechungsprotokoll Vorzeigetermin 1 (eigene Darstellung)]
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
    caption: [Besprechungsprotokoll Vorzeigetermin 2 (eigene Darstellung)]
  ) <protokoll_2>
]
