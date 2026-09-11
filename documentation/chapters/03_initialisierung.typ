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

Erweiterungen für Dynamics 365 werden in Models ausgeliefert. Ein Model bündelt
Code, Metadaten und die zugehörigen Label-Dateien und ist die Einheit, die bei einem
Kunden installiert wird. Jedes Label gehört damit zu einer Label-Datei innerhalb
eines Models, was sich an der Label-ID ablesen lässt. Bei `@BDM1:BDM110000003` steht
`BDM1` für die Label-Datei und der Rest für das Label selbst. Nicht jedes Model ist
beschreibbar, Models von Microsoft oder von Drittanbietern sind schreibgeschützt.

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
      columns: (auto, 1fr, 1.7fr, auto),
      table.header(
        [*ID*], [*Ziel*], [*Messkriterium*], [*Reihen-\ folge*],
      ),
      [Z1], [Funktionale Parität zum bestehenden Werkzeug],
      [Alle in @ist_funktionen aufgeführten Funktionen stehen in der Extension zur
       Verfügung.],
      [1],

      [Z2], [Übersetzungen im Code einsehbar],
      [Zu einer Label-ID im Editor lassen sich alle konfigurierten Sprachen
       einsehen, ohne den Editor zu verlassen, und nicht mehr nur Englisch wie
       bisher.],
      [2],

      [Z3], [Suche aus dem Editor],
      [Eine Label-Suche lässt sich aus dem geöffneten Editor starten, ohne das
       Extension-Fenster von Hand zu öffnen.],
      [2],

      [Z4], [Bearbeiten aus dem Code heraus],
      [Ein im Code referenziertes Label lässt sich im Extension-Panel öffnen und
       bearbeiten. Die Änderung steht nach dem Speichern in der Label-Datei.],
      [2],

      [Z5], [Verwendungssuche in der IDE],
      [Die Suche liefert alle Fundstellen mit Model, Datei, Zeile und Spalte. Ein
       Klick auf einen Treffer springt an die Stelle im Code.],
      [2],

      [Z6], [Extraktion hardcodierter Texte],
      [Ein markierter Text wird auf Befehl als neues Label angelegt und die Stelle
       im Code durch die Label-ID ersetzt.],
      [3],

      [Z7], [Automatische Übersetzung],
      [Beim Anlegen eines Labels schlägt ein externer Dienst die Übersetzungen für
       alle konfigurierten Sprachen vor. Sie lassen sich vor dem Speichern ändern.],
      [3],

      [Z8], [Erweiterbare Architektur],
      [Die Kernlogik liegt in einer Komponente ohne Abhängigkeit zum Visual Studio
       SDK und ist ohne laufende IDE testbar.],
      [laufend],
    ),
    caption: [Projektziele]
  ) <projektziele>
]

Die Spalte Reihenfolge gibt die Abfolge der Umsetzung an. Stufe 1 ist die funktionale Parität, ohne die
der bestehende Editor nicht abgelöst ist. Stufe 2 baut darauf auf und beseitigt den
Kontextwechsel. Stufe 3 trägt die grösste technische Unsicherheit, weil die
Textextraktion und die Anbindung eines externen Dienstes am wenigsten erprobt sind.
Z8 gilt laufend. Die Einteilung regelt die Abfolge und nicht den Verzicht, denn die
Erfolgskriterien verlangen alle acht Ziele.


=== Nichtziele / Abgrenzung

Der Fokus der Arbeit liegt auf der Lokalisierung. Die folgenden Punkte sind
bewusst nicht Bestandteil des Projekts.

#[
  #show figure: set align(left)
  #figure(
    table(
      align: left,
      columns: (1fr, 1.4fr),
      table.header(
        [*Nicht Bestandteil der Arbeit*], [*Begründung*],
      ),
      [Tracing-Funktionen des bestehenden Werkzeugs],
      [Interne Vorschaufunktion, die bei BE-terna noch in Entwicklung ist. Eine
       Übernahme bleibt möglich, sobald sie dort abgeschlossen ist.],

      [Weitere Entwicklungsprozesse ausserhalb der Lokalisierung],
      [Die Architektur soll spätere Ergänzungen zulassen, umgesetzt werden sie in
       dieser Arbeit nicht.],

      [Integration in die CI/CD-Pipeline von BE-terna],
      [Erfolgt nach Abschluss der Arbeit.],

      [Produktive Einführung bei BE-terna],
      [Entscheid der Auftraggeberin nach Projektabschluss.],

      [Veröffentlichung des bestehenden BE-LabelEditors],
      [Der Quellcode ist Eigentum von BE-terna und dient ausschliesslich als
       Referenz.],

      [Betrieb des externen Übersetzungsdienstes],
      [Die Extension bindet einen bestehenden Dienst über dessen Schnittstelle an.
       Ein eigener Übersetzungsdienst wird nicht entwickelt.],
    ),
    caption: [Abgrenzung]
  ) <abgrenzung>
]

