#import "../helpers.typ": todo

= Konzept <konzept>

== Kontextdiagramm

Die Extension läuft in Visual Studio und hat vier Nachbarn. Von den Developer
Tools erfährt sie, was der X++-Editor als Label erkennt und welches Element im
Designer gewählt ist. Mit dem Package-Verzeichnis tauscht sie Label-Dateien und
die XML-Dateien der Elemente aus. Den Übersetzungsdienst ruft sie beim Anlegen
eines Labels auf. Über Visual Studio fügt sie Text in den Editor ein und schreibt
Meldungen ins Output Window.

#figure(
  image("../diagrams/Kontextdiagramm.png", width: 85%),
  caption: [Kontextdiagramm (eigene Darstellung)]
) <kontextdiagramm>

Ausserhalb der Systemgrenze liegen der Build von Dynamics 365, der die
Label-Dateien kompiliert, und der Best-Practice-Check, der hardcodierte Texte
meldet. Die Extension greift in keinen der beiden ein, muss ihre Dateien aber so
hinterlassen, dass der Build sie weiterhin liest.

== Geschäftsprozessanalyse

Der heutige Ablauf in @kontextwechsel wechselt zweimal die Anwendung, einmal in den
BE-LabelEditor und einmal zurück. @sollablauf_label zeigt denselben Ablauf mit der
Extension. Er bleibt vollständig in Visual Studio, und beim Anlegen werden die
Übersetzungen nur noch geprüft statt von Hand erfasst. Für ein vorhandenes Label
sinkt die Zahl der Schritte von sieben auf vier.

#figure(
  image("../diagrams/Sollablauf_Label.png", width: 60%),
  caption: [Sollablauf beim Suchen oder Anlegen eines Labels (eigene Darstellung)]
) <sollablauf_label>

Hardcodierte Texte meldet der Best-Practice-Check bereits heute. Neu ist, dass
der Entwickler sie an der Fundstelle in ein Label umwandelt, ohne den Text von
Hand herauszulösen und die Label-ID von Hand einzusetzen.

#figure(
  image("../diagrams/Sollablauf_Extraktion.png", width: 55%),
  caption: [Sollablauf beim Ersetzen eines hardcodierten Textes (eigene
            Darstellung)]
) <sollablauf_extraktion>

== Detailanforderungen an das neue System <detailanforderungen>

Wo eine Label-ID vorkommt, gelten beide Formen aus @festlegungen. Neue Labels
entstehen nur in der neuen Form.

=== Funktionale Anforderungen

