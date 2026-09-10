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
      [API], [Application Programming Interface],
      [CI/CD], [Continuous Integration / Continuous Deployment],
      [D365], [Microsoft Dynamics 365],
      [ERD], [Entity Relationship Diagram],
      [GUI], [Graphical User Interface],
      [IDE], [Integrated Development Environment],
      [REST], [Representational State Transfer],
      [VSIX], [Visual Studio Extension],
    ),
    caption: [Abkürzungsverzeichnis]
  ) <abkuerzungsverzeichnis>
]

= Projektinitialisierung

== Ausgangslage

Bei der Entwicklung für Microsoft Dynamics 365 werden angezeigte Texte nicht direkt
im Code hinterlegt, sondern als Labels. Ein Label ist ein Platzhalter mit einer
eindeutigen ID, den die Anwendung zur Laufzeit durch die Übersetzung in der Sprache
des Benutzers ersetzt. Für jede unterstützte Sprache existiert eine eigene
Label-Datei.

Die von Microsoft mitgelieferte Verwaltung dieser Labels wurde bei BE-terna als
unzureichend beurteilt. Aus diesem Grund entstand intern ein eigenes Werkzeug, der
BE-LabelEditor. Es handelt sich um eine eigenständige Desktop-Anwendung, mit der
Labels gesucht, erstellt und bearbeitet werden können. Ebenso lassen sich die
Verwendungen eines Labels im Quellcode auffinden.

Die Entwicklung für Dynamics 365 findet in Visual Studio statt, weil Microsoft dort
das gesamte Tooling für die Plattform bereitstellt. Der BE-LabelEditor läuft daneben
als zweite Anwendung. Wer ein Label sucht oder anlegt, verlässt also die IDE,
erledigt die Arbeit im separaten Fenster und kehrt mit der kopierten Label-ID
zurück.

== Situationsanalyse (IST-Zustand)

Der bestehende BE-LabelEditor deckt den Lokalisierungsprozess funktional bereits
weitgehend ab. @ist_screenshot zeigt die Hauptansicht der aktuellen Version 1.8.3
mit einem synthetischen Demo-Datensatz.

#figure(
  image("../screenshots/GUI_full.png", width: 100%),
  caption: [BE-LabelEditor 1.8.3, Hauptansicht (eigene Darstellung)]
) <ist_screenshot>

Die Gruppe Tracing (Preview) im Ribbon ist eine interne Vorschaufunktion, die sich
bei BE-terna noch in Entwicklung befindet. Sie wird von dieser Arbeit ausgegrenzt.
Eine spätere Übernahme in die Extension bleibt möglich, sobald die Funktion intern
abgeschlossen ist.

Die folgende Übersicht fasst zusammen, was das Werkzeug heute leistet.

#[
  #show figure: set align(left)
  #figure(
    table(
      align: left,
      columns: (auto, 1fr),
      table.header(
        [*Funktion*], [*Beschreibung*],
      ),
      [Suche],
      [Acht Suchmodi. Exact match, Substring und Anything like that jeweils mit und
       ohne Beachtung der Gross- und Kleinschreibung, dazu Label id und MatchWord.
       Die Treffer erscheinen nach Relevanz sortiert in einer Liste.],
      [Detailansicht],
      [Zu einem ausgewählten Label werden die Übersetzungen aller konfigurierten
       Sprachen angezeigt, etwa de-CH, en-US, fr-CH und it-CH. Text und Kommentar
       sind direkt editierbar.],
      [Erstellen],
      [Ein neues Label wird im gewählten Model angelegt und in allen zu erstellenden
       Sprachen mit dem zuletzt eingegebenen Suchbegriff vorbelegt.],
      [Löschen, Kopieren, Verschieben],
      [Kopieren legt eine Kopie in einem Ziel-Model an, Verschieben löscht
       anschliessend das Original. Bestehende Referenzen im Code lassen sich dabei
       optional aktualisieren.],
      [Ersetzen],
      [Alle Verwendungen des markierten Labels werden im Code durch eine andere,
       bereits bestehende Label-ID ersetzt.],
      [Verwendungssuche],
      [Listet alle Fundstellen einer Label-ID mit Model, Datei, Zeile und Spalte
       auf.],
      [Apply],
      [Kopiert die vollständige Label-ID in die Zwischenablage und minimiert das
       Fenster, damit der Entwickler zurück nach Visual Studio wechseln kann.],
      [Einstellungen],
      [Auswahl der User-ID sowie Konfiguration der zu ladenden und der beim Erstellen
       anzulegenden Sprachen. Eine Änderung der geladenen Sprachen wird erst nach
       einem Neustart wirksam.],
      [Konsole],
      [Meldungsprotokoll mit den Kategorien Errors, Warnings und Messages.],
    ),
    caption: [Funktionsumfang des bestehenden BE-LabelEditors]
  ) <ist_funktionen>
]

