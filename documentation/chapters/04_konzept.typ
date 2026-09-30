#import "../helpers.typ": todo

// Nummerierte alternative Abläufe in den Use-Case-Beschreibungen
#let ablauf(..items) = grid(columns: (2em, 1fr), row-gutter: 0.6em, ..items)

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
       - Ohne Markierung gilt der Inhalt des String Literals unter dem
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
      [- Im Editor wird ein markiertes String Literal als Ganzes zu einem neuen
         Label und enthält danach die Label-ID. Rückgängig machen
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
         Quellsprache, Metadaten-Konfiguration, weitere Package-Verzeichnisse,
         Übersetzungsdienst mit API-Schlüssel und die Inline-Anzeige.
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
       - Eigene Schreibvorgänge lösen keine Meldung aus.
       - Nicht gespeicherte Labels bleiben beim Neuladen erhalten.],
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
      [TC32 in D4],

      [NFA02\ Performance],
      [- Eine Suche über alle geladenen Labels dauert höchstens 500 ms, gemessen
         auf der Testumgebung mit den dort vorhandenen Label-Dateien.
       - Das Laden läuft im Hintergrund. Visual Studio meldet währenddessen keine
         blockierte Oberfläche.],
      [TC31 in D4],

      [NFA03\ Erweiterbarkeit],
      [- Die Kernlogik hat keine Abhängigkeit zu Visual Studio, siehe
         @architektur.
       - Ein weiterer Übersetzungsdienst lässt sich ergänzen, ohne bestehende
         Komponenten zu ändern.],
      [TC33],

      [NFA04\ Stabilität],
      [- Jeder Einstiegspunkt fängt Fehler ab und meldet sie im Output Window.
       - Eine beschädigte Label-Datei, ein fehlendes Package-Verzeichnis, ein
         nicht erreichbarer Übersetzungsdienst und eine gesperrte Datei führen zu
         einer Meldung. Visual Studio läuft weiter.],
      [TC10, TC19,\ TC21, TC26],

      [NFA05\ Wartbarkeit],
      [- Öffentliche Klassen und Methoden sind dokumentiert.
       - Der Build läuft ohne Warnungen.],
      [TC34],

      [NFA06\ Sicherheit],
      [- Der API-Schlüssel liegt verschlüsselt über die Data Protection API im
         Benutzerprofil.
       - Er erscheint weder im Klartext auf der Festplatte noch im Output Window
         noch im Repository.],
      [TC20],
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
      [TC12, in D4\ wiederholt],

      [OA02\ Dokumentation],
      [- Das Repository beschreibt Aufbau, Build und Erweiterungspunkte so, dass
         eine andere Person die Weiterentwicklung übernehmen kann.],
      [TC35],

      [OA03\ Quellcodeablage],
      [- Das Repository enthält weder Code der Standardanwendung noch Code des
         bestehenden Tools.
       - Testdaten sind synthetisch.],
      [TC35, Prüfung\ vor jedem Commit],

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
       in einer der konfigurierten Sprachen. Die Suche nach "Lieferadresse" findet
       so die Stelle mit der zugehörigen Label-ID.],
      [Ob sich die Suche von Visual Studio dafür erweitern lässt, ist nicht
       untersucht. Fallback ist ein eigener Command.],

      [KA07\ Vorschlag am String Literal],
      [Steht der Cursor auf einem hardcodierten String Literal, bietet der Editor
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

Menschlicher Akteur ist in allen Use Cases der Entwickler. Die Developer Tools und
der Übersetzungsdienst nehmen als Systeme an einzelnen Use Cases teil. Die Use
Cases fassen die Anforderungen aus @funktionale_anforderungen zu Aufgaben aus Sicht
des Entwicklers zusammen. FA17 betrifft alle und hat keinen eigenen Use Case.

#figure(
  image("../diagrams/UseCases.png", width: 9.5cm),
  caption: [Use-Case-Diagramm (eigene Darstellung)]
) <use_case_diagramm>

#[
  #show figure: set align(left)
  #show figure: set block(breakable: false)
  #set text(size: 10pt)
  #figure(
    table(
      align: left,
      columns: (auto, 1fr, auto, auto),
      table.header(
        [*UC*], [*Use Case*], [*Anforderungen*], [*Stufe*],
      ),
      [UC01], [Label suchen], [FA01, FA07, FA12], [1, 2],
      [UC02], [Label anlegen], [FA02, FA10, FA16], [1, 3],
      [UC03], [Label bearbeiten], [FA03, FA08, FA16], [1, 2],
      [UC04], [Verwendungen anzeigen], [FA04], [1, 2],
      [UC05], [Label ersetzen], [FA13], [1],
      [UC06], [Label-ID einfügen], [FA14, FA16], [1],
      [UC07], [Übersetzungen im Code ansehen], [FA05, FA06], [2],
      [UC08], [Hardcodierten Text extrahieren], [FA09], [3],
      [UC09], [Einstellungen ändern], [FA11], [1],
      [UC10], [Label-Dateien neu laden], [FA15], [1],
    ),
    caption: [Use Cases mit Anforderungen und Stufe (eigene Darstellung)]
  ) <use_cases>
]

== Use-Case-Beschreibungen

Ausführlich beschrieben sind die vier Use Cases, die mehrere Komponenten oder ein
externes System einbeziehen. Für die übrigen legen die Akzeptanzkriterien in
@funktionale_anforderungen das Verhalten fest.

#[
  #show figure: set align(left)
  #set text(size: 9.5pt)
  #figure(
    table(
      align: left,
      columns: (auto, 1fr),
      [*Nummer*], [UC01],
      [*Kurzbeschreibung*],
      [Der Entwickler sucht ein Label über Text, Kommentar oder ID, im Tool Window
       oder direkt aus dem Editor.],
      [*Akteure*], [Entwickler],
      [*Auslöser*],
      [Der Entwickler öffnet das Tool Window oder ruft die Suche im X++-Editor
       über das Kontextmenü oder einen Shortcut auf.],
      [*Vorbedingungen*], [Die Labels sind geladen, siehe @sequenz_laden.],
      [*Typischer Ablauf*],
      [+ Der Entwickler öffnet das Tool Window und gibt einen Suchbegriff ein.
       + Die Trefferliste zeigt jedes passende Label einmal, nach Relevanz
         sortiert.
       + Der Entwickler wählt einen Treffer. Die Detailansicht zeigt Text und
         Kommentar in allen geladenen Sprachen.],
      [*Alternative Abläufe*],
      [#ablauf(
        [1a], [Der Entwickler markiert im X++-Editor einen Text und ruft die Suche
               auf. Das Tool Window öffnet sich und sucht mit dem markierten
               Text.],
        [1b], [Wie 1a, aber ohne Markierung. Gesucht wird mit dem Inhalt des
               String Literals unter dem Cursor.],
        [2a], [Die Labels sind noch nicht geladen. Das Tool Window zeigt einen
               Hinweis statt der Trefferliste.],
        [2b], [Kein Treffer. Der Entwickler legt ein neues Label an, weiter mit
               UC02.],
      )],
      [*Nachbedingungen*],
      [Ein Label ist gewählt. Der Entwickler fügt seine ID ein (UC06) oder
       bearbeitet es (UC03). Die Suche selbst ändert keine Datei.],
      [*Verknüpfungen*], [--],
      [*Anforderungen*], [FA01, FA07, FA12, NFA02],
      [*Testfälle*], [TC04, TC13, TC24],
      [*Stufe*], [1, aus dem Editor 2],
    ),
    caption: [Use Case UC01 Label suchen (eigene Darstellung)]
  ) <uc_01>
]