#[
  #show figure: set align(left)
  #set text(size: 9.5pt)
  #figure(
    table(
      align: left,
      columns: (8.5em, 1fr, auto, auto),
      table.header(
        [*Anforderung*], [*Akzeptanzkriterien*], [*Setzt\ voraus*], [*Stufe*],
      ),
      [FA01\ Label-Suche],
      [- Suchmodi Exact match und Substring, je mit und ohne Beachtung der Gross-
         und Kleinschreibung, dazu die Suche nach Label-ID.
       - Durchsucht werden ID, Text und Kommentar in allen geladenen Sprachen.
       - Jedes Label erscheint einmal in der Trefferliste, nicht einmal je
         Sprache.
       - Die Suche nach Label-ID findet beide Formen, etwa
         `@BDM1:L3F2A9C15B8047DE1` und `@SYS12345`.],
      [--], [1],

      [FA02\ Label-Erstellung],
      [- Das Label entsteht in einer Label-Datei eines beschreibbaren Models.
         Schreibgeschützte Models werden nicht angeboten.
       - Die ID folgt @festlegungen.
       - Alle anzulegenden Sprachen sind mit dem Suchbegriff vorbelegt.
       - Dynamics 365 kompiliert die Label-Datei danach ohne Fehler.],
      [FA01], [1],

      [FA03\ Label-Bearbeitung],
      [- Text und Kommentar lassen sich je Sprache ändern und speichern.
       - Löschen entfernt das Label in allen Sprachen der Label-Datei.
       - Kopieren legt in einer anderen Label-Datei ein neues Label mit allen
         Sprachen an, die es dort gibt. Verschieben löscht danach das Original.
       - Auf Wunsch werden die Referenzen in beschreibbaren Models auf die neue
         ID umgestellt.
       - Labels aus schreibgeschützten Models oder kompilierten Ressourcen lassen
         sich kopieren, aber nicht ändern.],
      [FA01, FA04], [1],

      [FA04\ Verwendungssuche],
      [- Findet jedes Vorkommen der vollständigen Label-ID in den XML-Dateien der
         Package-Verzeichnisse.
       - Jede Fundstelle zeigt Model, Datei, Zeile und Spalte.
       - Ein Klick öffnet die Fundstelle in Visual Studio, siehe
         @festlegungen.],
      [--], [1, 2],

      [FA05\ Hover-Tooltip],
      [- Beim Überfahren einer Label-ID im X++-Editor erscheinen alle
         konfigurierten Sprachen mit ihrem Text.
       - Fehlende Übersetzungen sind als fehlend markiert.
       - Der Eintrag steht neben dem der Developer Tools.
       - Ist die ID unbekannt, sagt der Eintrag das.],
      [--], [2],

      [FA06\ Inline-Anzeige],
      [- Oberhalb jeder Zeile mit einer Label-ID erscheinen die Übersetzungen
         aller konfigurierten Sprachen, wie in @demo_inline.
       - Fehlende Übersetzungen sind markiert.
       - Die Anzeige lässt sich ein- und ausschalten, die Einstellung bleibt
         nach einem Neustart erhalten.],
      [FA05], [2],

      [FA07\ Inline-Suche],
      [- Ein Command im Editor öffnet das Tool Window und sucht mit dem markierten
         Text.
       - Ohne Markierung gilt der Inhalt des String-Literals unter dem
         Cursor.
       - Der Command ist über das Kontextmenü des Editors und einen Shortcut
         erreichbar.],
      [FA01], [2],

      [FA08\ Im Extension-Panel öffnen],
      [- Ein Command auf einer Label-ID im Code öffnet das Label in der
         Detailansicht des Tool Window.
       - Dort lässt es sich wie in FA03 bearbeiten.],
      [FA03], [2],

      [FA09\ Extraktion hardcodierter Texte],
      [- Im Editor wird der markierte Text eines String-Literals zu einem
         neuen Label, das Literal enthält danach die Label-ID. Rückgängig machen
         stellt den alten Text im Code wieder her.
       - Im Designer bietet der Command die Label-Eigenschaften des gewählten
         Elements an, die hardcodierten Text enthalten, etwa Label und Help Text.
         Die Eigenschaft enthält danach die Label-ID.
       - Vorgeschlagen wird eine Label-Datei des Models, zu dem die Datei gehört.
       - Das neue Label ist mit dem extrahierten Text vorbelegt.],
      [FA02, FA10], [3],

      [FA10\ Automatische Übersetzung],
      [- Beim Anlegen schlägt der Dienst Übersetzungen für alle anzulegenden
         Sprachen ausser der Quellsprache vor.
       - Die Vorschläge lassen sich vor dem Speichern ändern. Gespeichert wird
         erst nach Bestätigung.
       - Ohne API-Schlüssel oder bei einem Fehler des Dienstes entsteht das Label
         wie bisher mit dem Ausgangstext in allen Sprachen. Eine Meldung nennt
         den Grund.],
      [FA02, FA11], [3],

      [FA11\ Einstellungen],
      [- Einstellbar sind zu ladende Sprachen, anzulegende Sprachen,
         Quellsprache, Package-Verzeichnisse, Übersetzungsdienst mit API-Schlüssel
         und die Inline-Anzeige.
       - Geänderte Sprachen wirken nach dem nächsten Laden, ohne Neustart von
         Visual Studio.
       - Die Einstellungen bleiben über einen Neustart erhalten.],
      [--], [1],

      [FA12\ Erweiterte Suchmodi],
      [- Zusätzlich die Suchmodi Anything like that mit und ohne Beachtung der
         Gross- und Kleinschreibung sowie MatchWord.
       - Für denselben Datensatz und Suchbegriff liefert jeder Suchmodus dieselben
         Treffer in derselben Reihenfolge wie das bestehende Tool.],
      [FA01], [1],

      [FA13\ Label ersetzen],
      [- Alle Verwendungen eines Labels in beschreibbaren Models werden durch eine
         andere, bestehende Label-ID ersetzt.
       - Das Output Window meldet, wie viele Dateien geändert wurden.],
      [FA04], [1],

      [FA14\ Label-ID einfügen],
      [- Die vollständige ID des gewählten Labels steht danach an der
         Cursorposition im aktiven Editor.
       - Speichern und einfügen speichert vorher die Änderungen.
       - Ist kein Editor aktiv, erscheint ein Hinweis statt eines Fehlers.],
      [FA01], [1],

      [FA15\ Dateiüberwachung],
      [- Ändert sich eine beschreibbare Label-Datei von aussen, bietet die
         Extension das Neuladen an.
       - Eigene Schreibvorgänge lösen keine Meldung aus.],
      [--], [1],

      [FA16\ Shortcuts],
      [- Speichern, Label-ID einfügen, beides kombiniert und neues Label mit dem
         letzten Suchbegriff sind über Shortcuts erreichbar.
       - Die Shortcuts kollidieren nicht mit den Standard-Shortcuts von Visual Studio
         und lassen sich dort umbelegen.],
      [FA02, FA03, FA14], [1],

      [FA17\ Meldungsprotokoll],
      [- Fehler, Warnungen und Meldungen erscheinen in einem eigenen Bereich des
         Output Window, mit Art und Zeitpunkt.],
      [--], [1],
    ),
    caption: [Funktionale Anforderungen mit Akzeptanzkriterien (eigene
              Darstellung)]
  ) <funktionale_anforderungen>
]

