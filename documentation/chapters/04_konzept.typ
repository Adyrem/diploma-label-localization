#import "../helpers.typ": todo

// Nummerierte alternative Abläufe in den Use-Case-Beschreibungen
#let ablauf(..items) = grid(columns: (2em, 1fr), row-gutter: 0.6em, ..items)

= Konzept <konzept>

== Kontextdiagramm

Die Extension läuft in Visual Studio und hat vier Nachbarn. Von den Developer
Tools erfährt sie, was der X++-Editor als Label erkennt und welches Element im
Designer gewählt ist. In der Unified Developer Experience (UDE) liest sie aus deren
Metadaten-Konfiguration, wo die Package-Verzeichnisse liegen. Mit dem Package-Verzeichnis tauscht sie Label-Dateien und
die XML-Dateien der Elemente aus. Den Übersetzungsdienst ruft sie beim Anlegen
eines Labels auf. Über Visual Studio fügt sie Text in den Editor ein und schreibt
Meldungen ins Output Window.

#figure(
  image("../diagrams/Kontextdiagramm.png", width: 85%),
  caption: [Kontextdiagramm (eigene Darstellung)]
) <kontextdiagramm>

Ausserhalb der Systemgrenze liegen der Build von Dynamics 365, der die
Label-Dateien kompiliert, und der Best-Practice-Check, der hardcodierte Texte
meldet @ms-build-operations. Die Extension greift in keinen der beiden ein, muss ihre Dateien aber so
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

=== Funktionale Anforderungen

Wo die Akzeptanzkriterien nichts anderes festlegen, übernimmt die Extension das
Verhalten des bestehenden Tools.

#[
  #show figure: set align(left)
  #set text(size: 9.5pt)
  #figure(
    table(
      align: left,
      columns: (9.5em, 1fr, auto, auto),
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
       - Die ID besteht aus `L` und acht kryptografisch zufälligen Bytes als 16
         hexadezimale Ziffern in Grossbuchstaben, etwa `L3F2A9C15B8047DE1`,
         davor wie bisher die Label-Datei. Eine User-ID gibt es nicht mehr. Gibt
         es die ID in der Label-Datei schon, entsteht eine neue.
       - Alle anzulegenden Sprachen sind mit dem Suchbegriff vorbelegt.
       - Angelegt wird nur in Sprachen, für die es in der Label-Datei schon eine
         Datei gibt. Eine fehlende Sprache nennt das Output Window, eine neue
         Datei legt die Extension vorerst nicht an.
       - Nach dem Anlegen kompiliert Dynamics 365 die Label-Datei ohne Fehler.],
      [FA01], [1],

      [FA03\ Label-Bearbeitung],
      [- Text und Kommentar lassen sich je Sprache ändern und speichern.
       - Löschen entfernt das Label in allen Sprachen der Label-Datei.
       - Kopieren legt in einer anderen Label-Datei ein neues Label in allen
         Sprachen an, die Original und Ziel gemeinsam haben, mit Text und
         Kommentar des Originals. Verschieben löscht danach das Original.
       - Auf Wunsch werden die Referenzen in beschreibbaren Models auf die neue
         ID umgestellt, in ungespeicherten Elementen erst nach dem Speichern.
       - Labels aus schreibgeschützten Models oder kompilierten Ressourcen lassen
         sich kopieren, aber nicht ändern.],
      [FA01, FA04], [1],

      [FA04\ Verwendungssuche],
      [- Findet jedes Vorkommen der vollständigen Label-ID in den XML-Dateien der
         Package-Verzeichnisse.
       - Jede Fundstelle zeigt Model, Datei, Zeile und Spalte.
       - Ein Klick öffnet die Fundstelle in Visual Studio.],
      [--], [1, 2],

      [FA05\ Hover-Tooltip],
      [- Beim Überfahren einer Label-ID im X++-Editor erscheinen alle
         konfigurierten Sprachen mit ihrem Text.
       - Fehlende Übersetzungen sind als fehlend markiert.
       - Der Eintrag steht neben dem der Developer Tools.
       - Ist die ID unbekannt, sagt der Eintrag das.],
      [--], [2],

      [FA06\ Inline-Anzeige],
      [- Oberhalb jeder Zeile mit einer Label-ID erscheinen die Übersetzungen aller konfigurierten Sprachen.
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

      [FA08\ Im Tool Window öffnen],
      [- Ein Command auf einer Label-ID im Code öffnet das Label in der
         Detailansicht des Tool Window.
       - Dort lässt es sich wie in FA03 bearbeiten.],
      [FA03], [2],

      [FA09\ Extraktion hardcodierter Texte],
      [- Im Editor wird ein markiertes String Literal als Ganzes zu einem neuen
         Label und enthält danach die Label-ID. Rückgängig machen
         stellt den alten Text im Code wieder her.
       - Im Designer lassen sich Label-Eigenschaften der gewählten Node mit
         hardcodiertem Text extrahieren, etwa Label und Help Text. Die
         Eigenschaft enthält danach die Label-ID.
       - Vorgeschlagen wird eine Label-Datei des Models, zu dem die Datei gehört.
       - Das neue Label ist mit dem extrahierten Text vorbelegt.],
      [FA02], [3],

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
         Gross- und Kleinschreibung sowie Match word.
       - Für denselben Datensatz und Suchbegriff liefert jeder Suchmodus dieselben
         Treffer wie das bestehende Tool. Der Treffer, den das bestehende Tool
         zuoberst zeigt, steht auch in der Extension zuoberst.],
      [FA01], [1],

      [FA13\ Label ersetzen],
      [- Alle Verwendungen eines Labels in beschreibbaren Models werden durch eine
         andere, bestehende Label-ID ersetzt, in ungespeicherten Elementen erst
         nach dem Speichern.
       - Das Output Window meldet, wie viele Dateien geändert wurden.],
      [FA04], [1],

      [FA14\ Label-ID einfügen],
      [- Einfügen setzt die vollständige ID des gewählten Labels an die
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
       - Die Extension arbeitet auf der UDE und auf der
         klassischen Entwicklungs-VM, kurz klassische VM.
       - Alle Testfälle bestehen unter Visual Studio 2026.
       - Unter Visual Studio 2022 hält das Testprotokoll das Ergebnis jedes
         Testfalls fest.],
      [TC14, TC36,\ TC37 in D4],

      [NFA02\ Performance],
      [- Eine Suche über alle geladenen Labels dauert höchstens 500 ms, gemessen
         auf der Testumgebung mit den dort vorhandenen Label-Dateien.
       - Das Laden läuft im Hintergrund. Visual Studio meldet währenddessen keine
         blockierte Oberfläche.],
      [TC15, TC35\ in D4],

      [NFA03\ Erweiterbarkeit],
      [- Die Kernlogik hat keine Abhängigkeit zu Visual Studio.
       - Ein weiterer Übersetzungsdienst lässt sich ergänzen, ohne bestehende
         Komponenten zu ändern.],
      [TC38],

      [NFA04\ Stabilität],
      [- Jeder Einstiegspunkt, also Command, MEF-Teil und Background Task, fängt
         Fehler ab und meldet sie im Output Window (Error Boundary).
       - Eine beschädigte Label-Datei, ein fehlendes Package-Verzeichnis, ein
         nicht erreichbarer Übersetzungsdienst und eine Datei, die ein anderes
         Programm blockiert, führen zu einer Meldung. Visual Studio läuft weiter.],
      [TC10, TC11, TC22,\ TC25, TC30, TC33],

      [NFA05\ Wartbarkeit],
      [- Öffentliche Klassen und Methoden sind dokumentiert.
       - Der Build läuft ohne Warnungen.],
      [TC39],

      [NFA06\ Sicherheit],
      [- Der API-Schlüssel liegt verschlüsselt über die Data Protection API im
         Benutzerprofil.
       - Er erscheint weder im Klartext auf der Festplatte noch im Output Window
         noch im Repository.],
      [TC23],
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
      [TC15, in D4\ wiederholt],

      [OA02\ Dokumentation],
      [- Das Repository beschreibt Aufbau, Build und Erweiterungspunkte so, dass
         eine andere Person die Weiterentwicklung übernehmen kann.],
      [TC40],

      [OA03\ Quellcodeablage],
      [- Das Repository enthält weder Code der Standardanwendung noch Code des
         bestehenden Tools.
       - Testdaten sind synthetisch.],
      [TC40, Prüfung\ vor jedem Commit],

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