#[
  #show figure: set align(left)
  #set text(size: 9.5pt)
  #figure(
    table(
      align: left,
      columns: (auto, 1fr),
      [*Nummer*], [UC02],
      [*Kurzbeschreibung*],
      [Der Entwickler legt ein Label in einer beschreibbaren Label-Datei an. Der
       Übersetzungsdienst schlägt die Übersetzungen vor.],
      [*Akteure*], [Entwickler, Übersetzungsdienst],
      [*Auslöser*],
      [Knopf im Tool Window oder Shortcut für ein neues Label, auch aus UC01 ohne
       Treffer und aus UC08.],
      [*Vorbedingungen*],
      [Die Labels sind geladen. Mindestens ein beschreibbares Model hat eine
       Label-Datei.],
      [*Typischer Ablauf*],
      [+ Der Entwickler wählt im Tool Window eine Label-Datei und legt ein neues
         Label an, per Knopf oder Shortcut.
       + Alle anzulegenden Sprachen sind mit dem letzten Suchbegriff vorbelegt.
       + Die Extension schickt den Text in der Quellsprache an den
         Übersetzungsdienst und ersetzt die Vorbelegung der übrigen Sprachen durch
         die Vorschläge.
       + Der Entwickler prüft und ändert die Texte und bestätigt.
       + Die Extension erzeugt die Label-ID und schreibt das Label in die Datei
         jeder anzulegenden Sprache.],
      [*Alternative Abläufe*],
      [#ablauf(
        [3a], [Kein API-Schlüssel hinterlegt oder der Dienst meldet einen Fehler.
               Alle Sprachen behalten den Ausgangstext, das Output Window nennt
               den Grund.],
        [4a], [Der Entwickler bricht ab. Keine Datei wird geändert.],
        [5a], [Eine Datei lässt sich nicht schreiben, etwa weil sie gesperrt ist.
               Das Output Window nennt die Datei. Das Label bleibt als nicht
               gespeichert im Label Store, und der Entwickler speichert es erneut,
               sobald die Datei frei ist.],
      )],
      [*Nachbedingungen*],
      [Das Label steht in allen anzulegenden Sprachen in der Label-Datei und im
       Label Store. Dynamics 365 kompiliert die Datei ohne Fehler.],
      [*Verknüpfungen*], [Wird von UC08 eingebunden.],
      [*Anforderungen*], [FA02, FA10, FA16, NFA04, NFA06],
      [*Testfälle*], [TC03, TC11, TC15, TC29, TC30],
      [*Stufe*], [1, die Übersetzung 3],
    ),
    caption: [Use Case UC02 Label anlegen (eigene Darstellung)]
  ) <uc_02>
]

#[
  #show figure: set align(left)
  #set text(size: 9.5pt)
  #figure(
    table(
      align: left,
      columns: (auto, 1fr),
      [*Nummer*], [UC07],
      [*Kurzbeschreibung*],
      [Der Entwickler sieht zu einer Label-ID im X++-Editor die Übersetzungen
       aller konfigurierten Sprachen, im Tooltip und oberhalb der Zeile.],
      [*Akteure*], [Entwickler, Developer Tools],
      [*Auslöser*],
      [Eine X++-Datei wird im Editor angezeigt, für die Inline-Anzeige. Der
       Entwickler überfährt eine Label-ID, für den Tooltip.],
      [*Vorbedingungen*], [Eine X++-Datei ist im Editor geöffnet.],
      [*Typischer Ablauf*],
      [+ Oberhalb jeder Zeile mit einer Label-ID blendet die Extension die
         Übersetzungen ein.
       + Der Entwickler überfährt eine Label-ID mit der Maus.
       + Die Extension erkennt das Label über die Classification der Developer
         Tools und liest es aus dem Label Store.
       + Der Tooltip zeigt zusätzlich zum Eintrag der Developer Tools alle
         konfigurierten Sprachen.],
      [*Alternative Abläufe*],
      [#ablauf(
        [1a], [Die Inline-Anzeige ist ausgeschaltet. Es erscheint nur der
               Tooltip.],
        [3a], [Die Labels sind noch nicht geladen. Die Extension zeigt nichts an
               und ergänzt die Inline-Anzeige, sobald das Laden abgeschlossen
               ist.],
        [3b], [Die Label-ID ist unbekannt. Der Eintrag im Tooltip sagt das.],
        [4a], [Einer Sprache fehlt die Übersetzung. Sie ist als fehlend
               markiert.],
      )],
      [*Nachbedingungen*], [Keine Datei wird geändert.],
      [*Verknüpfungen*], [--],
      [*Anforderungen*], [FA05, FA06, NFA04],
      [*Testfälle*], [TC02, TC22, TC23],
      [*Stufe*], [2],
    ),
    caption: [Use Case UC07 Übersetzungen im Code ansehen (eigene Darstellung)]
  ) <uc_07>
]

#[
  #show figure: set align(left)
  #set text(size: 9.5pt)
  #figure(
    table(
      align: left,
      columns: (auto, 1fr),
      [*Nummer*], [UC08],
      [*Kurzbeschreibung*],
      [Der Entwickler wandelt einen hardcodierten Text im Code oder in einer
       Eigenschaft im Designer in ein neues Label um.],
      [*Akteure*], [Entwickler, Developer Tools, über UC02 der Übersetzungsdienst],
      [*Auslöser*],
      [Der Entwickler ruft den Command für die Extraktion im X++-Editor oder im
       Designer auf.],
      [*Vorbedingungen*],
      [Die Labels sind geladen. Die Datei oder das Element gehört zu einem
       beschreibbaren Model.],
      [*Typischer Ablauf*],
      [+ Der Entwickler markiert im X++-Editor ein String Literal und ruft die
         Extraktion auf.
       + Die Extension bestimmt über die Classification `"X++ String"` das
         String Literal an der Markierung.
       + Das Tool Window öffnet ein neues Label mit dem Inhalt des String Literals
         als Text und schlägt eine Label-Datei des Models vor, zu dem die Datei
         gehört.
       + Weiter wie UC02 ab Schritt 3.
       + Die Extension ersetzt den Inhalt des String Literals durch die
         Label-ID.],
      [*Alternative Abläufe*],
      [#ablauf(
        [1a], [Der Entwickler wählt im Designer ein Element und ruft die
               Extraktion auf. Die Extension bietet dessen Label-Eigenschaften mit
               hardcodiertem Text an, etwa Label und Help Text. Nach der Wahl geht
               es mit Schritt 3 weiter. In Schritt 5 schreibt die Extension die
               Label-ID in die XML-Datei des Elements, siehe @festlegungen. Der
               Designer meldet die Änderung, und der Entwickler lädt das Element
               neu. Hat das Element ungespeicherte Änderungen, bietet die
               Extension vorher an, es zu speichern.],
        [2a], [An der Markierung liegt kein String Literal. Ein Hinweis erscheint,
               nichts wird geändert.],
        [3a], [Das Model hat keine Label-Datei. Der Entwickler wählt die
               Label-Datei selbst.],
        [4a], [Der Entwickler bricht ab. Code und Dateien bleiben unverändert.],
        [5a], [Der Entwickler macht die Änderung rückgängig. Der alte Text steht
               wieder im Code, das Label bleibt in der Label-Datei.],
      )],
      [*Nachbedingungen*],
      [Das Label existiert, und die Fundstelle enthält seine ID.],
      [*Verknüpfungen*], [Bindet UC02 ein.],
      [*Anforderungen*], [FA09, FA02, FA10],
      [*Testfälle*], [TC27, TC28],
      [*Stufe*], [3],
    ),
    caption: [Use Case UC08 Hardcodierten Text extrahieren (eigene Darstellung)]
  ) <uc_08>
]