=== Nicht-funktionale Anforderungen

#[
  #show figure: set align(left)
  #set text(size: 9.5pt)
  #figure(
    table(
      align: left,
      columns: (8.5em, 1fr, auto),
      table.header(
        [*Anforderung*], [*Akzeptanzkriterien*], [*Nachweis*],
      ),
      [NFA01\ Kompatibilität],
      [- Das VSIX-Paket lässt sich in Visual Studio 2026 und 2022 installieren.
       - Alle Testfälle bestehen unter Visual Studio 2026.
       - Unter Visual Studio 2022 hält das Testprotokoll das Ergebnis jedes
         Testfalls fest.],
      [Testfälle in D4],

      [NFA02\ Performance],
      [- Eine Suche über alle geladenen Labels dauert höchstens 500 ms, gemessen
         auf der Testumgebung mit den dort vorhandenen Label-Dateien.
       - Das Laden läuft im Hintergrund. Visual Studio meldet währenddessen keine
         blockierte Oberfläche.],
      [Messung in D4],

      [NFA03\ Erweiterbarkeit],
      [- Die Kernlogik hat keine Abhängigkeit zu Visual Studio, siehe
         @architektur.
       - Ein weiterer Übersetzungsdienst lässt sich ergänzen, ohne bestehende
         Komponenten zu ändern.],
      [Review],

      [NFA04\ Stabilität],
      [- Jeder Einstiegspunkt fängt Fehler ab und meldet sie im Output Window.
       - Eine beschädigte Label-Datei, ein fehlendes Package-Verzeichnis, ein
         nicht erreichbarer Übersetzungsdienst und eine gesperrte Datei führen zu
         einer Meldung. Visual Studio läuft weiter.],
      [Testfälle],

      [NFA05\ Wartbarkeit],
      [- Öffentliche Klassen und Methoden sind dokumentiert.
       - Der Build läuft ohne Warnungen.],
      [Review, Build],

      [NFA06\ Sicherheit],
      [- Der API-Schlüssel liegt verschlüsselt über die Data Protection API im
         Benutzerprofil.
       - Er erscheint weder im Klartext auf der Festplatte noch im Output Window
         noch im Repository.],
      [Review, Testfall],
    ),
    caption: [Nicht-funktionale Anforderungen mit Akzeptanzkriterien (eigene
              Darstellung)]
  ) <nicht_funktionale_anforderungen>
]

=== Organisatorische Anforderungen

#[
  #show figure: set align(left)
  #set text(size: 9.5pt)
  #figure(
    table(
      align: left,
      columns: (8.5em, 1fr, auto),
      table.header(
        [*Anforderung*], [*Akzeptanzkriterien*], [*Nachweis*],
      ),
      [OA01\ Bereitstellung],
      [- Ein einziges VSIX-Paket, das sich per Doppelklick ohne weitere Schritte
         installieren lässt.],
      [Testfall in D4],

      [OA02\ Dokumentation],
      [- Das Repository beschreibt Aufbau, Build und Erweiterungspunkte so, dass
         eine andere Person die Weiterentwicklung übernehmen kann.],
      [Review],

      [OA03\ Quellcodeablage],
      [- Das Repository enthält weder Code der Standardanwendung noch Code des
         bestehenden Tools.
       - Testdaten sind synthetisch.],
      [Prüfung vor jedem Commit],

      [OA04\ Ablösung des bestehenden Tools],
      [- Alle Anforderungen zu Z1 sind erfüllt, bevor BE-terna über den Umstieg
         entscheidet.],
      [Testprotokoll],
    ),
    caption: [Organisatorische Anforderungen mit Akzeptanzkriterien (eigene
              Darstellung)]
  ) <organisatorische_anforderungen>
]

=== Kann-Anforderungen <kann_anforderungen>

