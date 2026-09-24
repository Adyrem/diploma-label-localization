#import "../helpers.typ": todo

= Projektinitialisierung <initialisierung>

== Ausgangslage

Bei der Entwicklung für Microsoft Dynamics 365 werden angezeigte Texte nicht direkt
im Code hinterlegt, sondern als Labels. Ein Label ist ein Platzhalter mit einer
eindeutigen ID, den die Anwendung zur Laufzeit durch die Übersetzung in der Sprache
des Benutzers ersetzt. Labels sind in Label-Dateien zusammengefasst. Eine
Label-Datei wie `BDM1` ist eine logische Einheit, abgelegt wird sie als eine
physische Datei pro Sprache.

Erweiterungen für Dynamics 365 werden in Models ausgeliefert. Ein Model bündelt
Code, Metadaten und die zugehörigen Label-Dateien und ist die Einheit, die bei einem
Kunden installiert wird. Jedes Label gehört damit zu einer Label-Datei innerhalb
eines Models, was sich an der Label-ID ablesen lässt. Bei `@BDM1:BDM110000003` steht
`BDM1` für die Label-Datei und der Rest für das Label selbst. Nicht jedes Model ist
beschreibbar, Models von Microsoft oder von Drittanbietern sind schreibgeschützt.

#figure(
  image("../diagrams/Labelstruktur.png", width: 7.5cm),
  caption: [Model, Label-Datei, Label und Übersetzung (eigene Darstellung)]
) <labelstruktur>

Die von Microsoft mitgelieferte Verwaltung dieser Labels wurde bei BE-terna als
unzureichend beurteilt. Aus diesem Grund entstand intern ein eigenes Tool, der
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
       Fenster, damit der Entwickler zurück nach Visual Studio wechseln kann. Save
       and apply speichert vorher die Änderungen.],
      [Einstellungen],
      [Auswahl der User-ID sowie Konfiguration der zu ladenden und der beim Erstellen
       anzulegenden Sprachen. Eine Änderung der geladenen Sprachen wird erst nach
       einem Neustart wirksam.],
      [Konsole],
      [Meldungsprotokoll mit den Kategorien Errors, Warnings und Messages.],
      [Dateiüberwachung],
      [Ändert sich eine beschreibbare Label-Datei von aussen, bietet das Tool an, die
       Label-Dateien neu zu laden.],
      [Tastenkürzel],
      [Strg+S speichert, Strg+Shift+A führt Apply aus, Strg+Shift+S beides
       nacheinander. Strg+N legt ein neues Label mit dem letzten Suchbegriff an. Die
       Kürzel lassen sich nicht ändern.],
    ),
    caption: [Funktionsumfang des bestehenden BE-LabelEditors (eigene Darstellung)]
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

@kontextwechsel zeigt den heutigen Ablauf, wenn im Code ein Label gebraucht wird.

#figure(
  image("../diagrams/Kontextwechsel.png", width: 100%),
  caption: [Ablauf beim Suchen oder Anlegen eines Labels (eigene Darstellung)]
) <kontextwechsel>

Aus dem Aufbau als eigenständige Anwendung ergeben sich mehrere Schwachstellen. Der
Wechsel zwischen IDE und Tool unterbricht den Arbeitsfluss bei jedem
Lokalisierungsvorgang. Visual Studio zeigt beim Überfahren einer Label-ID mit der
Maus nur die englische Übersetzung an, die übrigen Sprachen bleiben verborgen. Eine
Suche lässt sich nicht aus dem Editor heraus starten. Hardcodierte Texte, die
eigentlich als Label hinterlegt sein müssten, meldet der Best-Practice-Check beim
Kompilieren bereits heute. Herauslösen und ersetzen muss der Entwickler sie aber
von Hand. Neue Labels werden in allen Sprachen mit demselben
Ausgangstext vorbelegt, übersetzt wird anschliessend manuell.

== Aufgabenstellung

Das bestehende Tool soll durch eine Extension für Visual Studio abgelöst werden.
Visual Studio ist dabei gesetzt, weil dort bereits das gesamte Tooling für Dynamics
365 liegt und die Entwickler ohnehin darin arbeiten.

Die Extension muss den Funktionsumfang des BE-LabelEditors vollständig übernehmen.
Dazu kommen die Möglichkeiten, die sich erst durch die Anbindung an die IDE
ergeben.

- Übersetzungen direkt im Code anzeigen und durchsuchen, statt nur die Label-ID zu
  sehen
- Übersetzungen neu erstellter Labels über einen externen Service automatisieren
- Hardcodierte Texte aus dem Code oder dem Properties Window extrahieren und durch
  ein übersetztes Label ersetzen

Auftraggeberin ist die BE-terna AG, mein aktueller Arbeitgeber. Die Arbeit gilt als
erfolgreich, wenn die Funktionalität des bestehenden Tools vollumfänglich
übernommen und um die genannten IDE-Funktionen erweitert wird.

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
      [Z1], [Feature Parity zum bestehenden Tool],
      [Alle in @ist_funktionen aufgeführten Funktionen stehen in der Extension
       gleichwertig zur Verfügung. Was gleichwertig heisst, legen FA01 bis FA04
       und FA11 bis FA17 fest.],
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
      [Ein markierter Text im Editor oder Properties Window wird auf Befehl als neues Label angelegt. Die Stelle
       wird dabei durch die Label-ID ersetzt.],
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
    caption: [Projektziele (eigene Darstellung)]
  ) <projektziele>
]

Umgesetzt wird in drei Stufen. Stufe 1 ist die Feature Parity, ohne die der
bestehende Editor nicht abgelöst ist. Stufe 2 baut darauf auf und beseitigt den
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
      [Tracing-Funktionen des bestehenden Tools],
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

      [Auswahl der User-ID],
      [Wird im bestehenden Tool vollständig zurückgebaut und ist nicht mehr
       nötig.],
    ),
    caption: [Abgrenzung (eigene Darstellung)]
  ) <abgrenzung>
]

Welche weiteren Möglichkeiten die Extension-API eröffnet, ist noch nicht
vollständig untersucht. Diese Analyse gehört ins Konzept. Funktionen, die dabei
hinzukommen, sind Kann-Anforderungen und zählen nicht zu den Erfolgskriterien.

Ausser der Themeneingabe bestehen keine Vorarbeiten. Die Extension entsteht
vollständig im Rahmen dieser Arbeit. Der bestehende BE-LabelEditor dient als
fachliche Referenz. Er zeigt, welches Verhalten die Anwender gewohnt sind,
insbesondere beim Suchen und Anlegen von Labels. Sein Code wird nicht übernommen.
Die Verfahren werden analysiert und dort überarbeitet, wo sich Verbesserungen
anbieten. Gerade bei der Referenzsuche ist das wahrscheinlich, weil sich im Editor andere
Möglichkeiten bieten als in einem eigenständigen Fenster.

== Rahmenbedingungen

=== Prozessbezogene Rahmenbedingungen

Die Entwicklung erfolgt ausschliesslich auf einem privaten Gerät und ausserhalb der
Unternehmenssysteme. Die im Projekt eingesetzten KI-Tools dürfen auf den Systemen
des Arbeitgebers nicht betrieben werden, und diese Systeme werden auch anderweitig
genutzt. Auf dem privaten Gerät steht keine Umgebung von Dynamics 365 zur
Verfügung, nachgestellt wird sie durch einen synthetischen Datensatz. Eine
Testumgebung des Arbeitgebers darf verwendet werden, um die Extension auszuführen
und zu beobachten. Entwickelt wird darauf nicht. Einzelne Assemblies lassen
sich auf das private Gerät kopieren, ein vollständiges Abbild der Umgebung nicht.

Für die Dauer des Projekts wird der Code über ein öffentliches GitHub-Repository
bereitgestellt. Nach Abschluss der Arbeit soll die Extension in die bestehende
CI/CD-Pipeline von BE-terna integriert werden.