== Systemarchitektur <architektur>

Die Extension heisst BE-LabelExtension. Die Lösung besteht aus drei Projekten. Die Kernlogik enthält alles, was ohne
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
      [Sprachen, Metadaten-Konfiguration, weitere Package-Verzeichnisse,
       Übersetzungsdienst und API-Schlüssel.],
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
      [Konfigurierbar, mehrere Verzeichnisse möglich. In der Unified Developer
       Experience gilt genau eine aktive Metadaten-Konfiguration der Developer
       Tools. Sie nennt einen Ordner für die eigenen Models und Ordner für
       Referenz-Metadaten, darunter das entpackte `PackagesLocalDirectory` mit den
       Models von Microsoft @ms-ude-tools. Die Konfigurationen liegen als je eine
       JSON-Datei im Ordner `XPPConfig` unter
       `%LOCALAPPDATA%\Microsoft\Dynamics365` @bucher-ude. Der Ordner für eigene
       Models steht dort unter `ModelStoreFolder`, die Referenz-Metadaten unter
       `FrameworkDirectory` und `ReferencePackagesPaths`. Welche Konfiguration
       aktiv ist, lässt sich an diesen Dateien nicht ablesen. Der Entwickler wählt
       sie deshalb in den Einstellungen, bei nur einer ist sie vorgewählt. Die
       Extension liest die Datei bei jedem Laden neu, weil sich Namen und Pfade
       mit Updates ändern. Weitere Verzeichnisse lassen sich von Hand ergänzen,
       etwa die fest eingestellten Pfade der klassischen Entwicklungs-VM, die das
       bestehende Tool als einzige kennt.],
      [Einträge und Ablage auf der Testumgebung geprüft. Beim Wechsel der aktiven
       Konfiguration änderte sich keine Datei. Die aktive Konfiguration
       automatisch zu erkennen bleibt vorgemerkt.],

      [Kompilierte Labels],
      [Labels ohne Label-Datei liest das bestehende Tool aus kompilierten
       Ressourcen, indem es die Assembly lädt. Im Prozess von Visual Studio bliebe
       die Datei bis zum Schliessen geladen @ms-assembly-unload, und ein erneutes
       Laden lieferte die alte Fassung @ms-assembly-loadfrom. Die Kernlogik liest
       die Ressourcen deshalb direkt aus der Datei, ohne die Assembly zu laden.],
      [Es gibt Models, die nur kompilierte Ressourcen haben. Zu prüfen in
       AP3.2.],

      [Laden],
      [Im Hintergrund, sobald das Tool Window oder die erste X++-Datei geöffnet
       wird. Bis dahin zeigt der Tooltip keinen Eintrag und das Tool Window einen
       Hinweis.],
      [NFA02],

      [Schreiben],
      [Im selben Format wie das bestehende Tool, also UTF-8 und leere
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
       Der Span beginnt mit dem Anführungszeichen. Beide Formen der Label-ID
       werden verarbeitet, mit und ohne Doppelpunkt. Die Namen der
       Classifications stehen an einer Stelle, siehe R07.],
      [Neue Form belegt, alte Form nur im Properties Window beobachtet.],

      [Entwicklung ohne X++-Editor],
      [Auf dem privaten Gerät gibt es keinen X++-Editor. Im Debug-Build hängen sich
       Tooltip und Inline-Anzeige zusätzlich an Textdateien mit der Endung `.xpp`
       an und erkennen Labels über eine Regex. Dieselbe Regex ist der Fallback
       aus R07.],
      [Senkt die Zahl der Durchgänge auf der Testumgebung, siehe R04.],

      [Tool Window],
      [Remote UI des neuen Modells. Fehlt dort ein benötigtes Control, folgt
       ein klassisches Tool Window mit WPF, das im selben Prozess ebenfalls möglich
       ist.],
      [Im Spike nicht geprüft. Zu prüfen in AP3.1.],

      [Tooltip],
      [Eigene QuickInfo-Quelle über MEF. Ihr Eintrag erscheint im selben Tooltip
       wie der Eintrag der Developer Tools.],
      [Belegt, siehe @machbarkeitsbeurteilung.],

      [Inline-Anzeige],
      [Platz oberhalb der Zeile über dieselbe Technik, mit der die Developer Tools
       ihre Referenzanzeige zeichnen.],
      [Nicht erprobt, siehe R08. Z2 ist über FA05 auch ohne sie erreicht.],

      [Extraktion im Editor],
      [Ersetzt das markierte String Literal im Text Buffer des Editors. Rückgängig machen
       und Speichern bleiben bei Visual Studio und den Developer Tools.],
      [Standardschnittstelle des Editors, nicht eigens erprobt.],

      [Extraktion im Properties Window],
      [Lesen über das Selection Tracking. Geschrieben wird direkt in die
       XML-Datei des Elements, wie in @machbarkeitsbeurteilung festgelegt. Ein
       geöffneter Designer meldet die Änderung danach und bietet an, das Element
       neu zu laden. Das Element muss vorher gespeichert sein, siehe die nächste
       Zeile.],
      [Lesen belegt. Verhalten des Designers auf der Testumgebung
       beobachtet.],

      [Schreiben in Elemente],
      [Die Extraktion im Designer, das Umstellen der Referenzen beim Kopieren und
       Verschieben und das Ersetzen schreiben in die XML-Dateien von Elementen.
       Hat ein betroffenes Element in Visual Studio ungespeicherte Änderungen,
       schreibt die Extension nicht in dieses Element, nennt es im Output Window
       und bietet an, zuerst zu speichern. Fallback ist, vor jedem Schreiben
       anzubieten, alle Dokumente über den Command `File.SaveAll` zu speichern
       @ms-vs-shortcuts. In `.xpp`-Dateien schreibt die Extension nie, siehe die
       nächste Zeile.],
      [Auf der Testumgebung beobachtet, dass ein Element vor dem Ändern der
       XML-Datei gespeichert sein muss. Ob die Extension ungespeicherte
       Änderungen erkennt und wie ein geöffneter X++-Editor auf eine geänderte
       XML-Datei reagiert, prüft D2.],

      [Sprung an die Fundstelle],
      [Die Verwendungssuche findet die Label-ID in der XML-Datei des Elements.
       Der X++-Editor zeigt dagegen eine `.xpp`-Datei im Ordner XppSource, siehe
       @befundprotokoll. Die Developer Tools erzeugen sie erst beim Öffnen des
       Elements, eine Datei je Element, für Klassen wie für Tables. Ihre Zeilen
       entsprechen dem Editor, die Methoden folgen, soweit geprüft, der
       Reihenfolge im XML. XML und `.xpp` ändern sich erst beim Speichern. Eine
       Änderung direkt an der `.xpp`-Datei zeigt der Editor zwar an, beim
       nächsten Öffnen des Elements ist sie aber verworfen. Die Verwendungssuche
       sieht deshalb nur gespeicherte Stände.

       Ein Klick auf das Symbol einer Fundstelle öffnet das Element dort, wo die
       Label-ID steht. Steht sie im Code, öffnet die Extension das Element im
       X++-Editor und setzt den Cursor auf dasselbe Vorkommen der Label-ID, also
       auf das dritte, wenn die Fundstelle das dritte Vorkommen im Code des XML
       ist. Steht sie in einer Eigenschaft, öffnet die Extension das Element im
       Designer, wählt den Knoten mit der Eigenschaft, etwa ein Feld einer Table,
       und markiert die Eigenschaft im Properties Window. Gelingt das nicht, ist
       das Element trotzdem geöffnet, nur ohne Sprung an die Fundstelle.],
      [Ablage und Speichern auf der Testumgebung geprüft. Wie die Extension ein
       Element öffnet und darin einen Knoten und eine Eigenschaft wählt, ist
       nicht untersucht und wird in D2 geprüft.],

      [API-Schlüssel],
      [Verschlüsselt über die Data Protection API von Windows im Benutzerprofil,
       nie im Repository.],
      [NFA06],

      [Error Boundary],
      [Jeder Einstiegspunkt, also Command, MEF-Teil und Background Task, fängt
       Fehler ab und meldet sie im Output Window.],
      [NFA04, Nachweis im Testkonzept.],
    ),
    caption: [Technische Festlegungen (eigene Darstellung)]
  ) <festlegungen>
]

