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
      [*Testfälle*], [#todo[Testfälle aus dem Testkonzept eintragen.]],
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
      [*Testfälle*], [#todo[Testfälle aus dem Testkonzept eintragen.]],
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
      [*Testfälle*], [#todo[Testfälle aus dem Testkonzept eintragen.]],
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
               Label-ID in die XML-Datei des Elements, siehe @festlegungen.],
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
      [*Testfälle*], [#todo[Testfälle aus dem Testkonzept eintragen.]],
      [*Stufe*], [3],
    ),
    caption: [Use Case UC08 Hardcodierten Text extrahieren (eigene Darstellung)]
  ) <uc_08>
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
      [Ersetzt das markierte String Literal im Text Buffer des Editors. Rückgängig machen
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
       X++-Editor an die entsprechende Stelle. Fallback ist das Öffnen der
       XML-Datei an der Zeile.],
      [Zu prüfen, siehe unten.],

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
      [Findet die Models über die Descriptor-Dateien der Package-Verzeichnisse
       und erkennt schreibgeschützte Models. Ordnet einer Datei ihr Model zu, für
       den Vorschlag der Label-Datei bei der Extraktion.],
      [`FindModels`\ `FindModelFor`],

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
Fallback über die Eigenschaft des gewählten Elements vorzubereiten. Die
Property Descriptors melden sie als beschreibbar, erprobt ist das nicht.