Die Initialisierung hat die Anforderungen aus der Themeneingabe übernommen und
verfeinert. Was sich mit den Erweiterungspunkten von Visual Studio zusätzlich
umsetzen lässt, zeigte erst die Machbarkeitsstudie, und wo solche Funktionen
ansetzen könnten, erst die Systemarchitektur. @abgrenzung hat diese Analyse deshalb bewusst
ins Konzept verschoben. Die Kann-Anforderungen zählen nicht zu den
Erfolgskriterien und stehen nicht im Realisierungsplan. Umgesetzt werden sie nur,
wenn nach AP4.1 Zeit bleibt, sonst fliessen sie in die Empfehlungen ein.

#[
  #show figure: set align(left)
  #set text(size: 9.5pt)
  #figure(
    table(
      align: left,
      columns: (8.5em, 1.6fr, 1fr),
      table.header(
        [*Kann-Anforderung*], [*Beschreibung*], [*Technische Grundlage*],
      ),
      [KA01\ Fehlende Übersetzungen hervorheben],
      [Label-IDs, denen in einer der konfigurierten Sprachen eine Übersetzung
       fehlt, sind im Code farblich markiert.],
      [Tags zum Einfärben von Text, siehe @machbarkeitsbeurteilung.],

      [KA02\ Markierung am Rand],
      [Zeilen mit unvollständig übersetzten Labels tragen eine Markierung in der
       Margin des Editors. Ein Klick darauf öffnet das Label im Tool Window.],
      [Margin, siehe @machbarkeitsbeurteilung.],

      [KA03\ Platzhalter in der Inline-Anzeige],
      [Steht eine Label-ID in einem Aufruf von `strFmt`, ersetzt die
       Inline-Anzeige die Platzhalter `%1`, `%2` im Labeltext durch die Namen der
       übergebenen Argumente, etwa `{customerName}`.],
      [Inline-Anzeige aus FA06. Die Argumente müssen sich aus dem Code der Zeile
       lesen lassen.],

      [KA04\ Labels des geöffneten Elements],
      [Das Tool Window listet alle Labels, die im geöffneten Element vorkommen, mit
       ihren Übersetzungen. Das gilt für den X++-Editor wie für den Designer.],
      [Label-Erkennung und Label Store, im Designer über das Selection
       Tracking.],

      [KA05\ Übersetzungen im Designer],
      [Wählt der Entwickler im Designer ein Element, zeigt das Tool Window die
       Übersetzungen seiner Label-Eigenschaften wie Label und Help Text.],
      [Selection Tracking, Lesen belegt.],

      [KA06\ Suche im Code nach Labeltext],
      [Eine Suche im geöffneten Dokument findet eine Label-ID auch über ihren Text
       in einer der konfigurierten Sprachen. Die Suche nach «Lieferadresse» findet
       so die Stelle mit der zugehörigen Label-ID.],
      [Ob sich die Suche von Visual Studio dafür erweitern lässt, ist nicht
       untersucht. Rückfall ist ein eigener Command.],

      [KA07\ Vorschlag am String-Literal],
      [Steht der Cursor auf einem hardcodierten String-Literal, bietet der Editor
       die Extraktion aus FA09 als Vorschlag an.],
      [Suggested Actions des Editors über MEF, nicht untersucht.],

      [KA08\ Duplicates beim Anlegen],
      [Gibt es in der Ziel-Label-Datei bereits ein Label mit demselben Text, weist
       die Extension beim Anlegen darauf hin und bietet es zur Verwendung an.],
      [Suche aus FA01 über den Label Store.],

      [KA09\ Unbenutzte Labels],
      [Die Extension listet Labels beschreibbarer Models auf, die nirgends
       verwendet werden.],
      [Verwendungssuche aus FA04, bei vielen Labels entsprechend langsam.],

      [KA10\ Kontextbezogene Übersetzung],
      [GitHub Copilot in Visual Studio lässt sich als weiterer Übersetzungsdienst
       wählen. Er erhält neben dem Text den umgebenden Code, etwa Element, Feld
       und Methode, und sucht sich fehlenden Kontext selbst im Code. Daraus
       schlägt er Übersetzungen vor, die zum Verwendungszweck passen.],
      [Schnittstelle des Übersetzungsdienstes, siehe NFA03.],
    ),
    caption: [Kann-Anforderungen (eigene Darstellung)]
  ) <kann_anforderungen_tabelle>
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
    caption: [Use Case UC-01 (eigene Darstellung)]
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
    caption: [Use Case UC-02 (eigene Darstellung)]
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
    caption: [Use Case UC-03 (eigene Darstellung)]
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
    caption: [Fachklassen (eigene Darstellung)]
  ) <fachklassen>
]

== Systemarchitektur <architektur>