Welche weiteren Möglichkeiten die Extension-API eröffnet, ist noch nicht
vollständig untersucht. Diese Analyse gehört ins Konzept. Funktionen, die dabei
hinzukommen, sind Kann-Anforderungen und zählen nicht zu den Erfolgskriterien.

Ausser der Themeneingabe bestehen keine Vorarbeiten. Die Extension entsteht
vollständig im Rahmen dieser Arbeit. Der bestehende BE-LabelEditor dient als
fachliche Referenz. Er zeigt, welches Verhalten die Anwender gewohnt sind,
insbesondere beim Suchen und Anlegen von Labels. Ob die Verfahren gleich umgesetzt
oder überarbeitet werden, entscheidet sich im Konzept. Gerade bei der Suche ist eine
Überarbeitung wahrscheinlich, weil sich im Editor andere Möglichkeiten bieten als in
einem eigenständigen Fenster.

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

Die Zielplattform ist primär Visual Studio 2026. Visual Studio 2022 wird bald
abgelöst, deshalb findet die Entwicklung in 2026 statt. Bietet 2026
Erweiterungspunkte, die in 2022 fehlen, werden sie genutzt, auch wenn die Extension
dadurch unter 2022 nicht den vollen Funktionsumfang hat. Die Einbindung erfolgt über
das Visual Studio SDK als VSIX-Paket. Damit sind der Weg der Integration und die
verfügbaren Erweiterungspunkte vorgegeben.

In den Label-Prozess von Dynamics 365 lässt sich nicht eingreifen. Das Format der
Label-Dateien ist damit vorgegeben. Die Extension liest und schreibt diese Dateien
direkt auf dem Dateisystem und muss sie so hinterlassen, dass Dynamics 365 sie
weiterhin verwenden kann. Zusätzliche Daten lassen sich daneben ablegen, die
Label-Datei bleibt aber die Source of Truth.

Der externe Übersetzungsdienst wird über eine REST-Schnittstelle angebunden. Der
Anbieter soll konfigurierbar und austauschbar sein, damit die Extension nicht an
einen einzelnen Dienst gebunden ist.

Es werden keine kundenbezogenen Daten verwendet. Für Entwicklung und Tests dient ein
synthetischer Datensatz.

== Stakeholder-Analyse

Das Projekt betrifft ein internes Entwicklerwerkzeug. Der Kreis der Betroffenen ist
deshalb klein und liegt vollständig innerhalb von BE-terna. Externe Kunden sind
nicht betroffen, weil das Werkzeug die Entwicklung unterstützt und nicht in
ausgelieferte Lösungen eingreift.

#[
  #show figure: set align(left)
  #figure(
    table(
      align: left,
      columns: (auto, 1.4fr, auto, 1.4fr),
      table.header(
        [*Stakeholder*],
        [*Interesse am Projekt*],
        [*Einfluss*],
        [*Einbindung*],
      ),
      [BE-terna AG als Auftraggeberin],
      [Alle Entwickler sollen beim Lokalisieren schneller arbeiten.],
      [Hoch],
      [Gibt die Aufgabenstellung vor und entscheidet über die produktive Einführung
       nach Projektabschluss.],

      [Entwickler als Anwender],
      [Zeitersparnis beim Suchen und Anlegen von Labels sowie eine angenehmere
       Developer Experience durch den Wegfall des Kontextwechsels.],
      [Mittel],
      [Liefern Rückmeldungen zur Bedienung. Ihre Akzeptanz entscheidet darüber, ob
       die Extension das bestehende Werkzeug tatsächlich ablöst.],

      [Maintainer],
      [Die Architektur soll das Ergänzen weiterer Funktionen erlauben, ohne dass
       grundlegend umgebaut werden muss.],
      [Hoch],
      [Trifft die Architekturentscheide und verantwortet die Weiterentwicklung über
       die Diplomarbeit hinaus.],
    ),
    caption: [Stakeholderanalyse]
  ) <stakeholderanalyse>
]

Zwei der drei Rollen nehme ich selbst ein, einerseits als Entwickler, der das
Werkzeug täglich benutzt, andererseits als Maintainer, der es weiterentwickelt und
bereitstellt. Auch die Rolle des Firmenbetreuers liegt bei mir. Das verkürzt die
Abstimmungswege erheblich, birgt aber die Gefahr, dass Anforderungen aus meiner
eigenen Arbeitsweise heraus formuliert werden und nicht aus der Sicht der übrigen
Entwickler. Diesem Punkt wird in der Risikoanalyse Rechnung getragen.

== Grobe Anforderungen an das neue System