== Modellierung der Klassen

=== Klassendiagramm

Das Klassenmodell zeigt die Kernlogik. Die Extension setzt die Komponenten aus
@komponenten mit Klassen um, die Visual Studio anbinden und die Kernlogik
aufrufen. Eigene Fachlogik enthalten sie nicht, damit sich alles, was geprüft
werden muss, ohne Visual Studio testen lässt. Die Namen sind die späteren Namen im
Code und deshalb englisch.

@klassen_laden folgt für die Fachklassen dem Zusammenhang aus @labelstruktur.

#figure(
  image("../diagrams/Klassen_Laden.png", width: 100%),
  caption: [Klassen der Kernlogik, Fachklassen, Laden und Speichern (eigene
            Darstellung)]
) <klassen_laden>

Suche, Bearbeiten und Übersetzung in @klassen_bearbeiten greifen auf den Label
Store zu. Die Klassen aus @klassen_laden erscheinen dort ohne Attribute und
Methoden.

#figure(
  image("../diagrams/Klassen_Bearbeiten.png", width: 100%),
  caption: [Klassen der Kernlogik, Suche, Bearbeiten und Übersetzung (eigene
            Darstellung)]
) <klassen_bearbeiten>

=== Beschreibung der Klassen

#[
  #show figure: set align(left)
  #set text(size: 9.5pt)
  #figure(
    table(
      align: left,
      columns: (auto, 1fr, auto),
      table.header(
        [*Klasse*], [*Beschreibung*], [*Wichtige Methoden*],
      ),
      [`LabelId`],
      [Label-ID in einer der beiden Formen aus @festlegungen. Trennt eine ID in
       Label-Datei und Label. `FindAll` findet Label-IDs in einer Textzeile, für
       den Ersatz des X++-Editors im Debug-Build und als Fallback aus R07.],
      [`TryParse`\ `Parse`\ `FindAll`],

      [`Label`\ `Translation`],
      [Ein Label mit Text und Kommentar je Sprache. `IsModified` zeigt an, dass
       eine Änderung noch nicht in der Label-Datei steht.],
      [`GetText`],

      [`LabelFile`],
      [Steht für eine Label-Datei wie `BDM1` und kennt die Sprachen, in denen es
       sie gibt. Ändern lässt sie sich nur, wenn ihr Model beschreibbar ist und
       sie nicht nur kompiliert vorliegt.],
      [--],

      [`ModelInfo`\ `ModelDiscovery`],
      [Liest die Ordner der gewählten Metadaten-Konfiguration aus `XPPConfig`.
       Findet die Models über die Descriptor-Dateien dieser und der weiteren
       Package-Verzeichnisse und erkennt schreibgeschützte Models. Ordnet einer
       Datei ihr Model zu, für den Vorschlag der Label-Datei bei der
       Extraktion.],
      [`ReadConfiguration`\ `FindModels`\ `FindModelFor`],

      [`ILabelSource`],
      [Liefert die Label-Dateien eines Models. `LabelFileSource` liest die
       Label-Dateien, `CompiledLabelSource` die kompilierten Ressourcen, ohne die
       Assembly zu laden. Eine Quelle über die Metadata-API aus
       @variantenentscheid liesse sich ergänzen, ohne den Label Store zu
       ändern.],
      [`Load`],

      [`LabelFileFormat`],
      [Liest und schreibt das Format der Label-Dateien nach @festlegungen. Keine
       andere Klasse kennt das Format.],
      [`Read`\ `Write`],

      [`LabelStore`],
      [Hält alle geladenen Labels, nach Label-ID indiziert. `Find` liefert `null`
       für eine unbekannte ID. Das Laden baut einen neuen Index auf und ersetzt
       den alten erst am Ende. Suchen während des Ladens sehen so den bisherigen
       Stand, auch beim Neuladen nach FA15. Neue und geänderte Labels stehen
       sofort im Label Store und bleiben als geändert markiert, bis das Speichern
       gelingt. Ein Neuladen übernimmt sie in den neuen Index.],
      [`LoadAsync`\ `Find`],

      [`LabelFileWatcher`],
      [Meldet Änderungen an beschreibbaren Label-Dateien von aussen. Während
       eigener Schreibvorgänge ist er angehalten.],
      [`Suspend`],

      [`LabelSearch`],
      [Suche mit Ranking. `SearchMode` und `CaseSensitive` ergeben zusammen die
       acht Suchmodi aus @ist_funktionen.],
      [`Search`],

      [`LabelOperations`],
      [Anlegen, Ändern, Löschen, Kopieren und Verschieben. Trägt die Änderung in
       den Label Store ein, hält die Dateiüberwachung an und schreibt alle
       Sprachen der Label-Datei. Beim Kopieren und Verschieben stellt es auf
       Wunsch die Referenzen um.],
      [`Create`\ `Update`\ `Delete`\ `Copy`\ `Move`],

      [`LabelIdGenerator`],
      [Erzeugt neue Label-IDs nach @festlegungen.],
      [`NewId`],

      [`UsageSearch`\ `LabelUsage`],
      [Textsuche nach der vollständigen Label-ID in den XML-Dateien der Elemente.
       Eine Fundstelle zählt nur, wenn kein weiteres Zeichen der ID folgt, damit
       die Suche nach `@SYS1234` nicht `@SYS12345` meldet. Referenzen ersetzt sie
       nur in beschreibbaren Models.],
      [`FindUsages`\ `ReplaceReferences`],

      [`ITranslationService`\ `DeepLTranslationService`],
      [Übersetzt einen Text in mehrere Zielsprachen. Die Umsetzung für DeepL
       schickt je Zielsprache eine Anfrage, weil DeepL pro Anfrage nur eine
       Zielsprache annimmt @deepl-translate. Sie bildet die Sprachcodes von
       Dynamics 365 auf die von DeepL ab. `de` wird zu `DE`, `de-CH` zu `DE-CH`
       @deepl-languages. Die Unterscheidung zählt, weil die Schweizer Variante
       kein scharfes S kennt und "Strasse" statt "Straße" schreibt
       @deepl-swiss-german. Weitere Dienste wie in KA10 kommen als eigene
       Umsetzung dazu.],
      [`TranslateAsync`],

      [`LabelSettings`],
      [Package-Verzeichnisse, zu ladende und anzulegende Sprachen, Quellsprache.
       Gespeichert werden sie von der Extension, die auch den API-Schlüssel
       verwaltet.],
      [--],

      [`IMessageSink`],
      [Meldungen der Kernlogik mit ihrer Art. Die Extension leitet sie ins Output
       Window.],
      [`Report`],
    ),
    caption: [Klassen der Kernlogik (eigene Darstellung)]
  ) <klassen>
]

