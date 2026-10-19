= Realisierung <realisierung>

== Programmierumgebung und Richtlinien

=== Programmierumgebung

#[
  #show figure: set align(left)
  #figure(
    table(
      align: left,
      columns: (1fr, auto, 1fr),
      table.header(
        [*Tool / Technologie*], [*Version*], [*Verwendungszweck*],
      ),
      [], [], [],
      [], [], [],
      [], [], [],
    ),
    caption: [Programmierumgebung (eigene Darstellung)]
  ) <programmierumgebung>
]

=== Programmierrichtlinien

== Softwareaufbau

=== Projektstruktur

// #figure(
//   image("../diagrams/Moduluebersicht.png", width: 100%),
//   caption: [Modulübersicht (eigene Darstellung)]
// ) <moduluebersicht>

=== Kernlogik

=== Suche und Ranking

=== Anbindung an Visual Studio

== GUI-Implementierung

// #figure(
//   image("../screenshots/screenshot_01.png", width: 100%),
//   caption: [GUI (eigene Darstellung)]
// ) <gui_01>

== Datenzugriff und externe Anbindung

=== Datenzugriff

=== Übersetzungsdienst

== Entscheide und Abweichungen

=== Entscheide in der Realisierung

#[
  #show figure: set align(left)
  #figure(
    table(
      align: left,
      columns: (1fr, 1fr, 1fr),
      table.header(
        [*Entscheidung*], [*Begründung*], [*Alternativen*],
      ),
      [], [], [],
      [], [], [],
      [], [], [],
    ),
    caption: [Entscheide in der Realisierung (eigene Darstellung)]
  ) <entscheide_realisierung>
]

=== Abweichungen vom Konzept und vom bestehenden Tool

#[
  #show figure: set align(left)
  #figure(
    table(
      align: left,
      columns: (auto, 1fr, 1fr),
      table.header(
        [*Nr.*], [*Abweichung*], [*Grund*],
      ),
      [], [], [],
      [], [], [],
      [], [], [],
    ),
    caption: [Abweichungen vom Konzept und vom bestehenden Tool (eigene Darstellung)]
  ) <abweichungen_realisierung>
]

== Deployment und Installation

== Testprotokoll <testprotokoll>

=== Unit Tests

=== Durchgänge auf der Testumgebung

#heading(outlined: false, level: 4)[D1]

D1 lief am 08.10. und 09.10.2026 auf beiden Systemen der Testumgebung, mit dem
Debug-Build und den Probe-Commands aus AP3.1. Auf dem System mit der Unified
Developer Experience lief Visual Studio 2026. Auf der
klassischen Entwicklungs-VM lief Visual Studio 2022, weil
Visual Studio 2026 dort Probleme machte. Die Zeiten der Extension stammen aus ihren
eigenen Meldungen, die des bestehenden Tools aus dessen Anzeige.