Die Lösung besteht aus drei Projekten. Die Kernlogik enthält alles, was ohne
Visual Studio auskommt, und zielt auf .NET Standard 2.0. Die Extension zielt auf
.NET Framework 4.8, weil sie im Prozess von Visual Studio läuft, und bindet die
Kernlogik als Assembly ein. Mit .NET Framework 4.8 lief auch die Laufzeitprobe auf
der Testumgebung. Ein Testprojekt prüft die Kernlogik ohne Visual Studio und ohne
Dynamics 365. Es zielt auf .NET 8 und auf .NET Framework 4.8. Unter .NET 8 laufen
die Tests auch ausserhalb von Windows, unter .NET Framework 4.8 auf derselben
Laufzeit wie im Betrieb.

Weil .NET Standard weder WPF noch das Visual Studio SDK kennt, kann die Kernlogik
nicht versehentlich von Visual Studio abhängig werden. Damit ist Z8 in der Struktur
der Lösung verankert. Ausgeliefert wird ein einziges VSIX-Paket mit den Assemblies
der Extension und der Kernlogik.

#figure(
  image("../diagrams/Systemarchitektur.png", width: 100%),
  caption: [Systemarchitektur (eigene Darstellung)]
) <systemarchitektur>

#[
  #show figure: set align(left)
  #set text(size: 10pt)
  #figure(
    table(
      align: left,
      columns: (auto, 1fr, auto),
      table.header(
        [*Komponente*], [*Aufgabe*], [*Anforderungen*],
      ),
      table.cell(colspan: 3)[*Kernlogik*],
      [Model-Suche],
      [Findet die Models über ihre Descriptor-Dateien und erkennt schreibgeschützte
       Models.],
      [FA02, FA03],
      [Label-Dateien],
      [Liest und schreibt die Label-Dateien pro Sprache. Liest Labels aus
       kompilierten Ressourcen schreibgeschützt.],
      [FA01 -- FA03],
      [Label Store],
      [Hält alle geladenen Labels im Arbeitsspeicher, nach Label-ID indiziert.
       Lädt im Hintergrund.],
      [NFA02],
      [Suche und Ranking],
      [Alle Suchmodi, Treffer nach Relevanz sortiert.],
      [FA01, FA12],
      [Verwendungssuche],
      [Textsuche in den XML-Dateien der Elemente, Aktualisierung der Referenzen
       beim Kopieren, Verschieben und Ersetzen.],
      [FA03, FA04, FA13],
      [Dateiüberwachung],
      [Meldet Änderungen an Label-Dateien von aussen und pausiert während eigener
       Schreibvorgänge.],
      [FA15],
      [Übersetzungsdienst],
      [Schnittstelle mit austauschbarer Implementierung, zuerst für DeepL.],
      [FA10],
      table.cell(colspan: 3)[*Extension*],
      [Tool Window],
      [Suche, Trefferliste, Detailansicht, Bearbeiten, Verwendungen.],
      [FA01 -- FA04, FA11 -- FA13],
      [Commands],
      [Suche aus dem Editor, Öffnen im Panel, Label-ID einfügen, Extraktion,
       Shortcuts.],
      [FA07 -- FA09, FA14, FA16],
      [Label-Erkennung],
      [Findet Label-IDs im X++-Editor über die Classification der Developer
       Tools.],
      [FA05, FA06, FA08],
      [QuickInfo-Quelle],
      [Tooltip mit allen konfigurierten Sprachen.],
      [FA05],
      [Inline-Anzeige],
      [Einblendung der Übersetzung im Code.],
      [FA06],
      [Auswahl im Designer],
      [Liest über das Selection Tracking das gewählte Element und seine
       Label-Eigenschaften.],
      [FA09],
      [Einstellungen],
      [Sprachen, Package-Verzeichnisse, Übersetzungsdienst und API-Schlüssel.],
      [FA11, NFA06],
      [Meldungen],
      [Ausgabe im Output Window.],
      [FA17],
    ),
    caption: [Komponenten und ihre Anforderungen (eigene Darstellung)]
  ) <komponenten>
]

=== Technische Festlegungen