Die Kann-Anforderungen zählen nicht zu den
Erfolgskriterien und stehen nicht im Realisierungsplan. Umgesetzt werden sie nur,
wenn nach AP4.1 Zeit bleibt, sonst fliessen sie in die Empfehlungen ein.

#[
  #show figure: set align(left)
  #set text(size: 9.5pt)
  #figure(
    table(
      align: left,
      columns: (9.5em, 1.6fr, 1fr),
      table.header(
        [*Kann-Anforderung*], [*Beschreibung*], [*Technische Grundlage*],
      ),
      [KA01\ Fehlende Übersetzungen hervorheben],
      [Label-IDs, denen in einer der konfigurierten Sprachen eine Übersetzung
       fehlt, sind im Code farblich markiert.],
      [Tags zum Einfärben von Text, siehe @prototypenvergleich.],

      [KA02\ Markierung am Rand],
      [Zeilen mit unvollständig übersetzten Labels tragen eine Markierung in der
       Margin des Editors. Ein Klick darauf öffnet das Label im Tool Window.],
      [Margin, siehe @prototypenvergleich.],

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
      [Schnittstelle des Übersetzungsdienstes.],
    ),
    caption: [Kann-Anforderungen (eigene Darstellung)]
  ) <kann_anforderungen_tabelle>
]

== Use-Case-Diagramm

Menschlicher Akteur ist in allen Use Cases der Entwickler. Die Developer Tools und
der Übersetzungsdienst nehmen als Systeme an einzelnen Use Cases teil. Die Use Cases fassen die funktionalen Anforderungen zu Aufgaben aus Sicht
des Entwicklers zusammen. FA17 betrifft alle und hat keinen eigenen Use Case.

#figure(
  image("../diagrams/UseCases.png", width: 9.2cm),
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
externes System einbeziehen. Für die übrigen legen die Akzeptanzkriterien der funktionalen Anforderungen das Verhalten fest.

#[
  #show figure: set align(left)
  #set text(size: 9.5pt)
  #figure(
    table(
      align: left,
      columns: (auto, 1fr),
      [*Use Case*], [UC01 Label suchen],
      [*Kurzbeschreibung*],
      [Der Entwickler sucht ein Label über Text, Kommentar oder ID, im Tool Window
       oder direkt aus dem Editor.],
      [*Akteure*], [Entwickler, bei 1b die Developer Tools],
      [*Auslöser*],
      [Der Entwickler öffnet das Tool Window oder ruft die Suche im X++-Editor
       über das Kontextmenü oder einen Shortcut auf.],
      [*Vorbedingungen*], [Die Labels sind geladen.],
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
      [Ein Label ist gewählt. Der Entwickler fügt seine ID ein oder bearbeitet es. Die Suche selbst ändert keine Datei.],
      [*Verknüpfungen*], [--],
      [*Anforderungen*], [FA01, FA07, FA12, NFA02],
      [*Testfälle*], [TC04, TC16, TC28],
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
      [*Use Case*], [UC02 Label anlegen],
      [*Kurzbeschreibung*],
      [Der Entwickler legt ein Label in einer beschreibbaren Label-Datei an. Der
       Übersetzungsdienst schlägt die Übersetzungen vor.],
      [*Akteure*], [Entwickler, Übersetzungsdienst],
      [*Auslöser*],
      [Button im Tool Window oder Shortcut für ein neues Label, auch aus UC01 ohne
       Treffer und aus UC08.],
      [*Vorbedingungen*],
      [Die Labels sind geladen. Mindestens ein beschreibbares Model hat eine
       Label-Datei.],
      [*Typischer Ablauf*],
      [+ Der Entwickler wählt im Tool Window eine Label-Datei und legt ein neues
         Label an, per Button oder Shortcut.
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
        [5a], [Eine Datei lässt sich nicht schreiben, etwa weil ein anderes
               Programm sie blockiert.
               Das Output Window nennt die Datei. Das Label bleibt als nicht
               gespeichert im Label Store, und der Entwickler speichert es erneut,
               sobald die Datei frei ist.],
        [5b], [Für eine anzulegende Sprache hat die Label-Datei keine Datei. Das
               Label entsteht in den übrigen Sprachen, das Output Window nennt die
               fehlende.],
      )],
      [*Nachbedingungen*],
      [Das Label steht in allen anzulegenden Sprachen, die die Label-Datei hat,
       in der Label-Datei und im Label Store. Dynamics 365 kompiliert die Datei
       ohne Fehler.],
      [*Verknüpfungen*], [Wird von UC08 eingebunden.],
      [*Anforderungen*], [FA02, FA10, FA16, NFA04, NFA06],
      [*Testfälle*], [TC03, TC11, TC18, TC33, TC34],
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
      [*Use Case*], [UC07 Übersetzungen im Code ansehen],
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
      [*Testfälle*], [TC02, TC26, TC27],
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
      [*Use Case*], [UC08 Hardcodierten Text extrahieren],
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
               es mit Schritt 3 weiter. In Schritt 5 schreibt die Extension die Label-ID in die XML-Datei des Elements. Der
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
      [*Testfälle*], [TC31, TC32],
      [*Stufe*], [3],
    ),
    caption: [Use Case UC08 Hardcodierten Text extrahieren (eigene Darstellung)]
  ) <uc_08>
]

== Systemarchitektur <architektur>