#[
  #show figure: set align(left)
  #set text(size: 9.5pt)
  #figure(
    table(
      align: left,
      columns: (0.8fr, 1.7fr, 1fr),
      table.header(
        [*Prüfpunkt*], [*Ergebnis*], [*Folge*],
      ),
      [Installation],
      [Per Doppelklick auf das VSIX installiert.],
      [Keine.],

      [Fehlerbehandlung (TC22)],
      [Ein absichtlich ausgelöster Fehler erschien im Output Window, Visual Studio
       lief weiter.],
      [Keine.],

      [Laden (TC25)],
      [392'858 Labels aus 946 Label-Dateien in 185 Models in 12,2 s, darunter 126
       Label-Dateien, die nur kompiliert vorliegen. Visual Studio blieb bedienbar.
       Der Speicher von Visual Studio stieg um rund 1 GB.],
      [Ablage der Labels in AP3.4 verkleinert, Nachmessung in D2.],

      [Suchdauer],
      [Je Suchmodus zwischen 64 und 405 ms, bei Anything like that ohne Beachtung
       der Gross- und Kleinschreibung 623 bis 656 ms.],
      [In AP3.4 optimiert, Nachmessung in D2.],

      [Bestehendes Tool],
      [Mit nur zwei geladenen Sprachen 23,5 s zum Laden, 282 MB Speicher und je
       Suche 0,5 bis 1,1 s. Die Trefferzahlen stimmen mit der Extension überein.],
      [Keine.],

      [Lange Pfade],
      [18 Label-Dateien von Microsoft mit Pfaden über 260 Zeichen waren nicht
       lesbar und standen mit Namen im Output Window.],
      [Behoben, Nachprüfung in D2.],

      [Ungültige IDs],
      [22 Labels in Dateien von Microsoft mit einem Leerzeichen vor dem
       Gleichheitszeichen wurden als ungültig verworfen.],
      [Behoben, Nachprüfung in D2.],

      [Standardverzeichnis der klassischen VM],
      [Die Packages liegen auf Laufwerk J:. Die Extension wählte das fast leere
       Verzeichnis auf C: und lud keine Labels.],
      [Behoben, kann manuell konfiguriert werden.],

      [Element an einer Stelle öffnen],
      [Die XML-Datei einer Klasse öffnet immer den Designer. Den Code zeigt erst die
       Datei unter `XppSource`, dort springt der Cursor auf die gesuchte Reference.
       Auf der klassischen VM liegt sie unter
       `PackagesLocalDirectory\bin\XppSource`.],
      [Grundlage für den Sprung an Fundstellen in AP3.7.],

      [Ungespeicherte Elemente],
      [Ungespeicherte Änderungen im X++-Editor und im Designer sind erkennbar.],
      [Grundlage für das Schreiben in Elemente.],

      [Gewählte Node im Designer],
      [Die Auswahl liefert Name und Label eines Felds. Über den Namen findet sich
       die Node in der XML-Datei eindeutig, über den Wert des Labels nicht.],
      [Extraktion im Designer sucht über den Namen.],

      [Geänderte XML-Datei bei geöffnetem Element],
      [Der X++-Editor bietet das Neuladen an. Hat das Element ungespeicherte
       Änderungen und wird das Neuladen abgelehnt, überschreibt das nächste
       Speichern die Änderung von aussen.],
      [Bestätigt die Regel, nie in Elemente mit ungespeicherten Änderungen zu
       schreiben.],

      [Textsuche über die XML-Dateien],
      [Über alle 185 Models mit rund 199'000 Dateien und 1,9 GB dauerte sie 24 bis
       56 s, über die eigenen Models 0,2 bis 0,3 s. Jede Element-Datei liegt
       zweimal vor, einmal als Ausgabe des Builds.],
      [Grundlage für den Umfang der Verwendungssuche in AP3.7.],
    ),
    caption: [Ergebnisse von D1 (eigene Darstellung)]
  ) <d1_ergebnisse>
]

#heading(outlined: false, level: 4)[D2]

D2 lief am 15.10.2026 auf beiden Systemen der Testumgebung, mit dem Debug-Build
auf dem Stand von AP3.7. Die klassische Entwicklungs-VM war eine andere als in D1
und hatte Visual Studio 2026. Für die schreibenden Testfälle wurden in einem
beschreibbaren Model ein Test-Label, eine Klasse und ein Datentyp angelegt und am
Ende wieder gelöscht.