#[
  #show figure: set align(left)
  #set text(size: 9.5pt)
  #figure(
    table(
      align: left,
      columns: (auto, 1.6fr, 1fr),
      table.header(
        [*Thema*], [*Festlegung*], [*Stand*],
      ),
      [Package-Verzeichnis],
      [Konfigurierbar, mehrere Verzeichnisse möglich. Vorbelegt mit dem
       Verzeichnis der Unified Developer Experience im Benutzerprofil, bei
       mehreren Versionen mit der neuesten. Das bestehende Tool kennt nur die
       fest eingestellten Pfade der klassischen Entwicklungs-VM.],
      [Auf der Testumgebung liegen drei Versionen. Zu prüfen, siehe unten.],

      [Kompilierte Labels],
      [Labels ohne Label-Datei liest das bestehende Tool aus kompilierten
       Ressourcen, indem es die Assembly lädt. Im Prozess von Visual Studio bliebe
       die Datei bis zum Schliessen geladen @ms-assembly-unload, und ein erneutes
       Laden lieferte die alte Fassung @ms-assembly-loadfrom. Die Kernlogik liest
       die Ressourcen deshalb direkt aus der Datei, ohne die Assembly zu laden.],
      [Zu prüfen in 3.2.],

      [Laden],
      [Im Hintergrund, sobald das Tool Window oder die erste X++-Datei geöffnet
       wird. Bis dahin zeigt der Tooltip keinen Eintrag und das Tool Window einen
       Hinweis.],
      [NFA02],

      [Schreiben],
      [Im selben Format wie das bestehende Tool, also UTF-8 mit BOM und leere
       Texte als ein Leerzeichen. Geschrieben wird in eine temporäre Datei, die
       danach die alte ersetzt.],
      [Format aus dem bestehenden Tool übernommen.],

      [Neue Label-IDs],
      [Der Buchstabe `L` und acht kryptografisch zufällige Bytes als 16
       hexadezimale Ziffern in Grossbuchstaben, etwa `L3F2A9C15B8047DE1`. Die
       Label-Datei steht wie bisher davor, die User-ID entfällt.],
      [Aus der Logik übernommen, die im Betrieb bereits verwendet wird.],

      [Label-Formen],
      [Die alte Form ohne Doppelpunkt, etwa `@SYS12345`, kommt im Code und in
       Eigenschaften noch an vielen Stellen vor. In der Label-Datei steht sie mit
       vollständiger ID samt `@`. Suche, Tooltip, Inline-Anzeige, Öffnen im Panel,
       Auswahl im Designer, Bearbeiten, Verwendungssuche, Ersetzen, Kopieren und
       Verschieben verarbeiten beide Formen. Neue Labels entstehen nur in der
       neuen Form, auch beim Kopieren und Verschieben.],
      [Das bestehende Tool verarbeitet beide Formen.],

      [Label-Erkennung],
      [Über die Classifications `"X++ Modern Label"` und `"X++ Legacy Label"`.
       Die Spanne beginnt mit dem Anführungszeichen. Beide Formen der Label-ID
       werden verarbeitet, mit und ohne Doppelpunkt. Die Namen der
       Classifications stehen an einer Stelle, siehe R07.],
      [Neue Form belegt, alte Form nur im Properties Window beobachtet.],

      [Entwicklung ohne X++-Editor],
      [Auf dem privaten Gerät gibt es keinen X++-Editor. Im Debug-Build hängen sich
       Tooltip und Inline-Anzeige zusätzlich an Textdateien mit der Endung `.xpp`
       an und erkennen Labels über einen Mustervergleich. Derselbe Mustervergleich
       ist der Rückfall aus R07.],
      [Senkt die Zahl der Durchgänge auf der Testumgebung, siehe R04.],

      [Tool Window],
      [Remote UI des neuen Modells. Fehlt dort ein benötigtes Steuerelement, folgt
       ein klassisches Tool Window mit WPF, das im selben Prozess ebenfalls möglich
       ist.],
      [Im Spike nicht geprüft. Zu prüfen in 3.1.],

      [Tooltip],
      [Eigene QuickInfo-Quelle über MEF. Ihr Eintrag erscheint im selben Tooltip
       wie der Eintrag der Developer Tools.],
      [Belegt, siehe @machbarkeitsbeurteilung.],

      [Inline-Anzeige],
      [Platz oberhalb der Zeile über dieselbe Technik, mit der die Developer Tools
       ihre Referenzanzeige zeichnen.],
      [Nicht erprobt, siehe R08. Z2 ist über FA05 auch ohne sie erreicht.],

      [Extraktion im Editor],
      [Ersetzt den markierten Text im Textpuffer des Editors. Rückgängig machen
       und Speichern bleiben bei Visual Studio und den Developer Tools.],
      [Standardschnittstelle des Editors, nicht eigens erprobt.],

      [Extraktion im Properties Window],
      [Lesen über das Selection Tracking. Geschrieben wird direkt in die
       XML-Datei des Elements, wie in @machbarkeitsbeurteilung festgelegt. Offen ist,
       wie sich ein geöffneter Designer verhält, wenn sich die Datei darunter
       ändert.],
      [Lesen belegt. Schreiben zu prüfen, siehe unten.],

      [Sprung an die Fundstelle],
      [Die Verwendungssuche findet die Label-ID in der XML-Datei des Elements.
       Der X++-Editor arbeitet dagegen auf einer `.xpp`-Datei im Ordner
       XppSource, siehe @befundprotokoll. Angestrebt ist der Sprung in den
       X++-Editor an die entsprechende Stelle. Rückfall ist das Öffnen der
       XML-Datei an der Zeile.],
      [Zu prüfen, siehe unten.],

      [API-Schlüssel],
      [Verschlüsselt über die Data Protection API von Windows im Benutzerprofil,
       nie im Repository.],
      [NFA06],

      [Error Boundary],
      [Jeder Einstiegspunkt, also Command, MEF-Teil und Hintergrundaufgabe, fängt
       Fehler ab und meldet sie im Output Window.],
      [NFA04, Nachweis im Testkonzept.],
    ),
    caption: [Technische Festlegungen (eigene Darstellung)]
  ) <festlegungen>
]