Aus der öffentlichen Ablage ergeben sich zwei Auflagen. Der Quellcode des
bestehenden BE-LabelEditors ist Eigentum von BE-terna und wird nicht veröffentlicht.
Ebenso wenig veröffentlicht werden Quellcode und Metadaten der Standardanwendung von
Dynamics 365, die mit jeder Installation der Developer Tools einsehbar sind, aber
Microsoft gehören. Das Vorgehen ist in @konfigurationsmanagement beschrieben.

=== Produktbezogene Rahmenbedingungen

Entwickelt wird für Visual Studio 2026, getestet zusätzlich unter Visual Studio
2022. Bietet 2026 Erweiterungspunkte, die in 2022 fehlen, werden sie genutzt, auch
wenn die Extension dadurch unter 2022 nicht den vollen Funktionsumfang hat. Fehler,
die nur unter 2022 auftreten, werden mit tiefer Priorität behoben. Die Extension wird als VSIX-Paket
ausgeliefert. Damit sind der Weg der Integration und die
verfügbaren Erweiterungspunkte vorgegeben.

In den Label-Prozess von Dynamics 365 lässt sich nicht eingreifen. Das Format der
Label-Dateien ist damit vorgegeben. Die Extension liest und schreibt diese Dateien
direkt auf dem Dateisystem und muss sie so hinterlassen, dass Dynamics 365 sie
weiterhin verwenden kann. Zusätzliche Daten lassen sich daneben ablegen, die
Label-Datei bleibt aber die Source of Truth.

Der externe Übersetzungsdienst wird über eine REST-Schnittstelle angebunden. Der
Anbieter soll konfigurierbar und austauschbar sein, damit die Extension nicht an
einen einzelnen Dienst gebunden ist. BE-terna erlaubt, Label-Texte dafür an einen
externen Dienst zu senden.

Es werden keine kundenbezogenen Daten verwendet. Für Entwicklung und Tests dient ein
synthetischer Datensatz.

== Stakeholder-Analyse

Das Projekt betrifft ein internes Tool für Entwickler. Der Kreis der Betroffenen ist
deshalb klein, ausser der Schule liegen alle Stakeholder innerhalb von BE-terna. Externe Kunden sind
nicht betroffen, weil das Tool die Entwicklung unterstützt und nicht in
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
       die Extension das bestehende Tool tatsächlich ablöst.],

      [Maintainer],
      [Die Architektur soll das Ergänzen weiterer Funktionen erlauben, ohne dass
       grundlegend umgebaut werden muss.],
      [Hoch],
      [Trifft die Architekturentscheide und verantwortet die Weiterentwicklung über
       die Diplomarbeit hinaus.],

      [TEKO mit Betreuung],
      [Eine Diplomarbeit, die den Richtlinien entspricht und die Kompetenzen der
       Fachrichtung belegt.],
      [Hoch],
      [Begleitet die Arbeit an zwei Vorzeigeterminen und bewertet Dokumentation,
       Ergebnis und Präsentation.],

      [Experte],
      [Eine fachlich nachvollziehbare Arbeit, deren Ergebnis sich im Unternehmen
       einsetzen lässt.],
      [Hoch],
      [Entwickler bei BE-terna. Bewertet die Arbeit zusammen mit der Betreuung und
       nimmt die Präsentation ab.],
    ),
    caption: [Stakeholderanalyse (eigene Darstellung)]
  ) <stakeholderanalyse>
]

Drei der fünf Rollen nehme ich selbst ein, einerseits als Entwickler, der das
Tool täglich benutzt, andererseits als Maintainer, der es weiterentwickelt und
bereitstellt. Auch die Rolle des Firmenbetreuers liegt bei mir. Das verkürzt die
Abstimmungswege erheblich, birgt aber die Gefahr, dass Anforderungen aus meiner
eigenen Arbeitsweise heraus formuliert werden und nicht aus der Sicht der übrigen
Entwickler. Dagegen wird die Arbeit einem Experten im Unternehmen vorgezeigt,
sobald erste Teile des Konzepts stehen. Der Punkt steht ausserdem als R09 in der
Risikoanalyse.

== Grobe Anforderungen an das neue System

Jede Anforderung ist einem Projektziel zugeordnet. Verfeinert werden sie mit
Akzeptanzkriterien und Abhängigkeiten in @detailanforderungen.

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
      [Hardcodierten Text im Editor oder im Properties Window als neues Label
       anlegen und durch die Label-ID ersetzen.], [Z6],
      [FA10], [Automatische Übersetzung],
      [Übersetzungen über einen externen Dienst vorschlagen. Sie sind vor dem
       Speichern prüf- und änderbar.], [Z7],
      [FA11], [Einstellungen],
      [Zu ladende und beim Erstellen anzulegende Sprachen konfigurieren,
       API-Schlüssel für externe Dienste hinterlegen.], [Z1, Z7],
      [FA12], [Erweiterte Suchmodi],
      [Zusätzlich zu FA01 die Suchmodi Anything like that mit und ohne Beachtung
       der Gross- und Kleinschreibung sowie MatchWord. Die Treffer erscheinen nach
       Relevanz sortiert.], [Z1],
      [FA13], [Label ersetzen],
      [Alle Verwendungen eines Labels im Code durch eine andere, bestehende
       Label-ID ersetzen.], [Z1],
      [FA14], [Label-ID einfügen],
      [Die Label-ID des gewählten Labels an der Cursorposition im Editor einfügen,
       wahlweise nach dem Speichern. Entspricht Apply und Save and apply.], [Z1],
      [FA15], [Dateiüberwachung],
      [Änderungen an beschreibbaren Label-Dateien von aussen erkennen und das
       Neuladen anbieten.], [Z1],
      [FA16], [Tastenkürzel],
      [Speichern, Label-ID einfügen, beides kombiniert und neues Label anlegen über
       Tastenkürzel auslösen, ohne mit Kürzeln von Visual Studio zu kollidieren.],
      [Z1],
      [FA17], [Meldungsprotokoll],
      [Fehler, Warnungen und Meldungen der Extension im Output Window von Visual
       Studio ausgeben.], [Z1],

      table.cell(colspan: 4)[*Nicht-funktionale Anforderungen*],
      [NFA01], [Kompatibilität],
      [Entwickelt für Visual Studio 2026, getestet auch unter Visual Studio 2022.
       Fehler, die nur unter 2022 auftreten, haben tiefe Priorität. Spätere
       Versionen sollen nicht ausgeschlossen sein.], [--],
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
      [NFA06], [Sicherheit],
      [API-Schlüssel werden nicht im Klartext abgelegt und gelangen nicht ins
       Repository.], [--],

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
      [OA04], [Ablösung des bestehenden Tools],
      [Der Umstieg erfolgt erst, wenn die Extension den Funktionsumfang des
       BE-LabelEditors abdeckt. Bis dahin bleibt das bestehende Tool in
       Gebrauch.], [Z1],
    ),
    caption: [Grobe Anforderungen (eigene Darstellung)]
  ) <grobe_anforderungen>
]

== Lösungskonzept mit Varianten und Beurteilung

=== Varianten <varianten>

Die Wahl der IDE ist durch die Aufgabenstellung vorgegeben. Offen sind fünf
technische Entscheidungen, die den Aufbau der Extension prägen.

Mehrere Varianten betreffen die Erweiterungspunkte des Editors von Visual Studio.
QuickInfo ist das Fenster, das beim Überfahren mit der Maus erscheint. Ein
Tagger verknüpft Textbereiche mit Zusatzinformationen, auf die andere Funktionen
aufbauen. CodeLens blendet Angaben oberhalb einer Codezeile ein. Welche dieser
Erweiterungen in einer Datei greifen, entscheidet der Content Type, also die
Kennzeichnung, mit der Visual Studio den Inhalt einer Datei einordnet
@ms-editor-extension-points @ms-editor-extensibility. Über das Selection Tracking
erfährt eine Extension zudem, welches Element im Designer gerade gewählt ist.