=== Design Patterns

#[
  #show figure: set align(left)
  #set text(size: 9.5pt)
  #figure(
    table(
      align: left,
      columns: (auto, auto, 1fr),
      table.header(
        [*Pattern*], [*Verwendung*], [*Begründung*],
      ),
      [Try-Parse Pattern],
      [`LabelId`\ `.TryParse`],
      [Ob ein Suchbegriff oder ein Eigenschaftswert im Designer eine Label-ID ist,
       zeigt sich erst beim Parsen. Ein Fehlschlag ist dort der Normalfall und
       kein Fehler. `TryParse` meldet ihn mit `false` statt mit einer Exception,
       die um Grössenordnungen langsamer sein kann, und liefert die ID im
       `out`-Parameter. Microsoft empfiehlt das Pattern für solche Fälle, zusammen
       mit einer Methode, die eine Exception wirft @ms-try-parse. Das ist `Parse`,
       etwa für die IDs beim Lesen einer Label-Datei. Der Label Store braucht das
       Pattern nicht, weil ein fehlendes Label als `null` eindeutig ist.],

      [Strategy],
      [`ILabelSource`\ `ITranslationService`],
      [Eine neue Quelle oder ein neuer Übersetzungsdienst kommt als eigene Klasse
       dazu, ohne dass sich Label Store oder Tool Window ändern (NFA03). Unit Tests
       setzen eine Umsetzung mit festen Daten ein und kommen ohne Dateisystem und
       ohne Internet aus.],

      [Dependency Inversion],
      [`IMessageSink`],
      [Die Kernlogik legt das Interface für Meldungen fest, die Extension setzt es
       mit dem Output Window um. So braucht die Kernlogik keine Referenz auf
       Visual Studio (Z8).],

      [Task-based Asynchronous Pattern],
      [`LoadAsync`\ `TranslateAsync`],
      [Laden und Übersetzen dauern und dürfen Visual Studio nicht blockieren
       (NFA02). Microsoft empfiehlt das Pattern für neue Entwicklung @ms-tap. Der
       `CancellationToken` bricht das Laden ab, wenn Visual Studio schliesst oder
       ein neues Laden beginnt.],

      [Observer über Events],
      [`Changed`\ `ExternalChange`],
      [Tool Window und Inline-Anzeige erfahren von neuen Labels, ohne dass die
       Kernlogik sie kennt.],

      [`using`-Block mit `IDisposable`],
      [`LabelFileWatcher`\ `.Suspend`],
      [Innerhalb des Blocks ruht die Dateiüberwachung. Am Ende läuft sie wieder,
       auch wenn das Schreiben mit einer Exception abbricht @ms-using. Eigene
       Schreibvorgänge lösen so keine Meldung aus (FA15), und ein Fehler legt die
       Überwachung nicht dauerhaft still.],
    ),
    caption: [Design Patterns der Kernlogik (eigene Darstellung)]
  ) <design_patterns>
]

== Sequenzdiagramme

Die drei Sequenzen zeigen die Abläufe, in denen die Extension mit Visual Studio,
den Developer Tools oder einem externen Dienst zusammenspielt. Die übrigen
Abläufe bleiben im Tool Window und in der Kernlogik.

=== Laden

Das Laden beginnt, sobald das Tool Window oder die erste X++-Datei geöffnet wird,
siehe @festlegungen. Visual Studio wartet nicht darauf. Eine beschädigte oder
gesperrte Datei erzeugt eine Warnung, geladen werden die übrigen.

#figure(
  image("../diagrams/Sequenz_Laden.png", width: 90%),
  caption: [Sequenz beim Laden der Labels (eigene Darstellung)]
) <sequenz_laden>

=== Tooltip

Die Label-Erkennung wählt den Span mit der Classification eines Labels. Dieser
Span beginnt mit dem Anführungszeichen, siehe @befundprotokoll. Die
Label-Erkennung entfernt es vor dem Parsen. Die Inline-Anzeige verwendet dieselbe Erkennung und denselben
Zugriff auf den Label Store.

#figure(
  image("../diagrams/Sequenz_Tooltip.png", width: 95%),
  caption: [Sequenz beim Überfahren einer Label-ID (eigene Darstellung)]
) <sequenz_tooltip>

=== Extraktion

Gezeigt ist die Extraktion im X++-Editor. Die Classification `"X++ String"` für
String Literals hat die Machbarkeitsstudie beobachtet, siehe @befundprotokoll. Im
Designer tritt das Selection Tracking an die Stelle der Label-Erkennung, und
geschrieben wird in die XML-Datei des Elements statt in den Text Buffer.

#figure(
  image("../diagrams/Sequenz_Extraktion.png", width: 100%),
  caption: [Sequenz beim Extrahieren eines hardcodierten Textes im X++-Editor
            (eigene Darstellung)]
) <sequenz_extraktion>

== GUI-Design <gui_design>

Die Oberfläche übernimmt Aufbau und Begriffe des bestehenden Tools, damit sich
Entwickler, die es kennen, ohne Einarbeitung zurechtfinden. Beschriftet ist sie wie
das bestehende Tool auf Englisch.

=== Tool Window

Das Tool Window soll auch schmal am Rand angedockt benutzbar sein. Suche,
Trefferliste, Detailansicht und Verwendungen stehen deshalb untereinander und nicht
nebeneinander wie in @ist_screenshot. Die Trefferliste hat eine Spalte je geladene Sprache.

#figure(
  image("../diagrams/GUI_ToolWindow.png", width: 12cm),
  caption: [Wireframe des Tool Window mit Demo-Daten (eigene Darstellung)]
) <gui_toolwindow>