#todo[Auf der Testumgebung prüfen, wie sich ein geöffneter Designer verhält,
wenn sich die XML-Datei des Elements darunter ändert. Dazu ein EDT im Designer
öffnen, sein Label in der XML-Datei mit einem Texteditor ändern und festhalten, was
Visual Studio macht. Das Ergebnis entscheidet, ob die Extraktion aus dem Properties
Window direkt in die Datei schreiben kann.]

#todo[Auf der Testumgebung prüfen, welche der drei Versionen des
Package-Verzeichnisses die Developer Tools verwenden und woran die Extension das
erkennen kann. Davon hängt die Vorbelegung der Einstellung ab.]

#todo[Auf der Testumgebung prüfen, ob unter XppSource für jedes Element eine
`.xpp`-Datei liegt oder nur für geöffnete, und ob sich eine Fundstelle in der
XML-Datei auf eine Zeile im X++-Editor übertragen lässt. Davon hängt ab, wohin
ein Klick in der Verwendungssuche führt.]


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
    caption: [Testobjekte (eigene Darstellung)]
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
    caption: [Testfälle (eigene Darstellung)]
  ) <testfaelle>
]

== Einführung und Betrieb

// Einführungs-, Migrations- und Betriebskonzept bewusst in einem kurzen
// Abschnitt. Eine Datenmigration entfällt, weil die Label-Dateien unverändert
// bleiben. Die produktive Einführung entscheidet BE-terna nach Projektabschluss.

== GUI-Design

== Realisierungsplan <realisierungsplan>

Die Realisierung folgt den Stufen aus @projektziele. Die Kernlogik kommt vor der
Oberfläche, weil Tool Window, Tooltip und Extraktion auf denselben Label Store
zugreifen. Jeder Tag endet mit einem Ergebnis, das sich prüfen lässt. Gegenüber
@terminplan_soll wandert die Suche aus dem Editor von 3.6 zu 3.7, damit 3.6 ganz
der Anzeige im Editor gehört.