#[
  #show figure: set align(left)
  #set text(size: 9.5pt)
  #figure(
    table(
      align: left,
      columns: (0.8fr, 1.7fr, 1fr),
      table.header(
        [*Prüfpunkt*], [*Ergebnis*], [*Folge*],
      ),
      [Laden],
      [392'949 Labels aus 946 Label-Dateien in 185 Models in 16,2 s, in D1 waren
       es 12,2 s. Die 18 Label-Dateien mit langen Pfaden waren jetzt lesbar.],
      [Auf dem privaten Gerät lädt der Stand von D2 eine gleich grosse Menge
       synthetischer Labels schneller als der von D1. D3 misst die Ladezeit
       nochmals, aufgeteilt nach Phasen.],

      [Speicher],
      [Der Speicher von Visual Studio stieg beim Laden um 963 MB, in D1 um rund
       1 GB.],
      [Keine.],

      [Suchdauer],
      [Je Suchmodus zwischen 85 und 301 ms. Der langsamste Modus, Anything like
       that ohne Beachtung der Gross- und Kleinschreibung, brauchte in D1 noch 623
       bis 656 ms.],
      [Alle Modi unter 500 ms, Messung mit TC35 in D4.],

      [Ungültige IDs],
      [Die Extension verwarf noch 13 Labels von Microsoft als ungültig, in D1
       waren es 22. Sie liess in einer ID nur Buchstaben, Ziffern und Unterstrich
       zu, das bestehende Tool jedes Zeichen.],
      [Die Extension lädt solche Labels jetzt wie das bestehende Tool,
       Nachprüfung in D3.],

      [Shortcuts],
      [Nur Strg+Shift+Alt+S ist bereits belegt, und zwar nur im Scope
       `VC Image Editor`. Überall sonst gilt der Shortcut der Extension. Search
       Label und Open in Label Window stehen mit ihren Shortcuts im Kontextmenü
       des X++-Editors.],
      [Keine, die Shortcuts bleiben.],

      [Tooltip (TC26)],
      [Der Tooltip zeigt unter dem Eintrag der Developer Tools alle Sprachen,
       markiert fehlende Übersetzungen und meldet unbekannte IDs, auch bei der
       alten Form. Der Eintrag der Extension erschien aber doppelt.],
      [Behoben, Nachprüfung in D3.],

      [Inline-Anzeige (TC27)],
      [Die Anzeige stört die Referenzanzeige der Developer Tools nicht. Aus- und
       Einschalten wirkt sofort und bleibt nach einem Neustart erhalten.],
      [Keine.],

      [Suche aus dem Editor (TC28)],
      [Open in Label Window öffnet das Label in der Detailansicht. Search Label
       sucht den markierten Text, ohne Markierung den ganzen String.],
      [Keine.],

      [Verwendungssuche (TC29)],
      [Die Suche fand alle Fundstellen. Ein Klick auf eine Fundstelle im Code
       öffnete auf der Unified Developer Experience den Designer statt des
       X++-Editors, weil die Extension die `.xpp`-Datei im falschen Ordner
       suchte. Fundstellen in Eigenschaften öffneten das Element.],
      [Behoben, Nachprüfung in D3.],

      [Verschieben mit Referenzen (TC30)],
      [Für das ungespeicherte Element kam die Rückfrage. Die Referenzen wurden in
       beiden Fällen umgestellt und blieben nach dem Speichern im X++-Editor
       erhalten.],
      [Keine.],

      [Rückfrage beim Beenden],
      [Beim Beenden von Visual Studio fragte die Extension nach ungespeicherten
       Labels.],
      [Die Extension fragt jetzt auch beim Schliessen des Tool Window nach.
       Nachprüfung in D3.],

      [Darstellung des Tool Window],
      [Bei vielen Treffern fehlte die senkrechte Scroll bar der Trefferliste. Der
       Button Go to überlappte mit der Spalte Model. Der Tooltip einer Fundstelle
       zeigte bei langen Zeilen, etwa in Reports, die ganze Zeile. Unten
       angedockt, also breit und niedrig, war das Tool Window kaum brauchbar.],
      [Behoben. Ein neuer Schalter stellt Trefferliste und Details nebeneinander.
       Nachprüfung in D3.],

      [Anpassungen der Bedienung],
      [Die Spalten der Trefferliste sollen sich per Klick sortieren lassen. Nach
       dem Verschieben im Suchmodus Label id soll die Trefferliste das neue Label
       zeigen.],
      [Umgesetzt, Nachprüfung in D3.],

      [Anpassungen der Inline-Anzeige],
      [Die Inline-Anzeige soll Sprachen in konfigurierbarer Reihenfolge anzeigen
       und die Anzahl der gepflegten Übersetzungen weglassen können.],
      [Umgesetzt mit zwei neuen Optionen, Nachprüfung in D3.],

      [Klassische VM],
      [Die Extension fand das Standardverzeichnis auf K: selbst und lud 391'535
       Labels aus 905 Label-Dateien in 30,4 s. Tooltip und Inline-Anzeige
       verhielten sich wie auf der Unified Developer Experience. Ein Klick auf
       eine Fundstelle im Code öffnete den X++-Editor an der richtigen Stelle.],
      [Keine.],
    ),
    caption: [Ergebnisse von D2 (eigene Darstellung)]
  ) <d2_ergebnisse>
]

#heading(outlined: false, level: 4)[D3]