Die groben Anforderungen fassen zusammen, was die Extension leisten muss. Die Spalte
Ziel nennt das Projektziel, zu dem eine Anforderung gehört. Die Verfeinerung mit
Akzeptanzkriterien und Abhängigkeiten folgt in @detailanforderungen.

#[
  #show figure: set align(left)
  #set text(size: 10pt)
  #figure(
    table(
      align: left,
      columns: (auto, auto, 1fr, auto),
      table.header(
        [*ID*], [*Anforderung*], [*Beschreibung*], [*Ziel*],
      ),
      table.cell(colspan: 4)[*Funktionale Anforderungen*],
      [FA01], [Label-Suche],
      [Suchen anhand eines Suchbegriffs, mindestens Exact match, Substring und
       Label-ID, je mit und ohne Beachtung der Gross- und Kleinschreibung.], [Z1],
      [FA02], [Label-Erstellung],
      [Neue Labels anlegen, vorbelegt mit dem Suchbegriff in allen konfigurierten
       Sprachen.], [Z1],
      [FA03], [Label-Bearbeitung],
      [Bearbeiten, Löschen, Kopieren und Verschieben, mit Angabe eines Ziel-Models
       und optionaler Aktualisierung der Referenzen im Code.], [Z1],
      [FA04], [Verwendungssuche],
      [Alle Verwendungen eines Labels mit Model, Datei, Zeile und Spalte auffinden.
       Die Treffer sind anklickbar.], [Z1, Z5],
      [FA05], [Hover-Tooltip],
      [Beim Überfahren einer Label-ID alle konfigurierten Sprachübersetzungen
       anzeigen.], [Z2],
      [FA06], [Inline-Anzeige],
      [Die Übersetzung dauerhaft im Code einblenden, ein- und ausschaltbar.], [Z2],
      [FA07], [Inline-Suche],
      [Eine Label-Suche aus dem Editor starten, ohne das Extension-Fenster von Hand
       zu öffnen.], [Z3],
      [FA08], [Im Extension-Panel öffnen],
      [Ein im Code referenziertes Label im Panel öffnen und bearbeiten.], [Z4],
      [FA09], [Extraktion hardcodierter Texte],
      [Markierten Text als neues Label anlegen und die Stelle im Code durch die
       Label-ID ersetzen.], [Z6],
      [FA10], [Automatische Übersetzung],
      [Übersetzungen über einen externen Dienst vorschlagen. Sie sind vor dem
       Speichern prüf- und änderbar.], [Z7],
      [FA11], [Einstellungen],
      [Zu ladende und beim Erstellen anzulegende Sprachen konfigurieren,
       API-Schlüssel für externe Dienste hinterlegen.], [Z1, Z7],

      table.cell(colspan: 4)[*Nicht-funktionale Anforderungen*],
      [NFA01], [Kompatibilität],
      [Primär lauffähig unter Visual Studio 2026. Visual Studio 2022 wird
       unterstützt, soweit die verwendeten Erweiterungspunkte dort verfügbar sind.
       Spätere Versionen sollen nicht ausgeschlossen sein.], [--],
      [NFA02], [Performance],
      [Suchvorgänge in wenigen hundert Millisekunden. Das Laden der Label-Dateien
       blockiert den Editor nicht.], [--],
      [NFA03], [Erweiterbarkeit],
      [Weitere Features lassen sich ohne grundlegende Umstrukturierung
       ergänzen.], [Z8],
      [NFA04], [Stabilität],
      [Fehler innerhalb der Extension werden abgefangen und führen nicht zum
       Absturz von Visual Studio.], [--],
      [NFA05], [Wartbarkeit],
      [Der Code ist verständlich strukturiert und dokumentiert, sodass Dritte ihn
       weiterentwickeln können.], [Z8],

      table.cell(colspan: 4)[*Organisatorische Anforderungen*],
      [OA01], [Bereitstellung],
      [Die Extension liegt als installierbares VSIX-Paket vor und lässt sich ohne
       manuelle Nacharbeit auf einem Entwicklerrechner einrichten.], [--],
      [OA02], [Dokumentation],
      [Aufbau und Erweiterungspunkte sind so dokumentiert, dass eine andere Person
       die Weiterentwicklung übernehmen kann.], [Z8],
      [OA03], [Quellcodeablage],
      [Während des Projekts liegt der Code in einem öffentlichen Repository.
       Bestandteile der Standardanwendung von Dynamics 365 und des bestehenden
       BE-LabelEditors bleiben ausgeschlossen.], [--],
      [OA04], [Ablösung des bestehenden Werkzeugs],
      [Der Umstieg erfolgt erst, wenn die Extension den Funktionsumfang des
       BE-LabelEditors abdeckt. Bis dahin bleibt das bestehende Werkzeug in
       Gebrauch.], [Z1],
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