@demo_inline und @demo_quickinfo zeigen, wie die beiden Anzeigen aussehen
könnten. Beide Abbildungen beruhen auf erfundenen Beispieldaten und nicht auf einem
Lauf gegen echte Label-Dateien.

#figure(
  image("../screenshots/demo-codelens-uebersetzungen.png", width: 100%),
  caption: [Einblendung der Übersetzungen oberhalb der Label-Zeile, Demonstration
            mit Beispieldaten (eigene Darstellung)]
) <demo_inline>

#figure(
  image("../screenshots/demo-quickinfo-uebersetzungen.png", width: 100%),
  caption: [Tooltip mit allen konfigurierten Sprachen, Demonstration mit
            Beispieldaten (eigene Darstellung)]
) <demo_quickinfo>

#heading(outlined: false, level: 4)[V1 Zugriff auf die Label-Dateien]

Microsoft liefert mit den Developer Tools eine Metadata-API aus. Das ist eine
Sammlung von Assemblies, über die sich die Elemente eines Models lesen und schreiben
lassen, ohne die Dateien selbst zu kennen. Ein Disk-Provider richtet diesen Zugriff
auf das Package-Verzeichnis, also den Ordner, in dem die lokale Installation ihre
Models ablegt. @verzeichnisstruktur zeigt den Aufbau dieses Ordners.

#figure(
  image("../diagrams/Verzeichnisstruktur.png", width: 9cm),
  caption: [Ablage der Label-Dateien im Package-Verzeichnis (eigene Darstellung)]
) <verzeichnisstruktur>

#[
  #show figure: set align(left)
  #set text(size: 10pt)
  #figure(
    table(
      align: left,
      columns: (auto, 1fr, 1fr),
      table.header(
        [*Kriterium*],
        [*A Direkter Dateizugriff*],
        [*B Metadata-API von Microsoft*],
      ),
      [Vorgehen],
      [Models über die Descriptor-Dateien finden, Label-Dateien selbst parsen und
       schreiben. So arbeitet der bestehende BE-LabelEditor.],
      [Zugriff über `MetadataProviderFactory` und einen Disk-Provider auf das
       Package-Verzeichnis @meyer-labels-net.],
      [Abhängigkeiten],
      [Keine. Die Kernlogik läuft ohne installierte Developer Tools.],
      [Bindet die Extension an die Assemblies der lokalen D365-Installation.],
      [Formatänderungen],
      [Muss selbst nachgezogen werden, wenn Microsoft das Format ändert.],
      [Werden von der API abgefangen.],
      [Testbarkeit],
      [Unit Tests gegen Beispieldateien möglich, ohne D365.],
      [Tests brauchen die Assemblies und ein Package-Verzeichnis.],
      [Erfahrung],
      [Das Verfahren ist im bestehenden Tool erprobt.],
      [Neu zu erarbeiten.],
      [Target Framework],
      [Frei wählbar.],
      [Zwingend .NET Framework 4.8, die Assemblies lassen sich unter 4.7.2 nicht
       referenzieren.],
      [Geschwindigkeit],
      [Beim ersten Zugriff deutlich schneller.],
      [Beim ersten Zugriff langsamer wegen des Startaufwands, danach gleich
       schnell.],
      [Parsen der Labels],
      [Eigene Arbeit.],
      [Ebenfalls eigene Arbeit, die API liefert den Dateiinhalt unverarbeitet.],
    ),
    caption: [Variantenvergleich Zugriff auf die Label-Dateien (eigene Darstellung)]
  ) <variante_dateizugriff>
]

#heading(outlined: false, level: 4)[V2 Zusätzliches Add-in]

Die Extension wird als VSIX-Paket ausgeliefert und klinkt sich über die
Erweiterungspunkte von Visual Studio ein. Für die Developer Tools von
Dynamics 365 besteht daneben ein eigenes Add-in-Modell @ms-addins. Ein Add-in ist
keine VSIX-Datei, sondern eine Klassenbibliothek, die in den Installationsordner
dieser Tools kopiert und von ihnen geladen wird. Zu entscheiden ist, ob ein
solches Add-in zusätzlich gebaut wird.

#[
  #show figure: set align(left)
  #set text(size: 10pt)
  #figure(
    table(
      align: left,
      columns: (auto, 1fr, 1fr),
      table.header(
        [*Kriterium*],
        [*A Nur die Extension*],
        [*B Extension und Add-in*],
      ),
      [Zugang],
      [Alles, was Visual Studio bietet, also Anzeigen im Editor, eigene Fenster
       und das Selection Tracking im Designer.],
      [Zusätzlich zwei Menüeinträge, im Menü Dynamics 365 und im Kontextmenü des
       Element-Designers.],
      [Auslieferung],
      [Ein VSIX-Paket.],
      [Zusätzlich eine Bibliothek, die in den Installationsordner der
       Developer Tools kopiert wird.],
      [Abhängigkeit],
      [Keine zu den Developer Tools.],
      [Das Add-in wird von diesen geladen und hängt an ihrer Version.],
    ),
    caption: [Variantenvergleich Add-in (eigene Darstellung)]
  ) <variante_anbindung>
]

#heading(outlined: false, level: 4)[V3 Ablage der Label-Daten zur Laufzeit]

#[
  #show figure: set align(left)
  #set text(size: 10pt)
  #figure(
    table(
      align: left,
      columns: (auto, 1fr, 1fr, 1fr),
      table.header(
        [*Kriterium*],
        [*A Direkt aus den Dateien*],
        [*B Einmal in den Arbeitsspeicher*],
        [*C Eigener Index*],
      ),
      [Vorgehen],
      [Jede Suche liest die Label-Dateien.],
      [Beim Start einmal laden, danach im Speicher suchen. So arbeitet der
       bestehende BE-LabelEditor.],
      [Zusätzliche Ablage in einer Datenbank oder einem eigenen Format, inkrementell
       nachgeführt.],
      [Startzeit], [Keine], [Rund zwei Sekunden mit dem synthetischen Datensatz, siehe @ist_konsole], [Einmalig
       hoch, danach gering],
      [Suchgeschwindigkeit], [Gering], [Hoch], [Hoch],
      [Metadaten], [Nicht möglich], [Nur flüchtig], [Dauerhaft speicherbar],
      [Aktualität], [Immer aktuell], [Braucht eine Überwachung der Dateien],
      [Braucht Überwachung und Abgleich],
      [Aufwand], [Gering], [Mittel], [Hoch],
    ),
    caption: [Variantenvergleich Ablage der Label-Daten (eigene Darstellung)]
  ) <variante_ablage>
]

#heading(outlined: false, level: 4)[V4 Extension-Modell]

Visual Studio bietet drei Wege, eine Extension zu bauen @ms-extensibility-models
@ms-inproc-extensions. Sie unterscheiden sich im Target Framework, im Zugriff auf die
bestehenden Dienste und darin, was ein Fehler in der Extension anrichtet.

Das VSSDK ist das ursprüngliche Erweiterungspaket
von Visual Studio und gibt Zugriff auf alle internen Dienste. MEF steht für Managed
Extensibility Framework und ist der Mechanismus, über den Visual Studio
Erweiterungen zur Laufzeit einsammelt und einbindet. Die eingangs beschriebenen
Editor-Erweiterungen melden sich über MEF an, weshalb der Zugriff darauf die Voraussetzung für Hover und
Inline-Anzeige ist.