#[
  #show figure: set align(left)
  #set text(size: 9.5pt)
  #figure(
    table(
      align: left,
      columns: (auto, 1.5fr, auto, 1.5fr),
      table.header(
        [*Tag*], [*Inhalt*], [*Anforde-\ rungen*], [*Ergebnis*],
      ),
      table.cell(colspan: 4)[*Grundgerüst*],
      [3.1\ 09.10.],
      [Solution mit drei Projekten. Build und Paketierung mit den Erkenntnissen aus
       dem Spike. Output Window, Error Boundary, leeres Tool Window.],
      [FA17, NFA04],
      [Die Extension lädt in Visual Studio 2026 und öffnet ihr Tool Window. Ein
       absichtlich ausgelöster Fehler erscheint im Output Window, Visual Studio
       läuft weiter.],

      table.cell(colspan: 4)[*Stufe 1, Feature Parity*],
      [3.2\ 10.10.],
      [Model-Suche, Label-Dateien lesen und schreiben, kompilierte Labels,
       Label Store, Dateiüberwachung, eigener Testdatensatz.],
      [FA15, NFA02],
      [Unit Tests lesen und schreiben Label-Dateien ohne Verlust von Kommentaren
       und Kodierung. Das Laden läuft im Hintergrund.],
      [3.3\ 15.10.],
      [Alle Suchmodi mit Ranking.],
      [FA01, FA12, NFA02],
      [Unit Tests je Suchmodus. Eine Suche über den Testdatensatz dauert wenige
       hundert Millisekunden.],
      [3.4\ 16.10.],
      [Tool Window mit Suchfeld, Trefferliste und Detailansicht, Bearbeiten und
       Speichern.],
      [FA01, FA03],
      [Ein Label lässt sich im Tool Window finden, in allen Sprachen ändern und
       speichern.],
      [3.5\ 17.10.],
      [Anlegen, Löschen, Kopieren und Verschieben mit Referenzaktualisierung,
       Ersetzen, Label-ID einfügen, Einstellungen, Shortcuts.],
      [FA02, FA03, FA11, FA13, FA14, FA16],
      [Alle Funktionen aus @ist_funktionen ausser der Verwendungssuche sind im
       Tool Window verfügbar. Unit Tests decken die Referenzaktualisierung ab.],

      table.cell(colspan: 4)[*Stufe 2, Kontextwechsel beseitigen*],
      [3.6\ 19.10.],
      [Label-Erkennung, Tooltip mit allen Sprachen, Inline-Anzeige, Ersatz für den
       X++-Editor im Debug-Build.],
      [FA05, FA06],
      [Der Tooltip zeigt alle konfigurierten Sprachen, auf dem privaten Gerät über
       den Ersatz, auf der Testumgebung im X++-Editor.],
      [3.7\ 20.10.],
      [Suche aus dem Editor, Öffnen im Panel, Verwendungssuche mit Sprung an die
       Fundstelle.],
      [FA04, FA07, FA08],
      [Von einer Label-ID im Code führt ein Command ins Tool Window. Ein Klick auf
       eine Fundstelle öffnet die Datei an der richtigen Zeile.],

      table.cell(colspan: 4)[*Stufe 3, erweiterte Features*],
      [3.8\ 21.10.],
      [Extraktion im Editor und im Properties Window, Übersetzungsdienst,
       API-Schlüssel.],
      [FA09, FA10, NFA06],
      [Ein markierter Text wird zum Label. Die Übersetzungen werden vorgeschlagen
       und lassen sich vor dem Speichern ändern.],

      table.cell(colspan: 4)[*Tests und Abschluss*],
      [4.1\ 22.10.],
      [Restarbeiten und Fehler aus den Durchgängen auf der Testumgebung.],
      [--],
      [Keine offenen Fehler, die eine Anforderung verletzen.],
      [4.2\ 23.10.],
      [Testfälle auf der Testumgebung unter Visual Studio 2026 und 2022,
       Testprotokoll.],
      [NFA01],
      [Testprotokoll mit dem Ergebnis jedes Testfalls.],
      [4.3\ 24.10.],
      [Code Freeze, VSIX-Paket für die Abgabe, Beschreibung von Aufbau, Build und
       Erweiterungspunkten im Repository.],
      [OA01, OA02],
      [Das Paket lässt sich ohne Nacharbeit installieren. Eine andere Person kann
       die Solution anhand des Repositorys bauen.],
    ),
    caption: [Realisierungsplan (eigene Darstellung)]
  ) <realisierungsplan_tabelle>
]

NFA03, NFA05 und OA03 betreffen jeden Tag und haben deshalb keinen eigenen Platz
im Plan. OA04 folgt erst nach Projektabschluss.

Was nur mit dem X++-Editor oder dem Designer geprüft werden kann, wird in
Durchgängen auf der Testumgebung gesammelt. Der erste Durchgang liegt direkt nach
dem Grundgerüst, damit sich die grössten Unsicherheiten früh zeigen.

#[
  #show figure: set align(left)
  #set text(size: 10pt)
  #figure(
    table(
      align: left,
      columns: (auto, auto, 1fr),
      table.header(
        [*Durchgang*], [*Nach*], [*Prüft*],
      ),
      [D1], [3.1],
      [Laden neben den Developer Tools, siehe R06. Verhalten eines geöffneten
       Designers, wenn sich die XML-Datei des Elements darunter ändert. Ablage
       des X++-Codes unter XppSource. Beides lässt sich auch ohne Extension von
       Hand prüfen.],
      [D2], [3.7],
      [Label-Erkennung, Tooltip, Inline-Anzeige, Suche aus dem Editor, Öffnen im
       Panel, Verwendungssuche.],
      [D3], [3.8],
      [Extraktion im Editor und im Properties Window, Übersetzungsdienst.],
      [D4], [4.2],
      [Alle Testfälle, zusätzlich unter Visual Studio 2022.],
    ),
    caption: [Durchgänge auf der Testumgebung (eigene Darstellung)]
  ) <durchgaenge>
]

Reicht die Zeit an einem Tag nicht, wandert der Rest in AP4.1. Scheitert das
Schreiben aus dem Properties Window in D1, bleibt bis zum 21.10. Zeit, den
Rückfall über die Eigenschaft des gewählten Elements vorzubereiten. Die
Property Descriptors melden sie als beschreibbar, erprobt ist das nicht.