#figure(
  image("../screenshots/search_options.png", width: 7cm),
  caption: [Auswahl der Suchmodi (eigene Darstellung)]
) <ist_suchmodi>

#figure(
  image("../screenshots/create_label.png", width: 7cm),
  caption: [Leiste zum Anlegen neuer Labels, ein Knopf je beschreibbares Model
            (eigene Darstellung)]
) <ist_erstellen>

#figure(
  image("../screenshots/find_references.png", width: 100%),
  caption: [Ergebnis der Verwendungssuche (eigene Darstellung)]
) <ist_verwendungssuche>

#figure(
  image("../screenshots/settings.png", width: 11cm),
  caption: [Einstellungsseite, im Demo-Datensatz ohne hinterlegte User-IDs
            (eigene Darstellung)]
) <ist_einstellungen>

#figure(
  image("../screenshots/console.png", width: 100%),
  caption: [Konsole beim Laden der Label-Dateien (eigene Darstellung)]
) <ist_konsole>

Aus dem Aufbau als eigenständige Anwendung ergeben sich mehrere Schwachstellen. Der
Wechsel zwischen IDE und Werkzeug unterbricht den Arbeitsfluss bei jedem
Lokalisierungsvorgang. Visual Studio zeigt beim Überfahren einer Label-ID mit der
Maus nur die englische Übersetzung an, die übrigen Sprachen bleiben verborgen. Eine
Suche lässt sich nicht aus dem Editor heraus starten. Hardcodierte Texte, die
eigentlich als Label hinterlegt sein müssten, muss der Entwickler von Hand
herauslösen und ersetzen. Neue Labels werden in allen Sprachen mit demselben
Ausgangstext vorbelegt, übersetzt wird anschliessend manuell.

== Aufgabenstellung

Das bestehende Werkzeug soll durch eine Extension für Visual Studio abgelöst werden.
Visual Studio ist dabei gesetzt, weil dort bereits das gesamte Tooling für Dynamics
365 liegt und die Entwickler ohnehin darin arbeiten.

Die Extension muss den Funktionsumfang des BE-LabelEditors vollständig übernehmen.
Dazu kommen die Möglichkeiten, die sich erst durch die Anbindung an die IDE
ergeben.

- Übersetzungen direkt im Code anzeigen und durchsuchen, statt nur die Label-ID zu
  sehen
- Übersetzungen neu erstellter Labels über einen externen Service automatisieren
- Hardcodierte Texte aus dem Code oder dem Eigenschaftsfenster extrahieren und durch
  ein übersetztes Label ersetzen

Auftraggeberin ist die BE-terna AG, mein aktueller Arbeitgeber. Die Arbeit gilt als
erfolgreich, wenn die Funktionalität des bestehenden Werkzeugs vollumfänglich
übernommen und um die genannten IDE-Funktionen erweitert wird.

Die eingereichte Themeneingabe liegt im Anhang.

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