D3 lief am 16.10.2026 auf beiden Systemen der Testumgebung, mit dem Debug-Build
auf dem Stand von AP3.8. Vor jeder Messung des Ladens war Visual Studio ruhig und
hatte keine Dokumente offen. Die Testobjekte wurden wie in D2 angelegt und am Ende
wieder gelöscht.

#[
  #show figure: set align(left)
  #set text(size: 9.5pt)
  #figure(
    table(
      align: left,
      columns: (0.8fr, 1.7fr, 1fr),
      table.header(
        [*Prüfpunkt*], [*Ergebnis*], [*Folge*],
      ),
      [Laden],
      [Zwei Messungen ergaben 9,1 s und 8,6 s, davon rund 90~% für das Lesen der
       Dateien. Die 16,2 s aus D2 erklären sich damit, dass Visual Studio kurz
       nach dem Start noch beschäftigt war. Der Speicher stieg um 869 MB und
       853 MB.],
      [Keine.],

      [Ungültige IDs],
      [Die Extension verwirft beim Laden keine Labels mehr.],
      [Keine.],

      [Extraktion im Editor (TC31)],
      [Der String wurde zum Label, und Undo stellte ihn wieder her, auch mit
       Umlauten. Ohne String am Cursor kam ein Hinweis.],
      [Keine.],

      [Extraktion im Designer (TC32)],
      [Label und Help Text standen zur Wahl, und der Designer lud das Element nach
       dem Schreiben neu. Bei ungespeicherten Änderungen kam zuerst die
       Rückfrage. Auch das Feld einer Table liess sich extrahieren.],
      [Keine.],

      [Übersetzungsdienst (TC33, TC23)],
      [Ohne Key meldete die Extension, dass keine Vorschläge kommen. Mit
       Key lieferte DeepL die Vorschläge. Der Key stand weder in den
       Einstellungen noch im Output Window im Klartext.],
      [Keine.],

      [Kompilieren (TC34)],
      [Nach dem Anlegen, Extrahieren und Ändern von Labels lief der Build des
       Models ohne Fehler. Das Verschieben eines Labels wurde nicht geprüft.],
      [Verschieben folgt in D4.],

      [Verwendungssuche (TC29)],
      [Der Sprung an eine Fundstelle im Code öffnet jetzt den X++-Editor, auch
       wenn die Klasse schon offen ist.],
      [Keine.],

      [Korrekturen aus D2],
      [Der Tooltip zeigt den Eintrag der Extension nur noch einmal. Die
       Trefferliste hat eine Scroll bar und sortiert in etwa einer halben
       Sekunde. Side by side, die Rückfrage beim Schliessen des Tool Window und
       die Optionen der Inline-Anzeige funktionieren.],
      [Keine.],

      [Anpassungen der Bedienung],
      [Search Label soll ohne Markierung das Wort am Cursor suchen, etwa in einem
       Kommentar. Gelöschte Labels sollen sich vor dem Speichern wiederherstellen
       lassen, und die Delete-Taste soll löschen.],
      [Umgesetzt, Nachprüfung in D4.],

      [Anpassungen der Anzeige],
      [Die Hinweiszeile unter jedem Vorschlag von DeepL macht das neue Label
       unübersichtlich, die Meldung im Output Window genügt. Die Meldung der
       Extraktion im Designer soll klarer sagen, dass keine passende Eigenschaft
       gefunden wurde.],
      [Vorschläge stehen jetzt kursiv, die Meldung ist umformuliert. Nachprüfung
       in D4.],

      [Apply in den Einstellungen],
      [Ein Button Apply wäre hilfreich. Der Dialog Options von Visual Studio sieht
       ihn für eine einzelne Seite nicht vor.],
      [Nicht umgesetzt, die Einstellungen wirken schon nach OK.],

      [Klassische VM],
      [Die Extension lud in 15,0 s statt 30,4 s wie in D2, der Speicher stieg um
       691 MB. Die Extraktion lief wie auf der Unified Developer Experience.],
      [Keine.],
    ),
    caption: [Ergebnisse von D3 (eigene Darstellung)]
  ) <d3_ergebnisse>
]

#heading(outlined: false, level: 4)[D4]