#[
  #show figure: set align(left)
  #set text(size: 10pt)
  #figure(
    table(
      align: left,
      columns: (1fr, 1.4fr),
      table.header(
        [*Bestehendes Tool*], [*Extension*],
      ),
      [Apply, Save und Save and apply im Ribbon],
      [Insert, Save und Save and insert in der Toolbar. Insert fügt die Label-ID
       an der Cursorposition ein, statt sie in die Zwischenablage zu kopieren
       (FA14).],
      [Ein Knopf je beschreibbares Model, siehe @ist_erstellen],
      [Auswahl der Label-Datei und ein Knopf New. Eine Reihe von Knöpfen passt nicht
       in ein schmales Tool Window.],
      [Verwendungssuche in einer eigenen Ansicht, siehe @ist_verwendungssuche],
      [Bereich References unter der Detailansicht. Ein Klick auf das Symbol einer
       Fundstelle öffnet das Element an dieser Stelle (FA04).],
      [Console],
      [Eigener Bereich BE-LabelExtension im Output Window (FA17).],
      [Settings],
      [Seite in den Optionen von Visual Studio, siehe @gui_einstellungen. Der Knopf
       Settings öffnet sie.],
      [Tracing (Preview)],
      [Entfällt, siehe @abgrenzung.],
    ),
    caption: [Bedienelemente des bestehenden Tools in der Extension (eigene
              Darstellung)]
  ) <gui_zuordnung>
]

Ein neues Label erscheint in der Detailansicht ohne ID, bis der Entwickler
bestätigt. Vorschläge des Übersetzungsdienstes sind markiert, bis er sie ändert oder
bestätigt (FA10). Nicht gespeicherte Labels sind in der Trefferliste markiert.

=== Editor und Designer

Im X++-Editor erscheinen die Übersetzungen im Tooltip und oberhalb der Zeile wie in
@demo_quickinfo und @demo_inline. Eine fehlende Übersetzung ist als `missing`
markiert. Das Kontextmenü des Editors erhält drei Commands, Search label (FA07),
Open in label window (FA08) und Extract to label (FA09). Im Designer wirkt der Knopf
Extract im Tool Window auf das gewählte Element. Ob sich der Command auch ins
Kontextmenü des Designers einhängen lässt, ist nicht untersucht.

=== Einstellungen

In den Optionen von Visual Studio erhält die Extension eine eigene Seite
BE-LabelExtension mit den Einstellungen aus FA11. Der API-Schlüssel erscheint dort
verdeckt und wird nach NFA06 verschlüsselt abgelegt.

#figure(
  image("../diagrams/GUI_Einstellungen.png", width: 12cm),
  caption: [Wireframe der Einstellungen (eigene Darstellung)]
) <gui_einstellungen>

=== Shortcuts

Die Shortcuts des bestehenden Tools lassen sich nicht übernehmen, weil Visual Studio
alle vier bereits belegt. Strg+S und Strg+Shift+S speichern Dateien, Strg+Shift+A
fügt einem Projekt ein Element hinzu, und Strg+N legt eine neue Datei an
@ms-vs-shortcuts. Die Extension erhält deshalb neue Shortcuts, die in AP3.5 gewählt
werden. Jeder Entwickler kann sie in den Optionen von Visual Studio umbelegen, die
dort auch anzeigen, ob ein Shortcut schon vergeben ist @ms-vs-customize-shortcuts.

== Testkonzept <testkonzept>

=== Vorgehen

Getestet wird auf drei Stufen.

- Unit Tests prüfen die Kernlogik ohne Visual Studio und ohne Dynamics 365. Sie
  laufen bei jedem Build unter .NET 8 und unter .NET Framework 4.8.
- Systemtests prüfen die Extension in Visual Studio. Sie laufen zuerst auf dem
  privaten Gerät mit dem Debug-Build, die Testfälle zum X++-Editor dort über den
  Ersatz aus @festlegungen. Auf der Testumgebung laufen sie in den Durchgängen aus
  @durchgaenge, in D4 vollständig.
- Reviews prüfen, was sich nicht ausführen lässt, etwa die Abhängigkeiten der
  Kernlogik.

Die Unit Tests arbeiten mit synthetischen Testdaten, die in AP3.2 entstehen und
keine Daten des Arbeitgebers enthalten (OA03). Sie decken beide Label-Formen,
schreibgeschützte Models, kompilierte Ressourcen und beschädigte Label-Dateien ab.
Für FA12 werden die Treffer des bestehenden Tools auf denselben Testdaten einmal
erfasst und als erwartete Ergebnisse abgelegt.

Testfälle, die auf der Testumgebung schreiben, verwenden dort ein bestehendes
Model.

Ein Testfall ist bestanden, wenn alle erwarteten Ergebnisse eintreten. Fehler, die
eine Anforderung verletzen, werden bis AP4.1 behoben. Unter Visual Studio 2022 hält
das Testprotokoll die Ergebnisse nur fest, Fehler dort haben nach NFA01 tiefe
Priorität. Die Ergebnisse stehen im Testprotokoll, siehe @testprotokoll.

=== Testobjekte

#[
  #show figure: set align(left)
  #show figure: set block(breakable: false)
  #set text(size: 10pt)
  #figure(
    table(
      align: left,
      columns: (auto, 1fr, 1fr),
      table.header(
        [*ID*], [*Testobjekt*], [*Teststufe*],
      ),
      [TO1], [Kernlogik], [Unit Tests bei jedem Build],
      [TO2], [Extension im Debug-Build auf dem privaten Gerät], [Systemtest],
      [TO3], [VSIX-Paket unter Visual Studio 2026 auf der Testumgebung],
      [Systemtest in D1 bis D4],
      [TO4], [VSIX-Paket unter Visual Studio 2022 auf der Testumgebung],
      [Systemtest in D4],
      [TO5], [Quellcode und Repository], [Review],
    ),
    caption: [Testobjekte (eigene Darstellung)]
  ) <testobjekte>
]

=== Testfälle

Die Systemtests auf dem privaten Gerät laufen in D4 auf der Testumgebung ein
zweites Mal. OA04 folgt erst nach Projektabschluss und hat keinen Testfall.