Die Entwicklung erfolgt ausserhalb der Unternehmenssysteme auf einem privaten Gerät.
Für die Dauer des Projekts wird der Code über ein öffentliches GitHub-Repository
bereitgestellt. Nach Abschluss der Arbeit soll die Extension in die bestehende
CI/CD-Pipeline von BE-terna integriert werden.

Aus der öffentlichen Ablage ergeben sich zwei Auflagen. Der Quellcode des
bestehenden BE-LabelEditors ist Eigentum von BE-terna und wird nicht veröffentlicht.
Ebenso wenig veröffentlicht werden Quellcode und Metadaten der Standardanwendung von
Dynamics 365, die in der Entwicklungsumgebung zwar einsehbar sind, aber Microsoft
gehören. Das Vorgehen ist in @konfigurationsmanagement beschrieben.

=== Produktbezogene Rahmenbedingungen

Es werden keine kundenbezogenen Daten verwendet. Für Entwicklung und Tests dient ein
synthetischer Datensatz.

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
      [19.10. -- 21.10.2026], [Ferien, zusätzlich verfügbar],
    ),
    caption: [Verfügbarkeit im Projektzeitraum]
  ) <verfuegbarkeit>
]

Daraus ergeben sich im Zeitraum vom 04.09.2026 bis zur Abgabe am 02.11.2026
insgesamt 24 verfügbare Arbeitstage. Bei einem angenommenen Tagespensum von rund
acht Stunden entspricht dies einem Gesamtaufwand von etwa 192 Stunden und liegt
damit innerhalb der in den Richtlinien genannten Bandbreite von 150 bis 250 Stunden.

Durch die Ferien vom 19.10. bis 21.10.2026 entsteht mit dem 15.10. bis 24.10.2026
der längste zusammenhängende Arbeitsblock des Projekts. Er ist deshalb der
Realisierung zugeteilt, bei der ein durchgehendes Arbeiten den grössten Nutzen
bringt.

=== Terminplan (Soll)

#page(flipped: true, margin: (x: 2cm, y: 2cm))[
  #figure(
    image("../diagrams/Terminplan_Soll.png", height: 15cm),
    caption: [Soll-Terminplan (eigene Darstellung)]
  ) <terminplan_soll>
]

Schraffierte Bereiche im Terminplan kennzeichnen nicht verfügbare Tage. Die
Meilensteine M0, M1, M3 und M5 sind aus den Balken direkt ablesbar und daher nicht
zusätzlich eingezeichnet. Die Dokumentation läuft bewusst parallel zu den übrigen
Arbeitspaketen, damit die Ergebnisse direkt festgehalten werden und am Schluss
keine geschlossene Schreibphase notwendig ist.

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
      [AP3], [Realisierung], [15.10.], [21.10.], [6], [], [], [],
      [AP4], [Tests und Abschluss Realisierung], [22.10.], [24.10.], [3], [], [], [],
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
      [M5], [Code Freeze], [24.10.2026], [], [],
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

Weil das Repository öffentlich ist, gelten für den Inhalt die folgenden
Einschränkungen.

- Der Quellcode des bestehenden BE-LabelEditors wird nicht veröffentlicht. Er ist
  Eigentum von BE-terna und dient ausschliesslich als Referenz.
- Quellcode und Metadaten der Standardanwendung von Dynamics 365 werden nicht
  veröffentlicht. Sie sind in der Entwicklungsumgebung einsehbar, gehören aber
  Microsoft. Auf Standardobjekte wird über ihren Namen und die öffentliche
  Dokumentation von Microsoft verwiesen.
- Es werden keine kundenbezogenen Daten verwendet. Für Entwicklung, Tests und
  Screenshots dient ein synthetischer Datensatz.
- Zugangsdaten, API-Schlüssel, Umgebungs-URLs und Lizenzangaben werden nicht
  abgelegt.

Arbeitsstände, die nicht veröffentlicht werden sollen, bleiben ausserhalb des
Repositories in einem lokalen Ordner.