#[
  #show figure: set align(left)
  #set text(size: 9.5pt)
  #figure(
    table(
      align: left,
      columns: (auto, 1fr, 1fr, 1fr),
      table.header(
        [*Kriterium*],
        [*A VSSDK, in-process*],
        [*B VS.Extensibility, out-of-process*],
        [*C VS.Extensibility, in-process*],
      ),
      [Target Framework], [.NET Framework], [.NET 8], [.NET Framework],
      [VSSDK und MEF], [Voller Zugriff], [Kein Zugriff], [Voller Zugriff],
      [Fehler in der Extension],
      [Kann Visual Studio mitreissen],
      [Bleibt auf die Extension beschränkt],
      [Kann Visual Studio mitreissen],
      [Tooltip am Token für FA05],
      [Über MEF möglich],
      [Nicht möglich. Ein Tooltip lässt sich nur an ein CodeLens-Label hängen,
       nicht an beliebigen Text],
      [Über MEF möglich],
      [Einblendung im Code für FA06],
      [Freie Einblendung über MEF],
      [Nur über CodeLens oder die Margin, keine freie Einblendung],
      [Freie Einblendung über MEF],
      [Reifegrad der benötigten API],
      [Stabil],
      [Tagger und CodeLens sind als Vorschau markiert],
      [Stabil],
      [D365-Add-in-Modell], [Möglich], [Nicht möglich], [Möglich],
      [Empfehlung von Microsoft],
      [Weiterhin unterstützt, für neue Extensions nicht mehr erste Wahl],
      [Für neue Extensions ohne Bedarf an VSSDK-Diensten],
      [Für neue Extensions mit Bedarf an VSSDK-Diensten],
    ),
    caption: [Variantenvergleich Extension-Modell (eigene Darstellung)]
  ) <variante_extensionmodell>
]

#figure(
  image("../diagrams/Extensionmodelle.png", width: 100%),
  caption: [Prozessgrenze der drei Extension-Modelle (eigene Darstellung)]
) <extensionmodelle>

Die Entscheidung steht im Zielkonflikt mit NFA04. Ein Fehler in der Extension soll
Visual Studio nicht zum Absturz bringen, was für den out-of-process-Betrieb
spricht. QuickInfo und die Anbindung an das D365-Add-in-Modell verlangen aber
Zugriff auf VSSDK und MEF und damit den Betrieb im selben Prozess.

#heading(outlined: false, level: 4)[V5 Verfahren der Verwendungssuche]

FA04 verlangt Fundstellen mit Model, Datei, Zeile und Spalte. Dynamics 365 führt
dafür eine Cross-Reference-Datenbank, die beim Build mit der entsprechenden Option
gefüllt wird und die Visual Studio für die eigene Referenzsuche nutzt
@saxblog-xref.

#[
  #show figure: set align(left)
  #set text(size: 9.5pt)
  #figure(
    table(
      align: left,
      columns: (auto, 1fr, 1fr, 1fr),
      table.header(
        [*Kriterium*],
        [*A Textsuche in den XML-Dateien*],
        [*B Cross-Reference-Datenbank*],
        [*C Metadata-API durchlaufen*],
      ),
      [Vorgehen],
      [Suchpfade nach der Label-ID durchsuchen. So arbeitet der bestehende
       BE-LabelEditor.],
      [Abfrage der beim Build erzeugten Referenzdaten.],
      [Alle Elemente über die API durchlaufen und auf Label-Verweise prüfen.],
      [Genauigkeit],
      [Findet jedes Vorkommen im Text, auch in Kommentaren.],
      [Semantisch korrekt, dieselbe Grundlage wie die Referenzsuche der IDE.],
      [Semantisch korrekt.],
      [Aktualität],
      [Immer aktuell.],
      [Nur so aktuell wie der letzte Build mit Referenzdaten.],
      [Immer aktuell.],
      [Voraussetzungen],
      [Keine.],
      [Gefüllte Datenbank und Zugriff darauf.],
      [Assemblies der lokalen Installation.],
      [Aufwand], [Gering], [Mittel], [Hoch],
    ),
    caption: [Variantenvergleich Verwendungssuche (eigene Darstellung)]
  ) <variante_verwendungssuche>
]

=== Machbarkeitsbeurteilung <machbarkeitsbeurteilung>

Die Beurteilung stützt sich auf die Dokumentation von Microsoft, auf das bestehende
Tool, auf die Erfahrung aus der täglichen Arbeit mit Dynamics 365 und auf eine
Machbarkeitsstudie mit fünf Prototypen. Deren Protokoll steht in @befundprotokoll.

#heading(outlined: false, level: 4)[Technisch geklärt]

Der Zugriff auf die Label-Dateien ist auf beiden Wegen machbar. Der direkte Weg ist
im bestehenden BE-LabelEditor seit Jahren im Einsatz, die Metadata-API ist
dokumentiert und wird für das Auslesen von Labels bereits eingesetzt
@meyer-labels-net.

Das Add-in-Modell für die Developer Tools ist dokumentiert und liefert die
beiden genannten Einstiegspunkte. Die Editor-Erweiterbarkeit von Visual Studio mit
QuickInfo, Taggern und CodeLens ist ebenfalls dokumentiert.

Die Verwendungssuche ist über die Textsuche gesichert, weil das bestehende Tool
genau so arbeitet. Die Cross-Reference-Datenbank ist eine Verbesserung, kein
Risiko.

#heading(outlined: false, level: 4)[Prototypenvergleich zum Extension-Modell]

Zu V4 wurden drei Prototypen gebaut, je einer pro Modell. Sie kompilieren und
paketieren reproduzierbar unter Visual Studio Community 2026 in der Version 18.9.2
mit dem .NET SDK 10.0.400. Keiner der drei wurde in einer laufenden
Visual-Studio-Instanz geladen. Belegt ist damit, dass sich die jeweiligen
Schnittstellen ansprechen und kompilieren lassen, nicht dass sie zur Laufzeit
funktionieren. Aussagen darüber, dass etwas fehlt, stützen sich auf die
Schnittstellen des SDK und auf fehlgeschlagene Kompilierversuche.

Der wichtigste Befund betrifft den Betrieb ausserhalb des Prozesses. Die
WPF-Editor-Typen existieren dort nicht. Derselbe Quelltext, der im klassischen
Modell kompiliert, scheitert mit der Meldung, dass `IWpfTextViewCreationListener`
nicht gefunden wird. An deren Stelle tritt eine eigene, deutlich kleinere
Oberfläche mit Listenern für das Öffnen und Ändern einer Ansicht, einer
Margin und einem Tagger.

Daraus folgt für die beiden Anzeigeziele, dass CodeLens ausserhalb des Prozesses
erreichbar ist. Ein QuickInfo-Typ kommt in der Oberfläche dagegen nicht vor, eine
Suche über alle Assemblies des gebauten Pakets lieferte keinen Treffer. Ein
Tooltip existiert dort nur als Eigenschaft eines CodeLens-Labels, hängt also an
der CodeLens-Zeile und nicht am Text selbst. Freie Einblendungen mitten im Text
sind ebenfalls nicht möglich, es bleiben Margin, Einfärbung und
CodeLens.

Am Text selbst können ausserhalb des Prozesses überhaupt nur vier Dinge ansetzen.
Zwei Tag-Arten, die einfärben oder die Darstellung ändern, aber keinen Text tragen,
dazu CodeLens oberhalb der Zeile und die Margin am Rand. Keines davon zeigt
beim Überfahren eines Tokens Text an.

Tagger und CodeLens sind in dieser Fassung des SDK als Vorschau markiert. Der
Compiler bricht den Build ab, bis die entsprechende Warnung ausdrücklich
unterdrückt wird, und weist darauf hin, dass die Schnittstelle sich ändern oder
obsolet werden kann.

Zum Modell im selben Prozess ergab der Versuch zwei Punkte. Erstens lässt es sich
mit klassischem MEF-Code in einer einzigen Assembly verbinden, beides kompiliert
gemeinsam. Zweitens bringt es gegenüber dem klassischen Modell keinen Gewinn bei
der Ausfallsicherheit, denn es läuft im selben Prozess und trägt damit dasselbe
Absturzrisiko.

#heading(outlined: false, level: 4)[Befunde aus der Testumgebung]

In einer zweiten Runde wurden die Developer Tools von Dynamics 365 statisch ausgewertet
und eine Testerweiterung auf der Testumgebung des Arbeitgebers ausgeführt. Die Ergebnisse
stehen in @befundprotokoll.