Die Extension heisst BE-LabelExtension. Die Lösung liegt im Repository unter
`source` und besteht aus drei Projekten, `BE.LabelExtension`,
`BE.LabelExtension.Core` und `BE.LabelExtension.Tests`. Die Kernlogik enthält alles, was ohne
Visual Studio auskommt, und zielt auf .NET Standard 2.0. Die Extension zielt auf
.NET Framework 4.8, weil sie im Prozess von Visual Studio läuft
@ms-extensibility-models, und bindet die
Kernlogik als Assembly ein. Ein Testprojekt prüft die Kernlogik ohne Visual Studio und ohne
Dynamics 365. Es zielt auf .NET Framework 4.8, die Runtime, auf der Visual Studio
die Extension ausführt.

Weil .NET Standard weder WPF noch das Visual Studio SDK kennt, kann die Kernlogik
nicht versehentlich von Visual Studio abhängig werden. Damit ist Z8 in der Struktur
der Lösung verankert. .NET Standard ist dabei keine eigene Runtime, sondern eine
Spezifikation von Schnittstellen, die .NET Framework 4.8 umsetzt @ms-net-standard.
Ausgeführt wird die ganze Lösung deshalb auf einer einzigen Runtime. Ausgeliefert wird ein einziges VSIX-Paket mit den Assemblies
der Extension und der Kernlogik.

#figure(
  image("../diagrams/Systemarchitektur.png", width: 95%),
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
      [Suche, Trefferliste, Detailansicht, Bearbeiten, Verwendungen. Mit Remote
       UI des neuen Modells, fehlt dort ein Control, als klassisches Tool Window
       mit WPF. Ob Remote UI genügt, zeigt AP3.1.],
      [FA01 -- FA04, FA11 -- FA13],
      [Commands],
      [Suche aus dem Editor, Öffnen im Tool Window, Label-ID einfügen, Extraktion,
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

== Modellierung der Klassen

=== Klassendiagramm

Das Klassenmodell zeigt die Kernlogik. Die Extension setzt ihre Komponenten mit Klassen um, die Visual Studio anbinden und die Kernlogik
aufrufen. Eigene Fachlogik enthalten sie nicht, damit sich alles, was geprüft
werden muss, ohne Visual Studio testen lässt. Die Namen sind die späteren Namen im
Code und deshalb englisch.

@klassen_laden folgt für die Fachklassen dem Zusammenhang aus @labelstruktur.

#figure(
  image("../diagrams/Klassen_Laden.png", width: 92%),
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
      [- Teilt eine ID der neuen Form wie `@BDM1:BDM110000003` in Label-Datei
         und Label.
       - Die alte Form wie `@SYS12345` bleibt ungeteilt. Ihre Label-Datei ergibt
         sich aus der Datei, in der das Label steht.
       - Alle Funktionen verarbeiten beide Formen, neue Labels entstehen nur in
         der neuen.
       - Im X++-Editor erkennt die Extension Label-IDs über die Classification
         der Developer Tools. Wo diese fehlt, findet `FindAll` sie mit einer Regex
         direkt im Text einer Zeile. Das gilt auf dem privaten Gerät, wo es keinen
         X++-Editor gibt, und als Fallback aus R07.],
      [`TryParse`\ `Parse`\ `FindAll`],

      [`Label`\ `Translation`],
      [Ein Label mit Text und Kommentar je Sprache. `IsModified` zeigt an, dass
       eine Änderung noch nicht in der Label-Datei steht.],
      [`GetText`],

      [`LabelFile`],
      [Eine Label-Datei wie `BDM1` mit den Sprachen, in denen es sie gibt.
       Änderbar nur in einem beschreibbaren Model und nicht, wenn sie nur
       kompiliert vorliegt.],
      [--],

      [`ModelInfo`\ `ModelDiscovery`],
      [- Liest die Ordner der Metadaten-Konfiguration.
       - Findet die Models über ihre Descriptor-Dateien und erkennt
         schreibgeschützte.
       - `FindModelFor` ordnet einer Datei ihr Model zu, auch einer `.xpp`-Datei
         aus XppSource. Daraus folgt der Vorschlag der Label-Datei bei der
         Extraktion.],
      [`ReadConfiguration`\ `FindModels`\ `FindModelFor`],

      [`ILabelSource`],
      [- Liefert die Label-Dateien eines Models. `LabelFileSource` liest die
         Label-Dateien, `CompiledLabelSource` die kompilierten Ressourcen.
       - Die Sprache ergibt sich aus Ordner und Dateiname, weil der
         XML-Beschreibung für en-US das Element `Language` fehlt.],
      [`Load`],

      [`LabelFileFormat`],
      [- Liest und schreibt das Format aus @label_datei, eine Datei
         `<Label-Datei>.<Sprache>.label.txt` je Sprache.
       - Neue Labels kommen ans Ende, die übrigen behalten ihre Reihenfolge.
       - UTF-8, ein leerer Text als ein Leerzeichen, wie im bestehenden Tool.],
      [`Read`\ `Write`],

      [`LabelStore`],
      [- Hält alle Labels, nach Label-ID indiziert. `Find` liefert `null` für
         eine unbekannte ID.
       - Neu geladen wird in einen zweiten Index, etwa nach einer Änderung von
         aussen. Erst wenn das Laden fertig ist, tritt er an die Stelle des
         alten. Suche und Tooltip arbeiten so bis dahin mit dem bisherigen,
         vollständigen Stand statt mit halb geladenen Labels.
       - Ungespeicherte Labels bleiben markiert und gehen beim Neuladen nicht
         verloren.],
      [`LoadAsync`\ `Find`],

      [`LabelFileWatcher`],
      [Meldet Änderungen an beschreibbaren Label-Dateien von aussen. Während
       eigener Schreibvorgänge ist er angehalten.],
      [`Suspend`],

      [`LabelSearch`],
      [Suche mit Ranking. `SearchMode` und `CaseSensitive` ergeben die acht Suchmodi des bestehenden Tools, `Id` und `MatchWord` gibt es nur einmal.],
      [`Search`],

      [`LabelOperations`],
      [- Anlegen, Ändern, Löschen, Kopieren und Verschieben, auf Wunsch mit
         Umstellen der Referenzen.
       - Schreibt alle Sprachen der Label-Datei über eine temporäre Datei, die
         erst am Ende die alte ersetzt.
       - Anlegen, Kopieren und Verschieben speichern sofort, ebenso Extraktion
         und Ersetzen, weil danach Code auf das Label zeigen kann. Ändern und
         Löschen warten auf Save.],
      [`Create`\ `Update`\ `Delete`\ `Copy`\ `Move`],

      [`LabelIdGenerator`],
      [Erzeugt neue Label-IDs nach FA02, mit der Logik, die BE-terna bereits
       einsetzt.],
      [`NewId`],

      [`UsageSearch`\ `LabelUsage`],
      [- Sucht die vollständige Label-ID als Text in den XML-Dateien der
         Elemente. `@SYS1234` trifft dabei nicht `@SYS12345`.
       - Ersetzt Referenzen nur in beschreibbaren Models.],
      [`FindUsages`\ `ReplaceReferences`],

      [`ITranslationService`\ `DeepLTranslationService`],
      [- Übersetzt einen Text in mehrere Zielsprachen, bei DeepL mit einer
         Anfrage je Zielsprache @deepl-translate.
       - DeepL verwendet eigene Sprachcodes. Für `de-CH` gibt es dort die
         Variante `DE-CH`, die ss statt ß schreibt, etwa _Strasse_ statt
         _Straße_ @deepl-swiss-german. Für `fr-CH` und `it-CH` fehlt eine solche Variante,
         übersetzt wird nach `FR` und `IT`. Als Quellsprache nimmt DeepL nur den
         Grundcode, also `DE` statt `DE-CH` @deepl-languages.
       - Schlüssel für DeepL API Free enden auf `:fx` und gehen an
         `api-free.deepl.com`, alle anderen an `api.deepl.com` @deepl-auth.
         Laut Dokumentation, nicht erprobt.],
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
      columns: (7.5em, auto, 1fr),
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
       etwa für die IDs beim Lesen einer Label-Datei.],

      [Strategy],
      [`ILabelSource`\ `ITranslationService`],
      [Eine neue Quelle oder ein neuer Übersetzungsdienst kommt als eigene Klasse
       dazu, ohne dass sich Label Store oder Tool Window ändern. Unit Tests
       setzen eine Umsetzung mit festen Daten ein und kommen ohne Dateisystem und
       ohne Internet aus.],

      [Dependency Inversion],
      [`IMessageSink`],
      [Die Kernlogik legt das Interface für Meldungen fest, die Extension setzt es
       mit dem Output Window um. So braucht die Kernlogik keine Referenz auf Visual Studio.],

      [Task-based Asynchronous Pattern],
      [`LoadAsync`\ `TranslateAsync`],
      [Laden und Übersetzen dauern und dürfen Visual Studio nicht blockieren. Microsoft empfiehlt das Pattern für neue Entwicklung @ms-tap. Der
       `CancellationToken` bricht das Laden ab, wenn Visual Studio schliesst oder
       ein neues Laden beginnt.],

      [Observer über Events],
      [`Changed`\ `ExternalChange`],
      [Tool Window und Inline-Anzeige erfahren von neuen Labels, ohne dass die
       Kernlogik sie kennt.],

      [`using`-Block mit `IDisposable`],
      [`LabelFileWatcher`\ `.Suspend`],
      [Innerhalb des Blocks ruht die Dateiüberwachung. Am Ende läuft sie wieder,
       auch wenn das Schreiben mit einer Exception abbricht @ms-using. Eigene Schreibvorgänge lösen so keine Meldung aus, und ein Fehler legt die
       Überwachung nicht dauerhaft still.],
    ),
    caption: [Design Patterns der Kernlogik (eigene Darstellung)]
  ) <design_patterns>
]