#[
  #show figure: set align(left)
  #set text(size: 9pt)
  #figure(
    table(
      align: left,
      columns: (auto, 1fr, 1.2fr, auto),
      table.header(
        [*ID*], [*Testfall*], [*Erwartetes Ergebnis*], [*Referenz*],
      ),
      table.cell(colspan: 4)[*Unit Tests der Kernlogik, TO1*],
      [TC01],
      [Label-Datei mit Kommentaren, leeren Texten und beiden Label-Formen lesen und
       wieder schreiben.],
      [Die geschriebene Datei ist Byte für Byte gleich wie die gelesene, in
       UTF-8.],
      [FA03],

      [TC02],
      [Label-IDs beider Formen parsen und in einer Textzeile finden, dazu
       ungültige Eingaben.],
      [`@BDM1:L3F2A9C15B8047DE1` und `@SYS12345` werden erkannt und getrennt.
       Ungültige Eingaben liefern `false` ohne Exception. `FindAll` findet alle IDs
       einer Zeile mit Position.],
      [FA01\ FA05],

      [TC03],
      [10'000 neue Label-IDs erzeugen.],
      [Jede ID besteht aus `L` und 16 hexadezimalen Ziffern in Grossbuchstaben,
       keine kommt doppelt vor.],
      [FA02],

      [TC04],
      [Jeden der acht Suchmodi mit festen Suchbegriffen auf die Testdaten
       anwenden.],
      [Treffer und Reihenfolge entsprechen den erfassten Ergebnissen des
       bestehenden Tools. Jedes Label erscheint einmal.],
      [FA01\ FA12],

      [TC05],
      [Verwendungen einer Label-ID suchen und durch eine andere ersetzen.],
      [Alle Fundstellen mit Model, Datei, Zeile und Spalte. Die Suche nach
       `@SYS1234` meldet `@SYS12345` nicht. Ersetzt wird nur in beschreibbaren
       Models, die Zahl geänderter Dateien stimmt.],
      [FA04\ FA13],

      [TC06],
      [Label in eine andere Label-Datei kopieren und verschieben, mit Umstellen der
       Referenzen.],
      [Das neue Label hat eine ID in der neuen Form und alle Sprachen der
       Ziel-Datei. Referenzen in beschreibbaren Models zeigen auf die neue ID.
       Verschieben löscht das Original.],
      [FA03],

      [TC07],
      [Labels aus einem schreibgeschützten Model und aus kompilierten Ressourcen
       ändern, löschen und kopieren.],
      [Ändern und Löschen werden abgelehnt, Kopieren gelingt. Schreibgeschützte
       Label-Dateien stehen beim Anlegen nicht zur Wahl.],
      [FA02\ FA03],

      [TC08],
      [Labels aus einer Ressourcen-Assembly der Testdaten lesen.],
      [Alle Labels sind gelesen. Die Assembly ist danach nicht geladen und die
       Datei lässt sich ersetzen.],
      [@festlegungen],

      [TC09],
      [Ein Label ungespeichert ändern, die Label-Datei von aussen ändern und neu
       laden, danach selbst speichern.],
      [Die Änderung von aussen löst eine Meldung aus, das eigene Speichern nicht.
       Das ungespeicherte Label ist nach dem Neuladen noch da.],
      [FA15],

      [TC10],
      [Laden mit einer beschädigten Label-Datei, einem fehlenden
       Package-Verzeichnis und einer gesperrten Datei.],
      [Je eine Meldung mit Datei oder Pfad, die übrigen Labels sind geladen. Keine
       Exception verlässt die Kernlogik.],
      [NFA04],

      [TC11],
      [Mit einem simulierten Übersetzungsdienst übersetzen, dazu eine
       Fehlerantwort.],
      [Eine Anfrage je Zielsprache, `de-CH` geht als `DE-CH` hinaus. Die
       Fehlerantwort ergibt einen Fehler mit Grund statt Vorschlägen.],
      [FA10],

      table.cell(colspan: 4)[*Systemtests auf dem privaten Gerät, TO2*],
      [TC12],
      [VSIX-Paket per Doppelklick installieren, Visual Studio starten und das Tool
       Window öffnen.],
      [Die Installation braucht keine weiteren Schritte. Das Tool Window öffnet
       sich, das Laden läuft im Hintergrund.],
      [OA01\ NFA02],

      [TC13],
      [Im Tool Window in jedem Suchmodus suchen und einen Treffer wählen.],
      [Jedes Label erscheint einmal. Die Detailansicht zeigt alle geladenen
       Sprachen.],
      [FA01\ FA12],

      [TC14],
      [Text und Kommentar in zwei Sprachen ändern und speichern, danach ein Label
       löschen.],
      [Die Label-Dateien enthalten die Änderungen. Das gelöschte Label fehlt in
       allen Sprachen.],
      [FA03],

      [TC15],
      [Nach einer Suche ohne Treffer mit dem Shortcut ein neues Label anlegen.],
      [Alle anzulegenden Sprachen sind mit dem Suchbegriff vorbelegt. Nach dem
       Bestätigen steht das Label mit einer ID nach @festlegungen in allen
       Dateien.],
      [FA02\ FA16],

      [TC16],
      [Label-ID einfügen mit und ohne aktiven Editor, danach Save and insert.],
      [Die ID steht an der Cursorposition. Ohne Editor erscheint ein Hinweis. Save
       and insert speichert vorher.],
      [FA14],

      [TC17],
      [Alle Shortcuts auslösen und einen davon in den Optionen umbelegen.],
      [Jede Funktion reagiert. Die Optionen zeigen keine Doppelbelegung, der neue
       Shortcut wirkt.],
      [FA16],

      [TC18],
      [Sprachen und Metadaten-Konfiguration ändern, neu laden, dann Visual Studio
       neu starten.],
      [Die neuen Sprachen wirken nach dem Laden ohne Neustart. Nach dem Neustart
       sind alle Einstellungen erhalten.],
      [FA11],

      [TC19],
      [Einen Fehler auslösen, etwa mit einem ungültigen Package-Verzeichnis.],
      [Die Meldung steht mit Art und Zeitpunkt im eigenen Bereich des Output
       Window. Visual Studio läuft weiter.],
      [FA17\ NFA04],

      [TC20],
      [API-Schlüssel hinterlegen, dann Einstellungsdateien, Output Window und
       Repository nach ihm durchsuchen.],
      [Der Schlüssel erscheint nirgends im Klartext.],
      [NFA06],

      table.cell(colspan: 4)[*Systemtests auf der Testumgebung, TO3 und TO4*],
      [TC21],
      [Extension neben den Developer Tools laden und eine X++-Klasse öffnen, in D1.],
      [Beide laufen ohne Fehlermeldung. Die Developer Tools arbeiten wie ohne
       Extension.],
      [NFA04\ R06],

      [TC22],
      [Label-IDs beider Formen im X++-Editor überfahren, darunter eine unbekannte
       und eine mit fehlender Übersetzung, in D2.],
      [Der Tooltip zeigt alle konfigurierten Sprachen neben dem Eintrag der
       Developer Tools. Die fehlende Übersetzung ist markiert, die unbekannte ID
       gemeldet.],
      [FA05],

      [TC23],
      [Datei mit mehreren Label-IDs öffnen, die Inline-Anzeige aus- und
       einschalten, Visual Studio neu starten, in D2.],
      [Oberhalb jeder Zeile mit einer Label-ID stehen die Übersetzungen. Die
       Einstellung bleibt nach dem Neustart erhalten.],
      [FA06],

      [TC24],
      [Suche aus dem Editor, dann auf einer Label-ID das
       Label im Tool Window öffnen, in D2.],
      [Gesucht wird mit dem markierten Text oder dem Inhalt des String Literals.
       Das Label öffnet sich in der Detailansicht und lässt sich bearbeiten.],
      [FA07\ FA08],

      [TC25],
      [Verwendungen eines Labels suchen und eine Fundstelle anklicken, in D2.],
      [Alle Fundstellen mit Model, Datei, Zeile und Spalte. Ein Klick auf das
       Symbol einer Fundstelle im Code öffnet das Element im X++-Editor mit dem
       Cursor auf der Label-ID. Bei einer Fundstelle in einer Eigenschaft öffnet
       sich das Element im Designer mit gewähltem Knoten und markierter
       Eigenschaft, mindestens aber das Element.],
      [FA04],

      [TC26],
      [Label mit Umstellen der Referenzen verschieben, während ein betroffenes
       Element mit ungespeicherten Änderungen im Designer offen ist. Danach
       dasselbe mit einem gespeicherten Element, das im X++-Editor offen ist, in
       D2.],
      [Die Extension schreibt nicht in das ungespeicherte Element, nennt es im
       Output Window und bietet an, zuerst zu speichern. Nach dem Speichern
       gelingt das Umstellen, und der Designer bietet das Neuladen an. Die
       umgestellte Referenz geht auch beim nächsten Speichern im X++-Editor
       nicht verloren.],
      [FA03\ NFA04],

      [TC27],
      [String Literal im X++-Editor extrahieren, danach rückgängig machen, in D3.],
      [Das Literal enthält die neue Label-ID, das Label steht in der Label-Datei.
       Rückgängig stellt den Text wieder her.],
      [FA09],

      [TC28],
      [Im Designer ein Element mit hardcodiertem Label und Help Text wählen und
       extrahieren, in D3.],
      [Beide Eigenschaften stehen zur Wahl. Die gewählte enthält danach die
       Label-ID. Der Designer meldet die Änderung und lädt das Element nach
       Bestätigung neu.],
      [FA09],

      [TC29],
      [Label mit Übersetzungsdienst anlegen, mit gültigem und ohne API-Schlüssel,
       in D3.],
      [Vorschläge für alle anzulegenden Sprachen, vor dem Speichern änderbar. Ohne
       Schlüssel der Ausgangstext in allen Sprachen und eine Meldung.],
      [FA10],

      [TC30],
      [Nach Anlegen, Ändern und Verschieben von Labels das verwendete Model
       kompilieren, in D3.],
      [Der Build läuft ohne Fehler.],
      [FA02\ FA03],

      [TC31],
      [Zehn Suchen über alle Label-Dateien der Testumgebung messen und das Laden
       beobachten, in D4.],
      [Jede Suche dauert höchstens 500 ms. Visual Studio meldet während des Ladens
       keine blockierte Oberfläche.],
      [NFA02],

      [TC32],
      [Alle Systemtests unter Visual Studio 2022 wiederholen, in D4.],
      [Das Testprotokoll hält das Ergebnis jedes Testfalls fest.],
      [NFA01],

      table.cell(colspan: 4)[*Reviews, TO5*],
      [TC33],
      [Referenzen der Kernlogik prüfen und probeweise einen zweiten
       Übersetzungsdienst ergänzen.],
      [Keine Referenz auf Visual Studio. Der zweite Dienst braucht keine Änderung
       an bestehenden Klassen.],
      [NFA03\ Z8],

      [TC34],
      [Build der Solution und Dokumentation der öffentlichen Klassen prüfen.],
      [Keine Warnungen. Alle öffentlichen Klassen und Methoden sind dokumentiert.],
      [NFA05],

      [TC35],
      [Repository prüfen.],
      [Aufbau, Build und Erweiterungspunkte sind beschrieben. Kein Code der
       Standardanwendung oder des bestehenden Tools, die Testdaten sind
       synthetisch.],
      [OA02\ OA03],
    ),
    caption: [Testfälle (eigene Darstellung)]
  ) <testfaelle>
]