Der X++-Editor meldet einen gewöhnlichen Content Type und ist nicht abgeschottet.
Eine eigene Erweiterung kann sich daran anhängen, was der Versuch auf der
Testumgebung bestätigt hat. Damit sind Z2 und Z3 umsetzbar. Der Classifier von Dynamics 365
kennzeichnet Label-Token bereits selbst, unter anderem als "X++ Modern Label".
Eine Erweiterung findet Label-IDs damit über die vorhandene Klassifizierung,
statt X++ selbst zerlegen zu müssen. Das senkt den Aufwand für Z2 und Z3
erheblich.

Die eigene QuickInfo-Quelle wird beim Überfahren von X++-Code gefragt und erkennt
Label-Token. Ihr Eintrag erscheint im selben Tooltip wie der von Dynamics 365.
Der Mechanismus für FA05 ist damit belegt, das Auflösen eines Labels in alle
konfigurierten Sprachen dagegen noch nicht erprobt.

Für CodeLens verwenden die Developer Tools von Dynamics 365 nicht die Infrastruktur von
Visual Studio, sondern eine eigene Nachbildung. Ein Aufruf der
CodeLens-Schnittstelle wurde zur Laufzeit nie beobachtet. Die von Dynamics 365
verwendete Technik beruht aber auf öffentlichen Typen des Editors und steht einer
eigenen Erweiterung im gleichen Prozess ebenfalls offen. FA06 ist damit
umsetzbar, jedoch nicht über CodeLens.

Für das Properties Window bietet das Add-in-Modell keinen Erweiterungspunkt.
Über das Selection Tracking von Visual Studio lässt sich das im Designer gewählte
Element jedoch lesen, einschliesslich seiner Label-Eigenschaften. Für Z6 genügt
das, um ausgewählte Elemente heranzuziehen. Das Schreiben über den Designer wurde
nicht erprobt und wird nicht weiter verfolgt, weil Änderungen bei Bedarf direkt in
den Dateien erfolgen können.

Die Metadata-API lief auf einem privaten Gerät ohne Anwendungsserver, Datenbank
und Developer Tools. Im warmen Zustand sind beide Zugriffsarten gleich
schnell, weil das Zerlegen der Dateien den Aufwand bestimmt. Die API liefert den
Dateiinhalt unverarbeitet, das Zerlegen bleibt in beiden Fällen eigene Arbeit.

Die Developer Tools enthalten einen fertigen Label-Resolver von Microsoft, der zu
einer Label-ID den Text in einer gewählten Sprache liefert. Das entspricht fast
genau der Abfrage, die FA05 braucht. Er steht nur innerhalb von Visual Studio mit
installierten Developer Tools zur Verfügung und ist nicht zur Weitergabe mit einer
eigenen Extension bestimmt. Ausserhalb von Visual Studio scheiterte er an einer
fehlenden Abhängigkeit.

Belegt ist ausserdem, dass eine klassische Erweiterung mit MEF-Anteilen in Visual
Studio 2026 lädt und neben den Developer Tools von Dynamics 365 läuft, ohne diese
erkennbar zu stören.

#heading(outlined: false, level: 4)[Offen]

Nicht geprüft wurde das Auflösen eines Labels in alle konfigurierten Sprachen
innerhalb des Tooltips. Ebenso wenig, ob sich Eigenschaften über den Designer
schreiben lassen, was aber bewusst nicht weiter verfolgt wird. Die Prototypen des
neuen Modells wurden nie in einer laufenden Instanz geladen, belegt ist dort nur
das Kompilieren und Paketieren. Offen ist auch, ob der Label-Resolver von
Microsoft innerhalb von Visual Studio läuft.

#heading(outlined: false, level: 4)[Zielkonflikt aus V4]

Der Betrieb ausserhalb des Visual-Studio-Prozesses würde NFA04 am besten erfüllen,
denn ein Fehler bliebe auf die Extension beschränkt. Er schliesst aber den Zugriff
auf VSSDK und MEF aus. Davon betroffen sind QuickInfo, freie Einblendungen im Code
und die Anbindung an das Add-in-Modell. CodeLens bleibt erreichbar, allerdings über
eine als Vorschau markierte Schnittstelle. Damit stehen Z2 und Z6 gegen NFA04. Der
Entscheid dazu steht in @variantenentscheid.

#heading(outlined: false, level: 4)[Zeitliche Machbarkeit]

Der kritische Punkt ist nicht die Technik, sondern der Umfang. Z1 verlangt die
Feature Parity zu einem Tool, das über Jahre gewachsen ist. Neben der
Suche enthält es eine Bewertung, welche die Treffer nach Relevanz sortiert, die
Aktualisierung der Referenzen im Code beim Kopieren und Verschieben sowie eine
Überwachung der Label-Dateien auf Änderungen von aussen. Die Realisierung umfasst
acht Arbeitstage. Nach dem Grundgerüst am ersten Tag erhält Stufe 1 vier Tage,
Stufe 2 zwei und Stufe 3 einen. Für Tests und Restarbeiten folgen drei weitere
Tage.

Entlastend wirkt ein Befund aus der zweiten Runde. Weil der Classifier von
Dynamics 365 Label-Token bereits selbst kennzeichnet, entfällt das Zerlegen von
X++ für Z2 und Z3. Das war vorher der grösste Unsicherheitsposten der beiden
Ziele.

Die Ziele sind damit erreichbar, aber ohne Reserve. Die Reihenfolge aus
@projektziele ist die Vorkehrung dagegen. Fällt Zeit aus, wird an den Zielen der
Stufe 3 gekürzt.

=== Variantenentscheid <variantenentscheid>

Alle fünf Varianten sind entschieden. Wo die Befunde keinen eindeutigen Vorteil
zeigen, fällt die Wahl auf das einfachere oder bereits erprobte Verfahren. Die
jeweils andere Möglichkeit bleibt vorgemerkt.

Der Betrieb ausserhalb des Visual-Studio-Prozesses scheidet aus, aus zwei
voneinander unabhängigen Gründen.

FA05 verlangt die Anzeige beim Überfahren einer Label-ID, also am Token im
Editortext. Ausserhalb des Prozesses gibt es dafür keine Möglichkeit. Ein Tooltip
lässt sich dort nur an ein CodeLens-Label hängen, was den Benutzer zwingen würde,
statt des Labels die CodeLens-Zeile zu überfahren. Das erfüllt FA05 nicht und wäre für die Benutzererfahrung schlechter.

Unabhängig davon bliebe ausserhalb des Prozesses nur CodeLens als Anzeigeform.
Die Developer Tools von Dynamics 365 verwenden für X++ aber eine eigene
Nachbildung von CodeLens und nicht die Infrastruktur von Visual Studio. Der
Versuch auf der Testumgebung hat entsprechend nie einen Aufruf dieser
Infrastruktur beobachtet. Damit fiele auch die letzte verbleibende Anzeigeform
für X++ aus.

Der Zielkonflikt mit NFA04 wird damit zugunsten der Ziele aufgelöst, weil diese in
den Erfolgskriterien stehen.

Zwischen dem klassischen VSSDK und dem neuen Modell im selben Prozess fällt die
Wahl auf das neue Modell. Beide bieten dieselben Möglichkeiten, weil in beiden
Fällen MEF zur Verfügung steht, und beide teilen dasselbe Absturzrisiko. Den
Ausschlag gibt die Empfehlung von Microsoft, die das neue Modell für neue
Erweiterungen nennt, welche auf Dienste des VSSDK angewiesen sind
@ms-inproc-extensions. Der Prototyp hat gezeigt, dass sich der MEF-Anteil für
QuickInfo und die Einblendung im Code darin unverändert mitverwenden lässt.
Microsoft verwendet für die eigenen Developer Tools von Dynamics 365 dieselbe Mischform,
was die Wahl zusätzlich stützt.

Drei Folgen sind dabei in Kauf zu nehmen. Das Target Framework bleibt .NET Framework,
und die Erweiterung trägt zwei Manifeste, weil sie ihre Identität aus dem
klassischen Manifest bezieht. Sie ist damit strukturell eine Mischform und kein
reiner Vertreter des neuen Modells.