== Sequenzdiagramme

Die Sequenzen zeigen die Abläufe, in denen die Extension mit Visual Studio,
den Developer Tools oder einem externen Dienst zusammenspielt. Die übrigen
Abläufe bleiben im Tool Window und in der Kernlogik.

=== Laden

Das Laden beginnt, sobald das Tool Window oder die erste X++-Datei geöffnet wird,
und läuft im Hintergrund, ohne dass Visual Studio darauf wartet.

#figure(
  image("../diagrams/Sequenz_Laden.png", width: 90%),
  caption: [Sequenz beim Laden der Labels (eigene Darstellung)]
) <sequenz_laden>

#heading(outlined: false, level: 4)[Package-Verzeichnisse] <package_verzeichnisse>

Die Package-Verzeichnisse sind konfigurierbar, und es können mehrere sein. Wo sie
liegen, unterscheidet sich zwischen der UDE und der klassischen VM.

Auf der UDE legt eine Metadaten-Konfiguration der Developer Tools die Ordner fest
@ms-ude-tools. Jede Konfiguration ist eine JSON-Datei im Ordner
`%LOCALAPPDATA%\Microsoft\Dynamics365\XPPConfig`, aktiv ist immer genau eine
@bucher-ude. Die Extension liest daraus zwei Arten von Ordnern.

- `ModelStoreFolder` enthält die eigenen Models.
- `FrameworkDirectory` und `ReferencePackagesPaths` enthalten die
  Referenz-Metadaten, darunter das entpackte `PackagesLocalDirectory` mit den
  Models von Microsoft.

Diese Einträge sind auf der Testumgebung geprüft. Welche Konfiguration aktiv ist,
fand sich dort in keiner Datei, und beim Wechsel der Konfiguration änderte sich auch
keine. Der Entwickler wählt sie deshalb in den Einstellungen, gibt es nur eine, ist
sie vorgewählt. Sie automatisch zu erkennen bleibt vorgemerkt. Weil sich Namen und
Pfade mit Updates ändern, liest die Extension die gewählte Datei bei jedem Laden
neu.

Auf der klassischen VM liegen eigene Models und die von Microsoft in
einem gemeinsamen `PackagesLocalDirectory`, meist unter `K:\AOSService` oder
`C:\AOSService`, je nach Maschine auch auf einem anderen Laufwerk
@ms-vc-navigation. Findet die Extension keine Metadaten-Konfiguration, schlägt sie
wie das bestehende Tool das erste vorhandene dieser beiden Verzeichnisse vor.
Weitere Verzeichnisse lassen sich von Hand ergänzen.

#heading(outlined: false, level: 4)[Kompilierte Labels] <kompilierte_labels>

Es gibt Models, die ihre Labels nur als kompilierte Ressourcen mitbringen, je
Label-Datei und Sprache in einer Datei `<Label-Datei>.Resources.dll`. Gibt es für
dieselbe Sprache auch die Label-Datei, gilt diese. Das bestehende Tool liest die
Ressourcen, indem es die Assembly lädt. Im Prozess von Visual Studio
bliebe die Datei bis zum Schliessen geladen @ms-assembly-unload, und ein erneutes
Laden lieferte die alte Fassung @ms-assembly-loadfrom. Die Kernlogik liest die
Ressourcen deshalb direkt aus der Datei, ohne die Assembly zu laden. An einem
echten Model zeigt sich das in D1.

=== Tooltip

Beim Überfahren einer Label-ID ergänzt eine eigene QuickInfo-Quelle den Tooltip des
X++-Editors um einen Eintrag mit allen konfigurierten Sprachen. @sequenz_tooltip zeigt den Ablauf. Die Label-ID erkennt die
Extension über die Classification der Developer Tools. Deren Namen stehen an einer
einzigen Stelle im Code, damit bei einer Änderung nur diese anzupassen ist.

Die Inline-Anzeige blendet die Übersetzungen oberhalb der Zeile ein. Sie verwendet dieselbe Erkennung und denselben Zugriff auf den Label
Store wie die QuickInfo-Quelle.