== Einführung und Betrieb

Eine Datenmigration entfällt. Die Extension liest und schreibt dieselben
Label-Dateien im selben Format wie das bestehende Tool, siehe @festlegungen. Die
Einstellungen des bestehenden Tools werden nicht übernommen, weil es nur zwei Listen
von Sprachen sind.

Eingeführt wird in drei Schritten.

+ Das VSIX-Paket wird per Doppelklick installiert (OA01).
+ Beim ersten Start trägt der Entwickler seine Sprachen ein und prüft das
  vorbelegte Package-Verzeichnis. Für Übersetzungsvorschläge hinterlegt er einen
  API-Schlüssel.
+ Wo beide Tools dieselben Package-Verzeichnisse verwenden, lassen sie sich
  nebeneinander betreiben. Ändert eines eine Label-Datei, bietet das andere das
  Neuladen an, siehe FA15 und @ist_funktionen.

Ob und wann das bestehende Tool abgelöst wird, entscheidet BE-terna nach
Projektabschluss (OA04). Ich empfehle eine Pilotphase mit einzelnen Entwicklern,
deren Rückmeldungen vor der Ablösung einfliessen. Das wirkt zugleich R09 entgegen.

Im Betrieb braucht die Extension weder Server noch Datenbank. Eine neue Version
kommt als neues VSIX-Paket, das die alte ersetzt. Wie das Paket verteilt wird und
woher die API-Schlüssel im Betrieb stammen, legt BE-terna fest. Für Entwicklung und
Tests während des Projekts stelle ich einen eigenen Schlüssel. Die
Weiterentwicklung liegt beim Maintainer, siehe @stakeholderanalyse. Nach einem Uninstall bleiben die Label-Dateien unverändert.

#todo[Mit BE-terna klären, unter welcher Lizenz der Code im öffentlichen Repository
steht und dass BE-terna die Extension einsetzen und weiterentwickeln darf. Danach
eine Lizenzdatei ins Repository legen und die Lizenz hier nennen.]

== Realisierungsplan <realisierungsplan>

Die Realisierung folgt den Stufen aus @projektziele. Die Kernlogik kommt vor der
Oberfläche, weil Tool Window, Tooltip und Extraktion auf denselben Label Store
zugreifen. Jeder Tag endet mit einem Ergebnis, das sich prüfen lässt. Gegenüber
@terminplan_soll wandert die Suche aus dem Editor von AP3.6 zu AP3.7, damit AP3.6 ganz
der Anzeige im Editor gehört.

#[
  #show figure: set align(left)
  #set text(size: 9.5pt)
  #figure(
    table(
      align: left,
      columns: (auto, 1.5fr, auto, 1.5fr),
      table.header(
        [*AP*], [*Inhalt*], [*Anforde-\ rungen*], [*Ergebnis*],
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
       Label Store, Dateiüberwachung, synthetische Testdaten.],
      [FA15, NFA02],
      [Unit Tests lesen und schreiben Label-Dateien ohne Verlust von Kommentaren
       und Kodierung. Das Laden läuft im Hintergrund.],
      [3.3\ 15.10.],
      [Alle Suchmodi mit Ranking. Treffer des bestehenden Tools auf dem
       Testdaten als erwartete Ergebnisse erfassen.],
      [FA01, FA12, NFA02],
      [TC04 besteht für alle acht Suchmodi. Eine Suche über die Testdaten
       dauert wenige hundert Millisekunden.],
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
       das Symbol einer Fundstelle öffnet das Element an der Fundstelle.],

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
      [D1], [AP3.1],
      [Laden neben den Developer Tools (TC21).],
      [D2], [AP3.7],
      [Label-Erkennung, Tooltip, Inline-Anzeige, Suche aus dem Editor, Öffnen im
       Panel, Verwendungssuche mit Öffnen im X++-Editor, Schreiben in geöffnete
       Elemente (TC22 bis TC26).],
      [D3], [AP3.8],
      [Extraktion im Editor und im Properties Window, Übersetzungsdienst,
       Kompilieren nach Änderungen (TC27 bis TC30).],
      [D4], [AP4.2],
      [Alle Systemtests aus @testfaelle, zusätzlich unter Visual Studio 2022
       (TC32).],
    ),
    caption: [Durchgänge auf der Testumgebung (eigene Darstellung)]
  ) <durchgaenge>
]

Reicht die Zeit an einem Tag nicht, wandert der Rest in AP4.1. Das Schreiben aus
dem Properties Window setzt nach der Prüfung auf der Testumgebung nur voraus, dass
das Element gespeichert ist. Der Fallback über die Eigenschaft des gewählten
Elements bleibt vorgemerkt. Die Property Descriptors melden sie als beschreibbar,
erprobt ist das nicht.