Die dritte Folge betrifft NFA04. Der Betrieb im selben Prozess bietet keinen
strukturellen Schutz davor, dass ein Fehler in der Extension Visual Studio
mitreisst. Die Anforderung bleibt im Wortlaut bestehen und wird über die Umsetzung
so weit abgedeckt, wie es ohne Prozesstrennung möglich ist. Wie das geschieht und
wie es nachgewiesen wird, legt das Konzept fest.

Bei V2 entscheiden die Befunde gegen ein zusätzliches Add-in. Es brächte nur zwei
Menüeinträge, und für das Properties Window besteht darin kein
Erweiterungspunkt. Was die Ziele verlangen, deckt die Extension selbst ab, denn
das im Designer gewählte Element lässt sich über das Selection Tracking lesen. Ein
Add-in würde ausserdem eine zweite Auslieferungsform nötig machen und die Arbeit
an die Version der Developer Tools binden.

Bei V1 fällt die Wahl auf den direkten Dateizugriff mit eigenem Parser. Die
Messung zeigt im warmen Zustand keinen Unterschied, und die Metadata-API zerlegt
die Label-Dateien nicht, sondern liefert deren Inhalt unverarbeitet. Der Vorteil
der API bleibt damit gering, weil das Zerlegen ohnehin selbst zu schreiben ist.

Den Ausschlag gibt die Abhängigkeit. Ohne die API bleibt die Kernlogik frei von
Assemblies der lokalen Installation. Das vereinfacht die Tests und macht die
Arbeit auf einem anderen System nachvollziehbar, auf dem diese Assemblies
voraussichtlich fehlen. Z8 verlangt genau das. Aus demselben Grund wird der
Label-Resolver von Microsoft nicht eingesetzt. Er bleibt für FA05 als Möglichkeit
vorgemerkt.

Bei V3 fällt die Wahl auf das einmalige Laden in den Arbeitsspeicher, also auf das
Verfahren des bestehenden Tools. Es ist erprobt. Die Ladezeit von rund zwei
Sekunden stammt aus dem synthetischen Datensatz, auf einem Package-Verzeichnis mit
vielen Models ist sie nicht gemessen. Ein eigener Index brächte dauerhaften Zustand mit sich, der mit
den Dateien abgeglichen werden müsste, ohne dass ein Bedarf dafür belegt wäre. Er
bleibt als Möglichkeit vorgemerkt, falls sich die Suche in der Realisierung als zu
langsam erweist.

Bei V5 fällt die Wahl aus demselben Grund auf die Textsuche in den XML-Dateien.
Sie ist im bestehenden Tool erprobt und setzt nichts voraus. Die
Cross-Reference-Datenbank liefert zwar genauere Treffer, ist aber nur so aktuell
wie der letzte Build mit Referenzdaten und damit von einem Schritt abhängig, den
die Extension nicht auslöst. Auch sie bleibt als Möglichkeit vorgemerkt.

Eine Aufteilung in zwei Erweiterungen, also die inhaltliche Arbeit ausserhalb des
Prozesses und eine bewusst dünne Anzeigeschicht darin, würde beide Anliegen
teilweise erfüllen. Sie wurde nicht erprobt und ist eine Ableitung aus den
Befunden, kein Ergebnis. Der Preis wäre ein zusätzlicher Kommunikationsweg und
eine doppelte Auslieferung.

== Projektmanagement

=== Projektorganisation

Die Diplomarbeit wird als Einzelarbeit durchgeführt. Projektleitung, Entwicklung,
Test und Dokumentation liegen bei derselben Person.

#[
  #show figure: set align(left)
  #figure(
    table(
      align: left,
      columns: (auto, auto, 1fr),
      table.header(
        [*Rolle*], [*Person*], [*Aufgaben und Verantwortung*],
      ),
      [Projektleitung], [Adrian Aeschlimann],
      [Planung, Steuerung und Terminüberwachung, Nachführen von Aufwand und
       Terminplan.],
      [Entwicklung und Test], [Adrian Aeschlimann],
      [Konzept, Implementierung, Testdurchführung und Dokumentation.],
      [Firmenbetreuung], [Adrian Aeschlimann],
      [Fachliche Vertretung der Auftraggeberin gegenüber der Schule.],
      [Betreuende Person], [Stefan Canobbio],
      [Fachliche Begleitung, Vorzeigetermine, Bewertung der Arbeit.],
      [Experte], [Raphael Bucher],
      [Zweitbewertung der Arbeit und Abnahme der Präsentation.],
    ),
    caption: [Projektorganisation (eigene Darstellung)]
  ) <projektorganisation>
]

Während der Diplomarbeit findet keine Abnahme durch BE-terna statt. Vorgesehen ist,
die Arbeit einem Experten im Unternehmen vorzuzeigen und Rückmeldungen
einzuholen, sobald erste Teile des Konzepts stehen.

=== Projektplanung

Das Projekt folgt dem Wasserfallmodell. Die Phasen Initialisierung, Konzept,
Realisierung sowie Test und Abschluss werden nacheinander durchlaufen, wobei eine
Phase vor dem Beginn der nächsten abgeschlossen wird.

Zwei Gründe sprechen für dieses Vorgehen. Der Umfang steht mit der
Themeneingabe fest. BE-terna bringt während der Umsetzung keine neuen Anforderungen
ein und entscheidet erst nach Projektabschluss über die Einführung. Die Richtlinien verlangen eine durchgehende
Dokumentation von der Initialisierung bis zur Abnahme, was ein phasenweises
Vorgehen ohnehin nahelegt.

Innerhalb der Realisierung wird nach der Reihenfolge aus @projektziele umgesetzt.
Zuerst entsteht die Feature Parity zum bestehenden Tool, danach folgen die
Funktionen, die den Kontextwechsel beseitigen, zuletzt die Textextraktion und die
Anbindung des Übersetzungsdienstes. Die Dokumentation läuft über alle Phasen
hinweg mit.

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
    caption: [Verfügbarkeit im Projektzeitraum (eigene Darstellung)]
  ) <verfuegbarkeit>
]

Daraus ergeben sich im Zeitraum vom 04.09.2026 bis zur Abgabe am 02.11.2026
insgesamt 24 verfügbare Arbeitstage. Bei einem angenommenen Tagespensum von rund
acht Stunden entspricht dies einem Gesamtaufwand von etwa 192 Stunden und liegt
damit innerhalb der in den Richtlinien genannten Bandbreite von 150 bis 250 Stunden.
Mit den vier Tagen für die Präsentation nach der Abgabe umfasst die Detailplanung
28 Tage oder 224 Stunden.

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

Nach dem Review der Initialisierung wurde der Terminplan am 24.09.2026
überarbeitet. Das Konzept ist von fünf auf drei Tage verkürzt, weil der
Architekturentscheid bereits in der Initialisierung gefallen ist. Die beiden Tage
gehen an die Realisierung, die jetzt nach den Stufen aus @projektziele gegliedert
ist.

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
      [AP0], [Dokumentation laufend], [04.09.], [31.10.], [--], [04.09.], [], [],
      [AP1], [Projektinitialisierung], [04.09.], [26.09.], [7], [04.09.], [], [],
      [AP2], [Konzept], [01.10.], [08.10.], [3], [], [], [],
      [AP3], [Realisierung], [09.10.], [21.10.], [8], [], [], [],
      [AP4], [Tests und Abschluss Realisierung], [22.10.], [24.10.], [3], [], [], [],
      [AP5], [Dokumentation finalisieren und Review], [29.10.], [31.10.], [3], [], [], [],
      [AP6], [Präsentation vorbereiten], [05.11.], [12.11.], [4], [], [], [],
    ),
    caption: [Arbeitspakete Soll/Ist (eigene Darstellung)]
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
      [M0], [Start Diplomarbeit], [04.09.2026], [04.09.2026], [keine],
      [M1], [Projektinitialisierung abgeschlossen], [26.09.2026], [], [],
      [M2], [1. Vorzeigetermin Betreuung], [07.10.2026], [], [],
      [M3], [Konzept abgeschlossen], [08.10.2026], [], [],
      [M4], [2. Vorzeigetermin Betreuung (zu vereinbaren)], [22.10.2026], [], [],
      [M5], [Code Freeze], [24.10.2026], [], [],
      [M6], [Abgabe Diplomarbeit, 17.00 Uhr], [02.11.2026], [], [],
      [M7], [Präsentation], [13.11.2026], [], [],
    ),
    caption: [Meilensteine Soll/Ist (eigene Darstellung)]
  ) <meilensteine_soll_ist>
]