#figure(
  image("../diagrams/Sequenz_Tooltip.png", width: 95%),
  caption: [Sequenz beim Überfahren einer Label-ID (eigene Darstellung)]
) <sequenz_tooltip>

=== Extraktion

@sequenz_extraktion zeigt die Extraktion im X++-Editor. Die Extension erkennt das
String Literal über die Classification `"X++ String"` und ersetzt es mit einem Edit
im Text Buffer. Rückgängig machen und Speichern bleiben damit bei Visual Studio und
den Developer Tools. Der Editor zeigt eine `.xpp`-Datei, die die Developer Tools beim
Öffnen des Elements im Ordner XppSource erzeugen. Eine Änderung direkt an dieser
Datei zeigt der Editor zwar an, beim nächsten Öffnen des Elements ist sie aber
verworfen. Die Extension schreibt deshalb nie in `.xpp`-Dateien.

#figure(
  image("../diagrams/Sequenz_Extraktion.png", width: 92%),
  caption: [Sequenz beim Extrahieren eines hardcodierten Textes im X++-Editor
            (eigene Darstellung)]
) <sequenz_extraktion>

#heading(outlined: false, level: 4)[Extraktion im Designer] <extraktion_designer>

Im Designer gibt es keinen Text Buffer. Die Extension liest die gewählte Node über
das Selection Tracking und schreibt direkt in die XML-Datei des Elements.

+ Elementtyp und Elementname aus dem Selection Tracking ergeben die XML-Datei.
+ Der Name der gewählten Node ergibt die Stelle darin.
+ Ist das Element gespeichert, ersetzt die Extension dort den Text der gewählten
  Eigenschaft durch die Label-ID. Sonst bietet sie an, es zuerst zu speichern.

Findet sie keine eindeutige Stelle, bricht sie mit einer Meldung ab. Ob die
Zuordnung über die Namen trägt, zeigt eine Probe in D1. Als Fallback bleibt
vorgemerkt, die Eigenschaft über das gewählte Element zu setzen. Die Property
Descriptors melden sie als beschreibbar, erprobt ist das nicht.

=== Sprung an die Fundstelle <sprung_fundstelle>

Die Verwendungssuche findet die Label-ID in der XML-Datei des Elements, der
X++-Editor zeigt aber die `.xpp`-Datei. Für den Sprung in den Code zählt die
Extension deshalb die Vorkommen der Label-ID, wie @sequenz_sprung zeigt. Ist die
Fundstelle das dritte Vorkommen im Code des XML, setzt sie den Cursor auf das
dritte Vorkommen in der `.xpp`-Datei. Wie sich ein Element öffnen und darin eine
Node wählen lässt, zeigt eine Probe in D1.

#figure(
  image("../diagrams/Sequenz_Sprung.png", width: 85%),
  caption: [Sequenz beim Sprung an eine Fundstelle (eigene Darstellung)]
) <sequenz_sprung>

== GUI-Design <gui_design>

Die Oberfläche übernimmt Aufbau und Begriffe des bestehenden Tools, damit sich
Entwickler, die es kennen, ohne Einarbeitung zurechtfinden. Beschriftet ist sie wie
das bestehende Tool auf Englisch.

=== Tool Window

Das Tool Window soll auch schmal am Rand angedockt benutzbar sein. Suche,
Trefferliste, Detailansicht und Verwendungen stehen deshalb untereinander wie in
@gui_toolwindow und nicht nebeneinander wie in @ist_screenshot. Die Trefferliste hat
eine Spalte je geladene Sprache.

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
       an der Cursorposition ein, statt sie in die Zwischenablage zu kopieren.],
      [Ein Button je beschreibbares Model],
      [Auswahl der Label-Datei und ein Button New. Eine Reihe von Buttons passt nicht
       in ein schmales Tool Window.],
      [Verwendungssuche],
      [Bereich References unter der Detailansicht. Ein Klick auf das Symbol einer
       Fundstelle öffnet das Element an dieser Stelle.],
      [Console],
      [Eigener Bereich BE-LabelExtension im Output Window.],
      [Settings],
      [Seite in den Optionen von Visual Studio. Der Button Settings öffnet sie.],
      [Tracing (Preview)],
      [Entfällt, siehe @abgrenzung.],
    ),
    caption: [Bedienelemente des bestehenden Tools in der Extension (eigene
              Darstellung)]
  ) <gui_zuordnung>
]

Ein neues Label erscheint in der Detailansicht ohne ID, bis der Entwickler
bestätigt. Vorschläge des Übersetzungsdienstes sind markiert, bis er sie ändert oder
bestätigt. Geänderte und gelöschte Labels sind in der Trefferliste markiert, bis er
sie mit Save speichert. Sind beim Schliessen von Visual Studio noch Labels
ungespeichert, fragt die Extension nach.

=== Editor und Designer

Im X++-Editor erscheinen die Übersetzungen im Tooltip und oberhalb der Zeile wie in
@demo_quickinfo und @demo_inline. Eine fehlende Übersetzung ist als `missing`
markiert. Das Kontextmenü des Editors erhält drei Commands, Search Label, Open in Label Window und Extract to Label. Sie stehen auch im Tool
Window. Lässt sich das Kontextmenü des X++-Editors nicht erweitern, stehen sie im
Menü Extensions, die Shortcuts gelten in beiden Fällen. Im Designer wirkt der Button
Extract im Tool Window auf das gewählte Element.

=== Einstellungen

In den Optionen von Visual Studio erhält die Extension eine eigene Seite
BE-LabelExtension mit den Einstellungen aus FA11. Der API-Schlüssel erscheint dort
verdeckt. Auf einer klassischen VM bleibt die Auswahl der Metadaten-Konfiguration leer, und unter den
weiteren Package-Verzeichnissen steht das gefundene Standardverzeichnis.

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
  laufen bei jedem Build unter .NET Framework 4.8 und verwenden vorerst xUnit.
  Macht es Probleme, lässt es sich durch ein anderes Framework ersetzen, ohne die
  Kernlogik zu ändern.
- Systemtests prüfen die Extension in Visual Studio. Sie laufen zuerst auf dem
  privaten Gerät mit dem Debug-Build. Dort gibt es keinen X++-Editor. Im Debug-Build
  hängen sich Tooltip und Inline-Anzeige deshalb zusätzlich an Textdateien mit der
  Endung `.xpp` an und erkennen Labels über eine Regex, dieselbe wie im Fallback aus
  R07. Das senkt die Zahl der Durchgänge. Auf der Testumgebung laufen sie in den Durchgängen D1 bis D4, in D4 vollständig. Die Testumgebung umfasst ein System mit der
  UDE und eine klassische VM, beide mit
  Visual Studio 2026. Die Durchgänge laufen vor allem auf der UDE.
- Reviews prüfen, was sich nicht ausführen lässt, etwa die Abhängigkeiten der
  Kernlogik.