D4 lief am 19.10.2026 als letzter Durchgang auf beiden Systemen der Testumgebung,
auf dem Stand nach AP4.1 mit den umgesetzten Kann-Anforderungen. Auf der Unified
Developer Experience lief der Debug-Build. Die klassische Entwicklungs-VM war aus
organisatorischen Gründen wieder die aus D1, mit Visual Studio 2022, und dort lief
der Release-Build. Er verhielt sich wie der Debug-Build. Korrekturen nach D4 sind
nur auf dem privaten Gerät geprüft.

#[
  #show figure: set align(left)
  #set text(size: 9.5pt)
  #figure(
    table(
      align: left,
      columns: (0.8fr, 1.7fr, 1fr),
      table.header(
        [*Prüfpunkt*], [*Ergebnis*], [*Folge*],
      ),
      [Laden (TC25, TC35)],
      [392'953 Labels in 8,7 s, davon 7,6 s für das Lesen der Dateien. Während des
       Ladens reagierte Visual Studio auf Menü und Solution Explorer.],
      [Keine.],

      [Suchdauer (TC35)],
      [Zehn Suchen je Suchmodus dauerten höchstens 253 ms, im Median je nach Modus
       72 bis 251 ms.],
      [Keine.],

      [Editor (TC26 bis TC28, KA03)],
      [Tooltip, Inline-Anzeige mit dem Argument von `strFmt` an Stelle des
       Platzhalters, Open in Label Window und Search Label am Wort funktionieren.],
      [Keine.],

      [Verwendungen und Labels des Elements (TC29, KA04)],
      [Der Sprung an die Fundstellen öffnet Editor und Designer. Element labels
       zeigt die Labels der Klasse und des Datentyps.],
      [Keine.],

      [Designer (TC32, KA05)],
      [Der Bereich zur gewählten Node zeigt ihre Label-Eigenschaften, Open und
       Extract funktionieren. Stand die ID schon im Suchfeld und hatte Element
       labels die Trefferliste seither ersetzt, zeigte Open das Label nicht.],
      [Behoben, Open sucht jetzt immer neu.],

      [Extraktion (TC31, KA08)],
      [Ein String mit dem Text eines bestehenden Labels erhielt dessen ID, statt
       ein neues Label anzulegen. Undo und Redo funktionieren.],
      [Keine.],

      [Löschen und Wiederherstellen],
      [Die Delete-Taste löscht, und Restore holt ein gelöschtes Label auch nach
       einer neuen Suche zurück.],
      [Keine.],

      [Übersetzungsdienst (TC33)],
      [Die Vorschläge von DeepL stehen kursiv, ohne Hinweiszeile.],
      [Keine.],

      [Verschieben und Kompilieren (TC30, TC34)],
      [Das Label liess sich mit Umstellen der Referenzen in eine zweite Label-Datei
       eines anderen Models verschieben, der Build lief danach ohne Fehler. Die eine
       Datei hat nur de-CH, die andere nur de, deshalb kam der Text aus de-CH
       nicht mit.],
      [Anpassung umgesetzt. Fehlt dem Ziel de-CH, übernimmt de den Text, ebenso
       bei fr-CH und it-CH.],

      [Unbenutzte Labels (KA09)],
      [Unused labels listet keines der verwendeten Test-Labels.],
      [Keine.],

      [Rückfragen beim Schliessen],
      [Beide Rückfragen kamen. Die beim Schliessen des Tool Window war zu lang.],
      [Gekürzt.],

      [Klassische VM (TC24, TC37)],
      [Die Extension fand das Verzeichnis auf J: selbst, die Optionsseite schlug
       es vor, und sie lud 384'429 Labels in 9,3 s. Suche, Tooltip, Sprung und das
       Nebeneinander mit dem bestehenden Tool funktionierten.],
      [Keine.],
    ),
    caption: [Ergebnisse von D4 (eigene Darstellung)]
  ) <d4_ergebnisse>
]

=== Testergebnisse

== Zielerreichung

#[
  #show figure: set align(left)
  #figure(
    table(
      align: left,
      columns: (auto, 1fr, auto, 1fr),
      table.header(
        [*ID*], [*Ziel*], [*Erreicht*], [*Bemerkung*],
      ),
      [], [], [], [],
      [], [], [], [],
      [], [], [], [],
    ),
    caption: [Zielerreichung (eigene Darstellung)]
  ) <zielerreichung>
]

== Empfehlungen