Die tagesgenaue Detailplanung mit der laufenden Aufwanderfassung befindet sich im
Anhang unter @detailplanung.

=== Risikoanalyse

Eintrittswahrscheinlichkeit und Auswirkung werden je auf einer Skala von 1 bis 5
bewertet. Der Risikowert ist ihr Produkt.

#[
  #show figure: set align(left)
  #set text(size: 9pt)
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
      [R01], [Terminplan ohne Puffer],
      [Die 24 verfügbaren Arbeitstage sind vollständig verplant. Jede verlorene
       Stunde verschiebt sich unmittelbar auf die folgenden Arbeitspakete.],
      [4], [4], [16],
      [Arbeitspakete werden vorgezogen, sobald Kapazität frei wird. Reicht die Zeit
       nicht, wird bei den Zielen der Stufe 3 gekürzt und die Kürzung mit der
       Betreuung abgesprochen.],

      [R02], [Umfang von Z1 unterschätzt],
      [Die Feature Parity betrifft ein über Jahre gewachsenes Tool mit
       Ranking, Referenzaktualisierung und Überwachung der Label-Dateien.
       Dafür stehen in der Realisierung vier Arbeitstage zur Verfügung.],
      [4], [3], [12],
      [Die Funktionen aus @ist_funktionen werden im Konzept einzeln als
       Detailanforderung erfasst. Das Ranking entsteht zuerst in einer
       einfachen Form und wird nur bei verbleibender Zeit verfeinert.],

      [R03], [Verlust geplanter Arbeitstage],
      [Krankheit, private Termine oder eine hohe Auslastung im Beruf entziehen dem
       Projekt geplante Arbeitstage. Weil alle Projektrollen bei einer Person
       liegen, steht in dieser Zeit die gesamte Arbeit still.],
      [4], [4], [16],
      [Verlorene Stunden lassen sich an den Abenden von Montag bis Mittwoch
       nachholen, die der Plan nicht vorsieht. Fällt mehr als ein Arbeitstag aus,
       wird bei den Zielen der Stufe 3 gekürzt und die Betreuung informiert.],

      [R04], [Fehler der Editor-Anbindung zeigen sich spät],
      [Die Editor-Anbindung lässt sich nur auf der Testumgebung des Arbeitgebers
       prüfen, auf der nicht entwickelt werden darf. Ein Fehler zeigt sich erst
       beim nächsten Durchgang dort und nicht schon beim Entwickeln.],
      [4], [3], [12],
      [Der Entscheid zu V1 hält die Kernlogik frei von Assemblies der lokalen
       Installation und damit ohne Dynamics 365 testbar. Für die Label-Dateien
       dient ein synthetischer Verzeichnisbaum. Auf der Testumgebung wird nur die
       Editor-Anbindung geprüft, und zwar in gesammelten Durchgängen.],

      [R05], [Veröffentlichung geschützten Materials],
      [Das Repository ist öffentlich. Quellcode von Microsoft oder BE-terna,
       Betriebsdaten und Zugangsdaten dürfen nicht hineingelangen.],
      [2], [5], [10],
      [Vor jedem Commit werden die neu hinzugekommenen Dateien gegen die Auflagen
       aus @konfigurationsmanagement geprüft. Arbeitsstände, die nicht
       veröffentlicht werden, liegen ausserhalb des Repositories.],

      [R06], [Extension-Modell lädt nicht wie erwartet],
      [Belegt ist, dass der MEF-Anteil auf der Testumgebung lädt und
       QuickInfo an X++-Token liefert. Für das neue Modell, das diesen Anteil
       umschliesst, sind nur Kompilieren und Paketieren belegt.],
      [2], [3], [6],
      [Das erste Arbeitspaket der Realisierung stellt ein lauffähiges Grundgerüst
       her, bevor Funktionen entstehen. Scheitert es, bleibt der Rückfall auf das
       klassische VSSDK, das im Spike nachweislich lädt und den MEF-Anteil
       unverändert übernimmt.],

      [R07], [Unbelegte Annahmen erzwingen Nacharbeit],
      [Zwei Annahmen sind offen. NFA04 ist ohne Prozesstrennung nur über
       Fehlerbehandlung abzudecken, und die Erkennung der Label-Token stützt sich
       auf die Classifier der Developer Tools. Trifft eine davon nicht zu, kostet
       die Nacharbeit Arbeitstage.],
      [2], [3], [6],
      [Die Fehlerbehandlung legt das Konzept fest, die Tests weisen sie nach. Die
       Bezeichnungen der Classifier werden an einer Stelle gehalten, als Rückfall
       bleibt ein eigener Mustervergleich auf der Textzeile.],

      [R08], [FA06 nicht wie vorgesehen umsetzbar],
      [Die Developer Tools von Dynamics 365 verwenden für X++ eine eigene Nachbildung von
       CodeLens. Ob sich die dauerhafte Einblendung mit den öffentlichen Typen des
       Editors nachbauen lässt, ist nicht erprobt.],
      [3], [2], [6],
      [Z2 ist über FA05 bereits belegt und auch ohne FA06 erreicht. FA06 wird nach
       FA05 umgesetzt und bei Zeitmangel zurückgestellt.],

      [R09], [Alle Projektrollen in einer Person],
      [Planung, Umsetzung und Prüfung liegen bei derselben Person. Fehleinschätzungen
       fallen ohne Blick von aussen erst spät auf.],
      [3], [2], [6],
      [Die beiden Vorzeigetermine mit der Betreuung dienen als Prüfpunkt.
       Vorgesehen ist zudem, die Arbeit einem Experten im Unternehmen vorzuzeigen.],
    ),
    caption: [Risikoanalyse (eigene Darstellung)]
  ) <risikoanalyse>
]

=== Risikomatrix

// Die Farbe einer Zelle folgt dem Risikowert, damit Matrix und Bewertungsschema
// zusammenpassen.
#let risikofarbe(wert) = if wert >= 15 {
  rgb("#f4b6ac")
} else if wert >= 8 {
  rgb("#ffe08a")
} else {
  rgb("#bfe3b4")
}

#let risikofeld(e, a, inhalt) = table.cell(fill: risikofarbe(e * a), inhalt)

#[
  #show figure: set align(left)
  #figure(
    table(
      align: horizon + center,
      columns: (auto, 1fr, 1fr, 1fr, 1fr, 1fr),
      rows: (auto, auto, 2.2em, 2.2em, 2.2em, 2.2em, 2.2em),
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

      [*5 -- sehr hoch*],
      risikofeld(5, 1)[], risikofeld(5, 2)[], risikofeld(5, 3)[],
      risikofeld(5, 4)[], risikofeld(5, 5)[],

      [*4 -- hoch*],
      risikofeld(4, 1)[], risikofeld(4, 2)[], risikofeld(4, 3)[R02, R04],
      risikofeld(4, 4)[R01, R03], risikofeld(4, 5)[],

      [*3 -- mittel*],
      risikofeld(3, 1)[], risikofeld(3, 2)[R08, R09], risikofeld(3, 3)[],
      risikofeld(3, 4)[], risikofeld(3, 5)[],

      [*2 -- gering*],
      risikofeld(2, 1)[], risikofeld(2, 2)[], risikofeld(2, 3)[R06, R07],
      risikofeld(2, 4)[], risikofeld(2, 5)[R05],

      [*1 -- sehr gering*],
      risikofeld(1, 1)[], risikofeld(1, 2)[], risikofeld(1, 3)[],
      risikofeld(1, 4)[], risikofeld(1, 5)[],
    ),
    caption: [Risikomatrix (eigene Darstellung)]
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
      [1 -- 6], table.cell(fill: risikofarbe(4))[Gering],
      [Wird beobachtet. Die Massnahme ist festgehalten, wird aber nicht aktiv
       verfolgt.],
      [8 -- 12], table.cell(fill: risikofarbe(10))[Mittel],
      [Die Massnahme wird umgesetzt. Der Stand wird im Controlling-Bericht
       nachgeführt.],
      [15 -- 25], table.cell(fill: risikofarbe(20))[Hoch],
      [Die Massnahme wird sofort umgesetzt. Tritt das Risiko ein, wird die
       Betreuung informiert.],
    ),
    caption: [Bewertungsschema Risikomatrix (eigene Darstellung)]
  ) <risikomatrix_schema>
]