Die Unit Tests arbeiten mit synthetischen Testdaten, die in AP3.2 entstehen und
keine Daten des Arbeitgebers enthalten. Sie decken beide Label-Formen,
schreibgeschützte Models, kompilierte Ressourcen und beschädigte Label-Dateien ab,
dazu beide Ablagen, eine Metadaten-Konfiguration der UDE
und das `PackagesLocalDirectory` einer klassischen VM.
Für FA12 werden die Treffer des bestehenden Tools auf denselben Testdaten einmal
erfasst und als erwartete Ergebnisse abgelegt.

Testfälle, die auf der Testumgebung schreiben, verwenden dort ein bestehendes
Model. Für die Testfälle mit dem Übersetzungsdienst stellt der Autor einen eigenen
API-Schlüssel.

Ein Testfall ist bestanden, wenn alle erwarteten Ergebnisse eintreten. Fehler, die
eine Anforderung verletzen, werden bis AP4.1 behoben.

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
      [TO3], [VSIX-Paket unter Visual Studio 2026 auf der UDE der Testumgebung],
      [Systemtest in D1 bis D4],
      [TO4], [VSIX-Paket unter Visual Studio 2022 auf der UDE der Testumgebung],
      [Systemtest in D4],
      [TO5], [VSIX-Paket unter Visual Studio 2026 auf der klassischen VM der Testumgebung],
      [Systemtest in D4],
      [TO6], [Quellcode und Repository], [Review],
    ),
    caption: [Testobjekte (eigene Darstellung)]
  ) <testobjekte>
]

=== Testfälle

Die Systemtests TC15 bis TC24 laufen zuerst auf dem privaten Gerät. D4 wiederholt
sie auf der Testumgebung, TC24 auf der klassischen VM, die übrigen auf der UDE. OA04
folgt erst nach Projektabschluss und hat keinen Testfall.

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
      [Label-Datei mit Kommentaren, beiden Label-Formen und leeren Texten lesen
       und wieder schreiben. Leere Texte stehen als ein Leerzeichen.],
      [Die geschriebene Datei ist Byte für Byte gleich wie die gelesene, in
       UTF-8.],
      [FA03],

      [TC02],
      [Label-IDs beider Formen parsen und in einer Textzeile finden, dazu
       ungültige Eingaben.],
      [`@BDM1:L3F2A9C15B8047DE1` und `@SYS12345` werden erkannt. Die neue Form
       wird in Label-Datei und Label getrennt, die alte bleibt ungeteilt. Ungültige Eingaben liefern `false` ohne Exception. `FindAll` findet alle IDs
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
      [Die Treffer entsprechen den erfassten Ergebnissen des bestehenden Tools,
       der oberste Treffer ist derselbe. Jedes Label erscheint einmal.],
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
       Datei lässt sich ersetzen. Gibt es zur selben Label-Datei und Sprache eine
       `.label.txt`, wird die Ressource übersprungen.],
      [@kompilierte_labels],

      [TC09],
      [Ein Label ungespeichert ändern, die Label-Datei von aussen ändern und neu
       laden, danach selbst speichern.],
      [Die Änderung von aussen löst eine Meldung aus, das eigene Speichern nicht.
       Das ungespeicherte Label ist nach dem Neuladen noch da.],
      [FA15],

      [TC10],
      [Laden mit einer beschädigten Label-Datei, einem fehlenden
       Package-Verzeichnis und einer Datei, die ein anderes Programm blockiert.],
      [Je eine Meldung mit Datei oder Pfad, die übrigen Labels sind geladen. Keine
       Exception verlässt die Kernlogik.],
      [NFA04],

      [TC11],
      [Mit einem simulierten Übersetzungsdienst nach `de-CH` und `fr-CH`
       übersetzen, dazu eine Fehlerantwort.],
      [Eine Anfrage je Zielsprache. `de-CH` geht als `DE-CH` hinaus, `fr-CH` als
       `FR`. Als Quellsprache geht `de-CH` als `DE` hinaus. Die Fehlerantwort
       ergibt einen Fehler mit Grund statt Vorschlägen.],
      [FA10\ NFA04],

      [TC12],
      [Label-Datei in en-US lesen, deren XML-Beschreibung kein Element
       `Language` hat.],
      [Die Sprache ergibt sich aus Ordner und Dateiname unter `LabelResources`,
       en-US wird erkannt.],
      [FA01],

      [TC13],
      [`FindModelFor` mit dem Pfad einer XML-Datei aus einem Package-Verzeichnis
       und mit dem einer `.xpp`-Datei aus XppSource aufrufen.],
      [Beide Aufrufe liefern das richtige Model.],
      [FA09],

      [TC14],
      [Package-Verzeichnisse aus einer Metadaten-Konfiguration bestimmen, danach
       aus einem Aufbau ohne Konfiguration wie auf einer klassischen VM.],
      [Die Ordner aus `ModelStoreFolder`, `FrameworkDirectory` und
       `ReferencePackagesPaths` werden gefunden. Ohne Konfiguration schlägt die
       Extension das vorhandene Standardverzeichnis vor.],
      [FA11\ NFA01],

      table.cell(colspan: 4)[*Systemtests auf dem privaten Gerät, TO2*],
      [TC15],
      [VSIX-Paket per Doppelklick installieren, Visual Studio starten und das Tool
       Window öffnen.],
      [Die Installation braucht keine weiteren Schritte. Das Tool Window öffnet
       sich, das Laden läuft im Hintergrund.],
      [OA01\ NFA02],

      [TC16],
      [Im Tool Window in jedem Suchmodus suchen und einen Treffer wählen.],
      [Jedes Label erscheint einmal. Die Detailansicht zeigt alle geladenen
       Sprachen.],
      [FA01\ FA12],

      [TC17],
      [Text und Kommentar in zwei Sprachen ändern und speichern, danach ein Label
       löschen.],
      [Die Label-Dateien enthalten die Änderungen. Das gelöschte Label fehlt in
       allen Sprachen.],
      [FA03],

      [TC18],
      [Nach einer Suche ohne Treffer mit dem Shortcut ein neues Label anlegen,
       auch in einer Label-Datei, der eine anzulegende Sprache fehlt.],
      [Alle anzulegenden Sprachen sind mit dem Suchbegriff vorbelegt. Nach dem
       Bestätigen steht das Label mit einer ID nach FA02 in allen
       vorhandenen Dateien. Die fehlende Sprache nennt das Output Window.],
      [FA02\ FA16],

      [TC19],
      [Label-ID einfügen mit und ohne aktiven Editor, danach Save and insert.],
      [Die ID steht an der Cursorposition. Ohne Editor erscheint ein Hinweis. Save
       and insert speichert vorher.],
      [FA14],

      [TC20],
      [Alle Shortcuts auslösen und einen davon in den Optionen umbelegen.],
      [Jede Funktion reagiert. Die Optionen zeigen keine Doppelbelegung, der neue
       Shortcut wirkt.],
      [FA16],

      [TC21],
      [Sprachen und Metadaten-Konfiguration ändern, neu laden, dann Visual Studio
       neu starten.],
      [Die neuen Sprachen wirken nach dem Laden ohne Neustart. Nach dem Neustart
       sind alle Einstellungen erhalten.],
      [FA11],

      [TC22],
      [Einen Fehler auslösen, etwa mit einem ungültigen Package-Verzeichnis.],
      [Die Meldung steht mit Art und Zeitpunkt im eigenen Bereich des Output
       Window. Visual Studio läuft weiter.],
      [FA17\ NFA04],

      [TC23],
      [API-Schlüssel hinterlegen, dann Einstellungsdateien, Output Window und
       Repository nach ihm durchsuchen.],
      [Der Schlüssel erscheint nirgends im Klartext.],
      [NFA06],

      [TC24],
      [Eine Label-Datei der Testdaten im bestehenden Tool ändern und speichern,
       danach in der Extension. In D4 auf der klassischen VM
       wiederholt, weil das bestehende Tool nur deren Pfade kennt.],
      [Nach der Änderung im bestehenden Tool bietet die Extension das Neuladen
       an, nach der Änderung in der Extension das bestehende Tool. Nicht
       gespeicherte Labels der Extension bleiben erhalten.],
      [FA15],

      table.cell(colspan: 4)[*Systemtests auf der Testumgebung, TO3 bis TO5*],
      [TC25],
      [Extension neben den Developer Tools laden, die Labels der gewählten
       Metadaten-Konfiguration laden lassen und eine X++-Klasse öffnen, in D1.],
      [Beide laufen ohne Fehlermeldung, die Developer Tools arbeiten wie ohne
       Extension. Label-Dateien und kompilierte Ressourcen sind geladen,
       fehlerhafte Dateien stehen mit Namen im Output Window, die Ladezeit ist
       gemeldet.],
      [FA11\ NFA04\ R06],

      [TC26],
      [Label-IDs beider Formen im X++-Editor überfahren, darunter eine unbekannte
       und eine mit fehlender Übersetzung, in D2.],
      [Der Tooltip zeigt alle konfigurierten Sprachen neben dem Eintrag der
       Developer Tools. Die fehlende Übersetzung ist markiert, die unbekannte ID
       gemeldet.],
      [FA05],

      [TC27],
      [Datei mit mehreren Label-IDs öffnen, die Inline-Anzeige aus- und
       einschalten, Visual Studio neu starten, in D2.],
      [Oberhalb jeder Zeile mit einer Label-ID stehen die Übersetzungen. Die
       Einstellung bleibt nach dem Neustart erhalten.],
      [FA06],

      [TC28],
      [Suche aus dem Editor, dann auf einer Label-ID das
       Label im Tool Window öffnen, in D2.],
      [Gesucht wird mit dem markierten Text oder dem Inhalt des String Literals.
       Das Label öffnet sich in der Detailansicht und lässt sich bearbeiten.],
      [FA07\ FA08],

      [TC29],
      [Verwendungen eines Labels suchen und eine Fundstelle anklicken, in D2.],
      [Alle Fundstellen mit Model, Datei, Zeile und Spalte. Ein Klick auf das
       Symbol einer Fundstelle im Code öffnet das Element im X++-Editor mit dem
       Cursor auf der Label-ID. Bei einer Fundstelle in einer Eigenschaft öffnet
       sich das Element im Designer mit gewählter Node und markierter
       Eigenschaft, mindestens aber das Element.],
      [FA04],

      [TC30],
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

      [TC31],
      [String Literal im X++-Editor extrahieren, danach rückgängig machen, in D3.],
      [Das Literal enthält die neue Label-ID, das Label steht in der Label-Datei.
       Rückgängig stellt den Text wieder her.],
      [FA09],

      [TC32],
      [Im Designer ein Element mit hardcodiertem Label und Help Text wählen und
       extrahieren, in D3.],
      [Beide Eigenschaften stehen zur Wahl. Die gewählte enthält danach die
       Label-ID. Der Designer meldet die Änderung und lädt das Element nach
       Bestätigung neu.],
      [FA09],

      [TC33],
      [Label mit Übersetzungsdienst anlegen, mit gültigem und ohne API-Schlüssel,
       in D3.],
      [Vorschläge für alle anzulegenden Sprachen, vor dem Speichern änderbar. Ohne
       Schlüssel der Ausgangstext in allen Sprachen und eine Meldung.],
      [FA10\ NFA04],

      [TC34],
      [Nach Anlegen, Ändern und Verschieben von Labels das verwendete Model
       kompilieren, in D3.],
      [Der Build läuft ohne Fehler.],
      [FA02\ FA03],

      [TC35],
      [Je Suchmodus zehn Suchen über alle Label-Dateien der Testumgebung messen
       und das Laden beobachten, in D4.],
      [Jede Suche dauert höchstens 500 ms. Visual Studio meldet während des Ladens
       keine blockierte Oberfläche.],
      [NFA02],

      [TC36],
      [Alle Systemtests unter Visual Studio 2022 wiederholen, in D4.],
      [Das Testprotokoll hält das Ergebnis jedes Testfalls fest.],
      [NFA01],

      [TC37],
      [Auf der klassischen VM installieren, das vorgeschlagene
       Standardverzeichnis übernehmen, suchen, eine Label-ID im X++-Editor
       überfahren und eine Fundstelle anspringen, in D4.],
      [Das Standardverzeichnis ist vorgeschlagen, die Labels sind geladen.
       Suche, Tooltip und Sprung funktionieren wie auf der UDE.],
      [NFA01\ FA04\ FA05],

      table.cell(colspan: 4)[*Reviews, TO6*],
      [TC38],
      [Referenzen der Kernlogik prüfen und probeweise einen zweiten
       Übersetzungsdienst ergänzen.],
      [Keine Referenz auf Visual Studio. Der zweite Dienst braucht keine Änderung
       an bestehenden Klassen.],
      [NFA03\ Z8],

      [TC39],
      [Build der Solution und Dokumentation der öffentlichen Klassen prüfen.],
      [Keine Warnungen. Alle öffentlichen Klassen und Methoden sind dokumentiert.],
      [NFA05],

      [TC40],
      [Repository prüfen.],
      [Aufbau, Build und Erweiterungspunkte sind beschrieben. Kein Code der
       Standardanwendung oder des bestehenden Tools, die Testdaten sind
       synthetisch.],
      [OA02\ OA03],
    ),
    caption: [Testfälle (eigene Darstellung)]
  ) <testfaelle>
]

== Einführung und Betrieb <einfuehrung>

Eine Datenmigration entfällt. Die Extension liest und schreibt dieselben
Label-Dateien im selben Format wie das bestehende Tool. Die
Einstellungen des bestehenden Tools werden nicht übernommen, weil es nur zwei Listen
von Sprachen sind.