Hoch klassiert sind der fehlende Puffer im Terminplan und der Verlust geplanter
Arbeitstage. Beide lassen sich nicht beseitigen, weil die Zahl der verfügbaren
Tage feststeht. Wirksam sind nur das Vorziehen von Arbeitspaketen und die Kürzung
bei den Zielen der Stufe 3. Die beiden nächsten Werte hängen ebenfalls am
Zeitplan, denn der Umfang von Z1 und der Wechsel auf die Testumgebung kosten beide
Arbeitstage.

Die technischen Risiken liegen tiefer. Das ist die Wirkung der Machbarkeitsstudie,
die die offenen Fragen zum Extension-Modell vor der Realisierung beantwortet hat.
Was dort offen geblieben ist, steht als R06 bis R08 in der Tabelle.

=== Qualitätsmanagement

Die Qualitätsziele stehen als NFA01 bis NFA06 in @grobe_anforderungen und werden im
Testkonzept mit Testfällen hinterlegt. Ob ein Ziel erreicht ist, entscheidet das
Messkriterium aus @projektziele und nicht der Eindruck beim Ausprobieren.

#[
  #show figure: set align(left)
  #figure(
    table(
      align: left,
      columns: (auto, 1fr, auto),
      table.header(
        [*Gegenstand*], [*Massnahme*], [*Zeitpunkt*],
      ),
      [Dokumentation],
      [Review des Kapitels gegen die Richtlinien und die vorgegebene Gliederung.],
      [AP1.7, AP2.5, AP5.3],
      [Dokumentation],
      [Vorzeigen bei der Betreuung und Einarbeiten der Rückmeldungen.],
      [M2, M4],
      [Kernlogik],
      [Unit Tests für Parser, Suche und Dateizugriff. Sie laufen ohne Visual Studio
       und ohne Dynamics 365.],
      [laufend in AP3],
      [Editor-Anbindung],
      [Manuelle Tests auf der Testumgebung nach den Testfällen aus dem Konzept, mit
       Protokoll im Kapitel Realisierung.],
      [AP4.2],
      [Kompatibilität],
      [Installation und Funktionstest unter den Visual-Studio-Versionen aus NFA01
       auf der Testumgebung.],
      [AP4.2],
      [Quellcode],
      [Einheitliche Programmierrichtlinien, Kompilieren ohne Warnungen.],
      [laufend in AP3],
      [Quellcode],
      [Prüfung der neu hinzugekommenen Dateien gegen die Auflagen aus
       @konfigurationsmanagement.],
      [vor jedem Commit],
      [Zielerreichung],
      [Abgleich der umgesetzten Funktionen gegen die Messkriterien aller acht
       Ziele.],
      [AP4.3, AP5.1],
    ),
    caption: [Qualitätssichernde Massnahmen (eigene Darstellung)]
  ) <qualitaetsmassnahmen>
]

Zwei Einschränkungen bleiben. Ein Review durch eine zweite Person findet nur an den
Vorzeigeterminen statt, dazwischen fallen Umsetzung und Prüfung zusammen. Die
Editor-Anbindung lässt sich nicht automatisiert testen, weil dafür eine laufende
Instanz von Visual Studio mit den Developer Tools von Dynamics 365 nötig wäre. Diese
Tests bleiben manuell und werden über das Testprotokoll nachvollziehbar gehalten.

=== Konfigurationsmanagement <konfigurationsmanagement>

Für die Versionskontrolle wird Git eingesetzt. Das Repository liegt öffentlich auf
GitHub.

#block(
  width: 100%,
  fill: rgb("#f1f3f5"),
  stroke: 0.5pt + rgb("#adb5bd"),
  radius: 3pt,
  inset: 10pt,
)[
  #align(center)[
    #text(size: 13pt)[#link("https://github.com/Adyrem/diploma-label-localization")]
  ]
]

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
  veröffentlicht. Sie sind mit den Developer Tools einsehbar, gehören aber
  Microsoft. Auf Standardobjekte wird über ihren Namen und die öffentliche
  Dokumentation von Microsoft verwiesen.
- Es werden keine kundenbezogenen Daten verwendet. Für Entwicklung, Tests und
  Screenshots dient ein synthetischer Datensatz.
- Zugangsdaten, API-Schlüssel, Umgebungs-URLs und Lizenzangaben werden nicht
  abgelegt.

Arbeitsstände, die nicht veröffentlicht werden sollen, bleiben ausserhalb des
Repositories in einem lokalen Ordner.

== Abweichungen von der Themeneingabe <abweichungen_te>

Gegenüber der Themeneingabe vom 26.05.2026 weicht die Planung in den folgenden
Punkten ab. Die Abweichungen werden der Betreuung am ersten Vorzeigetermin
vorgelegt.

#[
  #show figure: set align(left)
  #set text(size: 10pt)
  #figure(
    table(
      align: left,
      columns: (auto, 1fr, 1fr, 1.2fr),
      table.header(
        [*Punkt*], [*Themeneingabe*], [*Jetzt*], [*Grund*],
      ),
      [NFA01 Kompatibilität],
      [Kompatibel mit Visual Studio 2022 und 2026.],
      [Entwickelt für 2026, getestet auch unter 2022. Fehler, die nur unter 2022
       auftreten, haben tiefe Priorität.],
      [Erweiterungspunkte, die nur Visual Studio 2026 bietet, sollen nutzbar
       sein.],

      [FA06 Inline-Anzeige],
      [Englische Übersetzung oberhalb der Label-ID.],
      [Übersetzung im Code, Sprache und Position offen.],
      [Z2 verlangt alle konfigurierten Sprachen, nicht nur Englisch. Die Position
       legt das Konzept fest. Oberhalb der Zeile ginge es mit derselben Technik,
       mit der die Developer Tools ihre Referenzanzeige zeichnen, erprobt ist das
       aber nicht, siehe R08.],

      [FA12 bis FA17],
      [Nicht enthalten.],
      [Ergänzt.],
      [Z1 verlangt den vollen Funktionsumfang des bestehenden Tools, FA01 bis FA11
       decken ihn nur teilweise ab.],

      [NFA06 Sicherheit],
      [Nicht enthalten.],
      [Ergänzt.],
      [FA11 legt API-Schlüssel ab.],

      [Phasenplan],
      [Initialisierung 1, Konzept 2, Realisierung 4, Test und Abschluss 1 Woche.],
      [Initialisierung 7, Konzept 3, Realisierung 8, Tests 3 und Dokumentation 3
       Arbeitstage.],
      [Der Architekturentscheid ist mit der Machbarkeitsstudie in die
       Initialisierung gewandert, das Konzept wird dadurch kürzer. Die
       Themeneingabe rechnet in Kalenderwochen, der Terminplan in Arbeitstagen,
       von denen es pro Woche regulär drei gibt.],
    ),
    caption: [Abweichungen von der Themeneingabe (eigene Darstellung)]
  ) <abweichungen_tabelle>
]