Eingeführt wird in drei Schritten.

+ Das VSIX-Paket wird per Doppelklick installiert.
+ Beim ersten Start trägt der Entwickler seine Sprachen ein und prüft das
  vorbelegte Package-Verzeichnis. Für Übersetzungsvorschläge hinterlegt er einen
  API-Schlüssel.
+ Wo beide Tools dieselben Package-Verzeichnisse verwenden, lassen sie sich
  nebeneinander betreiben. Ändert eines eine Label-Datei, bietet das andere das Neuladen an.

Ob und wann das bestehende Tool abgelöst wird, entscheidet BE-terna nach Projektabschluss. Sinnvoll ist eine Pilotphase mit einzelnen Entwicklern,
deren Rückmeldungen vor der Ablösung einfliessen. Das wirkt zugleich R09 entgegen.

Im Betrieb braucht die Extension weder Server noch Datenbank. Eine neue Version
kommt als neues VSIX-Paket, das die alte ersetzt. Wie das Paket verteilt wird und
woher die API-Schlüssel im Betrieb stammen, legt BE-terna fest. Die Weiterentwicklung liegt beim Maintainer. Nach einem Uninstall bleiben die Label-Dateien unverändert.

Der Code steht im öffentlichen Repository unter der MIT-Lizenz. Sie erlaubt
BE-terna, die Extension einzusetzen, zu ändern und weiterzugeben, solange der
Lizenzhinweis erhalten bleibt @osi-mit.

== Realisierungsplan <realisierungsplan>

Die Realisierung folgt den Stufen der Projektziele. Die Kernlogik kommt vor der
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
      columns: (auto, 1.5fr, 0.8fr, 1.5fr),
      table.header(
        [*AP*], [*Inhalt*], [*Anforde-\ rungen*], [*Ergebnis*],
      ),
      table.cell(colspan: 4)[*Grundgerüst*],
      [3.1\ 09.10.],
      [Solution mit drei Projekten. Build und Paketierung mit den Erkenntnissen aus
       dem Spike. Output Window, Error Boundary, leeres Tool Window. Drei
       Probe-Commands im Debug-Build für D1.],
      [FA17, NFA04],
      [Die Extension lädt in Visual Studio 2026 und öffnet ihr Tool Window. Ein
       absichtlich ausgelöster Fehler erscheint im Output Window, Visual Studio
       läuft weiter. Die Probe-Commands versuchen, ein Element an einer Stelle zu
       öffnen, ungespeicherte Dokumente aufzulisten und zur gewählten Node im
       Designer die Stelle im XML zu finden. Sie melden das Ergebnis im Output
       Window.],

      table.cell(colspan: 4)[*Stufe 1, Feature Parity*],
      [3.2\ 10.10.],
      [Model-Suche über die Metadaten-Konfiguration, Label-Dateien lesen und
       schreiben, kompilierte Labels, Label Store, Dateiüberwachung, synthetische
       Testdaten. Bis zur Einstellungsseite in AP3.5 wird die
       Metadaten-Konfiguration über eine einfache Vorgabe gewählt.],
      [FA15, NFA02],
      [Unit Tests lesen und schreiben Label-Dateien ohne Verlust von Kommentaren
       und Kodierung. Die Extension lädt die Labels der gewählten
       Metadaten-Konfiguration im Hintergrund.],
      [3.3\ 15.10.],
      [Alle Suchmodi mit Ranking. Treffer des bestehenden Tools auf den
       Testdaten als erwartete Ergebnisse erfassen.],
      [FA01, FA12, NFA02],
      [TC04 besteht für alle acht Suchmodi. Eine Suche über die Testdaten
       dauert höchstens 500 ms.],
      [3.4\ 16.10.],
      [Tool Window mit Suchfeld, Trefferliste und Detailansicht, Bearbeiten und
       Speichern.],
      [FA01, FA03],
      [Ein Label lässt sich im Tool Window finden, in allen Sprachen ändern und
       speichern.],
      [3.5\ 17.10.],
      [Anlegen, Löschen, Kopieren und Verschieben mit Referenzaktualisierung über
       die Suche der Fundstellen in der Kernlogik, Ersetzen, Label-ID einfügen,
       Einstellungen, Shortcuts.],
      [FA02, FA03, FA04, FA11, FA13, FA14, FA16],
      [Alle Funktionen des bestehenden Tools ausser der Verwendungssuche sind im
       Tool Window verfügbar. Unit Tests decken die Referenzaktualisierung ab.],

      table.cell(colspan: 4)[*Stufe 2, Kontextwechsel beseitigen*],
      [3.6\ 19.10.],
      [Label-Erkennung, Tooltip mit allen Sprachen, Inline-Anzeige, Ersatz für den
       X++-Editor im Debug-Build.],
      [FA05, FA06],
      [Der Tooltip zeigt alle konfigurierten Sprachen, auf dem privaten Gerät über
       den Ersatz, auf der Testumgebung im X++-Editor.],
      [3.7\ 20.10.],
      [Suche aus dem Editor, Öffnen im Tool Window, Verwendungssuche mit Sprung an die
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
Durchgängen auf der Testumgebung gesammelt. Der erste Durchgang liegt nach
AP3.2, sobald die Extension echte Label-Dateien lädt, damit sich die grössten
Unsicherheiten früh zeigen.

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
      [D1], [AP3.2],
      [Laden neben den Developer Tools und Laden der echten Label-Dateien über
       die Metadaten-Konfiguration, einschliesslich eines Models mit nur
       kompilierten Ressourcen (TC25). Mit den Probe-Commands aus AP3.1 ein
       Element im X++-Editor und im Designer an einer Stelle öffnen,
       ungespeicherte Elemente erkennen und zur gewählten Node die Stelle im
       XML finden. Von Hand messen, wie lange eine Textsuche über die
       XML-Dateien aller Models dauert, und prüfen, wie ein geöffneter
       X++-Editor auf eine geänderte XML-Datei reagiert und welche Datei der
       X++-Editor auf der klassischen VM öffnet.],
      [D2], [AP3.7],
      [Label-Erkennung, Tooltip, Inline-Anzeige, Suche aus dem Editor, Öffnen im
       Tool Window, Verwendungssuche mit Öffnen im X++-Editor, Schreiben in geöffnete
       Elemente (TC26 bis TC30).],
      [D3], [AP3.8],
      [Extraktion im Editor und im Properties Window, Übersetzungsdienst,
       Kompilieren nach Änderungen (TC31 bis TC34).],
      [D4], [AP4.2],
      [Alle Systemtests, zusätzlich unter Visual Studio 2022
       (TC36) und auf der klassischen VM (TC24, TC37).],
    ),
    caption: [Durchgänge auf der Testumgebung (eigene Darstellung)]
  ) <durchgaenge>
]

Reicht die Zeit an einem Tag nicht, wandert der Rest in AP4.1.
