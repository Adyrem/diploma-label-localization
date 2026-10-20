#import "../helpers.typ": todo

= Realisierung <realisierung>

== Programmierumgebung und Richtlinien

=== Programmierumgebung

Entwickelt wurde auf dem privaten Gerät, einer Windows-VM ohne Dynamics 365.
Geprüft wurde dort in der experimentellen Instanz von Visual Studio und in vier
Durchgängen auf der Testumgebung, siehe @testprotokoll.

#[
  #show figure: set align(left)
  #set text(size: 10pt)
  #figure(
    table(
      align: left,
      columns: (1fr, auto, 1.4fr),
      table.header(
        [*Tool / Technologie*], [*Version*], [*Verwendungszweck*],
      ),
      [Visual Studio Community 2026], [18.9.2],
      [Entwicklung, Build und experimentelle Instanz],
      [.NET Framework], [4.8], [Target Framework der Extension und der Unit Tests],
      [.NET Standard], [2.0], [Target Framework der Kernlogik],
      [VisualStudio.Extensibility SDK], [17.14.40608],
      [Commands, Tool Window mit Remote UI, Listener für geöffnete Dokumente],
      [Visual Studio SDK], [17.14.40265],
      [AsyncPackage, Optionsseite, MEF-Teile für den Editor, Auswahl im Designer],
      [VSSDK Build Tools], [18.9.820], [Build des VSIX-Pakets],
      [System.Reflection.Metadata], [8.0.1],
      [Lesen kompilierter Ressourcen, ohne die Assembly zu laden],
      [xUnit], [2.9.3], [Unit Tests],
      [Windows PowerShell], [5.1],
      [Skripte für Messungen auf der Testumgebung und für den Vergleich mit dem
       bestehenden Tool],
      [BE-LabelEditor], [1.7], [Vergleich der Suchergebnisse (TC04) und Betrieb
       nebeneinander (TC24). Die aktuelle Version bei BE-terna ist 1.8.3.],
      [DeepL API Free], [--], [Übersetzungsdienst für die Tests],
      [Git und GitHub], [--], [Versionsverwaltung, öffentliches Repository],
    ),
    caption: [Programmierumgebung (eigene Darstellung)]
  ) <programmierumgebung>
]

=== Programmierrichtlinien

Die meisten Richtlinien prüft der Build selbst.

- Gemeinsame Einstellungen aller Projekte stehen in `Directory.Build.props`. Jede
  Warnung bricht den Build ab, und öffentliche Typen und Member brauchen eine
  XML-Dokumentation (NFA05). Ausgenommen von der Dokumentationspflicht ist nur das
  Testprojekt.
- Nullable Reference Types sind eingeschaltet, der Build ist deterministisch.
- Die Kernlogik referenziert Visual Studio nicht. Alles, was Visual Studio braucht,
  liegt in der Extension (Z8, NFA03).
- Jeder Einstiegspunkt läuft durch die Error Boundary, keine Exception erreicht
  Visual Studio (NFA04).
- Fehler beim Parsen von Eingaben sind der Normalfall und kommen als Rückgabewert
  zurück, nach dem Try-Parse Pattern @ms-try-parse. Exceptions bleiben echten
  Fehlern vorbehalten.
- Namen, Kommentare und Meldungen im Code sind englisch, wie die Oberfläche.
- Im Repository liegen weder Code des bestehenden Tools noch Daten des Arbeitgebers.
  Die Testdaten sind synthetisch (TC40).

== Softwareaufbau

=== Projektstruktur

Die Solution hat die drei Projekte aus @architektur.

#[
  #show figure: set align(left)
  #show figure: set block(breakable: false)
  #set text(size: 10pt)
  #figure(
    table(
      align: left,
      columns: (auto, auto, 1fr, auto),
      table.header(
        [*Projekt*], [*Target Framework*], [*Inhalt*], [*Umfang*],
      ),
      [`BE.LabelExtension.Core`], [.NET Standard 2.0],
      [Kernlogik ohne Abhängigkeit zu Visual Studio], [59 Dateien,\ 6'819 Zeilen],
      [`BE.LabelExtension`], [.NET Framework 4.8],
      [Extension mit Commands, Tool Window, Editor und Optionsseite, ein
       VSIX-Paket], [39 Dateien,\ 6'788 Zeilen],
      [`BE.LabelExtension.Tests`], [.NET Framework 4.8],
      [Unit Tests der Kernlogik], [32 Dateien,\ 4'296 Zeilen],
    ),
    caption: [Projekte der Solution, Umfang in Zeilen C\# (eigene Darstellung)]
  ) <projekte>
]

Das Klassenmodell aus @klassen_laden und @klassen_bearbeiten hat gehalten. Seine
Klassen gibt es alle, in der Realisierung kamen Klassen dazu und einige Signaturen
änderten sich.

- `ILabelSource.Load` liefert alle Label-Dateien eines Package-Verzeichnisses, auch
  solche in Sprachen, die nicht geladen sind. Ohne sie wüsste FA02 nicht, in welchen
  Sprachen ein neues Label angelegt werden kann.
- Eine `Translation` kennt ihre Label-Datei, weil eine Übersetzung in einer anderen
  Datei liegen kann als das Label. Das kommt vor, wenn ein anderes Model die
  Label-Datei mit einer Erweiterung um Übersetzungen ergänzt, wie es auf der
  Testumgebung eine kompilierte `_Extension`-Datei tut. Ob eine Übersetzung
  schreibgeschützt ist, hängt deshalb an ihrer Datei.
- `FindModelFor` ist statisch, `LabelStore` ruft es auf.
- Neu sind Klassen für den Inhalt einer Label-Datei, das Lesen kompilierter
  Ressourcen, den Descriptor der Models und die Metadaten-Konfiguration.

=== Kernlogik

#[
  #show figure: set align(left)
  #set text(size: 10pt)
  #figure(
    table(
      align: left,
      columns: (auto, 1fr),
      table.header(
        [*Ordner*], [*Inhalt*],
      ),
      [`Labels`], [`LabelId`, `Label`, `Translation` und `LabelFile`.
       `LabelFileFormat` liest und schreibt `.label.txt`, `LabelKeyGenerator` erzeugt
       neue IDs.],
      [`Models`], [`ModelDiscovery` findet Package-Verzeichnisse und Models,
       `ModelAccessRule` entscheidet über den Schreibschutz.],
      [`Sources`], [Die Quellen hinter `ILabelSource` für `.label.txt` und für
       kompilierte Ressourcen.],
      [`Store`], [`LabelStore` hält die geladenen Labels. `LabelChanges` legt an,
       ändert, löscht und speichert, `LabelOperations` kopiert, verschiebt und
       ersetzt samt Referenzen. `LabelFileWatcher` meldet Änderungen von aussen.],
      [`Search`], [`LabelSearch` mit Suchmodi und Ranking.],
      [`Usages`], [Verwendungssuche, unbenutzte Labels, Umstellen von Referenzen.],
      [`Elements`], [Lesen und Schreiben in den XML-Dateien der Elemente, also der
       Klassen, Tables, Forms und übrigen Objekte eines Models.],
      [`TranslationServices`], [`ITranslationService`, DeepL und das Finden der
       Dienste.],
      [`Display`], [Der Text für Tooltip und Inline-Anzeige.],
      [`Diagnostics`, `Files`], [Error Boundary, Meldungen, lange Pfade und Dateien
       in ihrer Kodierung.],
    ),
    caption: [Ordner der Kernlogik (eigene Darstellung)]
  ) <kernlogik_ordner>
]

Das Laden folgt @sequenz_laden. Es läuft im Hintergrund und beginnt, sobald das
Tool Window oder die erste `.xpp`-Datei offen ist, nicht schon beim Start von Visual
Studio. `ModelDiscovery` liest die Package-Verzeichnisse
aus der Metadaten-Konfiguration oder sucht auf der klassischen Entwicklungs-VM das
Standardverzeichnis, zuerst auf K:, dann auf den übrigen Laufwerken. Aus dem
Descriptor jedes Models liest sie Layer und Locked, `ModelAccessRule` macht daraus
den Schreibschutz. Die beiden Quellen lesen danach die Label-Dateien und die
kompilierten Ressourcen. Gibt es zu einer Label-Datei und Sprache eine
`.label.txt`, liest die Extension die Ressource gar nicht, das spart auf der
Testumgebung rund 4000 Dateien. Am Ende führt `LabelStore` alles zusammen und
sortiert die Labels nach ID. IDs mit anderen Zeichen als Buchstaben, Ziffern und
Unterstrich lädt die Extension wie das bestehende Tool, denn Microsoft liefert einige
solche Labels aus (D2). Im Code erkennt sie nur IDs ohne solche Zeichen.

Speichern schreibt nur die geänderten Labels einer Datei. Die Datei entsteht dabei
als temporäre Datei daneben und ersetzt dann das Original, ein Abbruch hinterlässt
also nie eine halbe Label-Datei. Die Kodierung und die Zeilenenden der Datei bleiben,
wie sie waren. Während die Extension selbst schreibt, schweigt `LabelFileWatcher`,
damit nur Änderungen von aussen das Neuladen anbieten (FA15).

Neue IDs bestehen aus `L` und 16 hexadezimalen Ziffern aus einem kryptografischen
Zufallsgenerator, etwa `@BDM1:L8583DF5058DD2AD2`. Sollte es zu einer Kollision mit einer bestehenden ID kommen, wird eine weitere ID generiert.

=== Suche und Ranking

Suchmodi und Ranking folgen dem bestehenden Tool. Die Extension kennt acht Modi.

- Exact match und Substring vergleichen Text, ID und Kommentar mit dem Begriff, je
  mit oder ohne Beachtung der Gross- und Kleinschreibung. Geprüft wird in dieser
  Reihenfolge, und das erste Feld mit dem Begriff gibt den Wert. Ein Treffer im Text
  zählt 2, einer nur in der ID 3 und einer nur im Kommentar 1. Labels, deren ID den
  Begriff enthält, ihr Text aber nicht, stehen so zuoberst.
- Anything like that zerlegt den Begriff in Wörter und findet jedes Label, das eines
  davon enthält. Je grösser der Teil eines Felds ist, den die gesuchten Wörter
  ausmachen, desto höher der Wert. Ein kurzer Text, der fast nur aus den Wörtern
  besteht, steht also vor einem langen, der sie nebenbei enthält. Es zählen nur ID
  und Text.
- Match word sucht den Begriff als ganzes Wort in Text und Kommentar oder als ganze
  ID.
- Label id findet genau das Label mit dieser ID.

Ein Label erhält den höchsten Wert über alle Sprachen. Die Trefferliste zeigt die
1000 Labels mit dem höchsten Wert, absteigend sortiert und bei Gleichstand nach ID,
die Statuszeile nennt die Gesamtzahl. Ein Klick auf einen Spaltenkopf sortiert diese
Treffer alphabetisch nach der Spalte, ein zweiter absteigend und ein dritter wieder
nach dem Wert. Gesucht wird 250 ms nach dem letzten Tastendruck oder sofort mit
Enter.

TC04 vergleicht die Extension mit Ergebnissen, die ein Skript über UI Automation
aus dem bestehenden Tool gelesen hat. Von 304 Suchen mit 38 Begriffen in allen acht
Modi liefern 303 dieselben Treffer in derselben Reihenfolge. Die eine Suche fällt
weg, weil das bestehende Tool bei Match word den Begriff als Regex auswertet, siehe
@abweichungen_realisierung.

In D1 brauchte Anything like that ohne Beachtung der Gross- und Kleinschreibung
über 392'858 Labels 623 bis 656 ms und verfehlte NFA02. Vier Änderungen brachten
die längste Suche in D4 auf 253 ms.

- Die Übersetzungen eines Labels liegen in einem Array statt in einem Dictionary.
- Gleiche Texte und Kommentare teilen sich eine Instanz.
- Der Vergleich ohne Gross- und Kleinschreibung läuft über eine eigene Tabelle aller
  65'536 Zeichen. `IndexOf` mit `OrdinalIgnoreCase` war unter .NET Framework der
  langsamste Teil der Suche.
- Match word prüft vor der Regex, ob der Begriff überhaupt vorkommt.

=== Anbindung an Visual Studio

Die Extension verbindet VisualStudio.Extensibility im Prozess von Visual Studio mit
Teilen aus dem klassischen Visual Studio SDK, wie in @architektur festgelegt.

#[
  #show figure: set align(left)
  #set text(size: 10pt)
  #figure(
    table(
      align: left,
      columns: (auto, auto, 1fr),
      table.header(
        [*Teil*], [*Technik*], [*Aufgabe*],
      ),
      [Commands], [VisualStudio.Extensibility],
      [Menü Extensions, Kontextmenü des X++-Editors, Shortcuts],
      [Tool Window], [Remote UI], [Suche, Trefferliste, Detailansicht, References,
       Bereich zur gewählten Node im Designer],
      [Laden], [VisualStudio.Extensibility], [Ein Listener startet das Laden, sobald
       die erste `.xpp`-Datei offen ist.],
      [Tooltip], [MEF, `IAsyncQuickInfoSource`], [Übersetzungen unter dem Eintrag
       der Developer Tools],
      [Inline-Anzeige], [MEF, Tagger und Adornment Layer], [Übersetzungen über der
       Zeile, der Tagger reserviert dafür Platz],
      [AsyncPackage], [Visual Studio SDK], [Optionsseite, Rückfrage beim Beenden,
       Auswahl im Designer],
      [Elemente], [Visual Studio SDK], [Sprung an Fundstellen, ungespeicherte
       Elemente erkennen],
      [Output Window], [Visual Studio SDK], [Bereich BE-LabelExtension (FA17)],
      [Probes], [VisualStudio.Extensibility], [Nur im Debug-Build, Diagnostics und
       Testing auf der Testumgebung],
    ),
    caption: [Teile der Extension (eigene Darstellung)]
  ) <extension_teile>
]

Das Tool Window nutzt Remote UI wie die übrigen Teile aus VisualStudio.Extensibility.
Die Extension schickt die XAML an Visual Studio, das sie selbst darstellt und dabei
die Farben des gewählten Themes verwendet @ms-remote-ui. Ein Fallback auf WPF über
das Visual Studio SDK war nicht nötig. Für die Optionsseite und die Rückfrage beim
Beenden von Visual Studio fand sich in VisualStudio.Extensibility kein stabiler Weg.
Dafür liegt im selben VSIX ein AsyncPackage aus dem Visual Studio SDK.

Label-IDs im Editor erkennt die Extension über die Classifications der Developer
Tools, `"X++ Modern Label"` und `"X++ Legacy Label"`. Fehlen sie, erkennt eine Regex
die IDs im Text, wie in R07 vorgesehen. Auf dem privaten Gerät gibt es keinen
X++-Editor. Der Debug-Build hängt Tooltip und Inline-Anzeige deshalb zusätzlich an
`.xpp`-Dateien, die als Text offen sind.

Ein Klick auf eine Fundstelle im Code öffnet die `.xpp`-Datei des Elements im Ordner
`XppSource` und setzt den Cursor auf dieselbe Verwendung. Eine Fundstelle in einer Eigenschaft öffnet das Element im Designer.

== GUI-Implementierung

Die Oberfläche folgt dem GUI-Design aus @gui_design. Die Bilder stammen aus der
experimentellen Instanz mit den synthetischen Testdaten. Der Editor darin ist der
Ersatz des Debug-Builds für den X++-Editor, deshalb fehlen über dem Tooltip der
Eintrag und über den Methoden die Referenzanzeige der Developer Tools.

#figure(
  image("../screenshots/extension-toolwindow.png", width: 100%),
  caption: [Tool Window mit Trefferliste, Detailansicht und References (eigene
            Darstellung)]
) <gui_toolwindow_real>

@gui_toolwindow_real zeigt das Tool Window übereinander aufgebaut wie im Wireframe
@gui_toolwindow. Labels schreibgeschützter Models tragen ein Schloss, fehlende
Übersetzungen stehen kursiv als missing. de-CH ist gesperrt, weil die Label-Datei
diese Sprache nicht hat. Ein Klick auf einen Spaltenkopf sortiert die Trefferliste.
Gegenüber dem Wireframe hat die Toolbar zwei Buttons und eine Checkbox mehr,
Element labels für die Labels des offenen Elements (KA04), Unused labels für
unbenutzte Labels (KA09) und Side by side.

#figure(
  image("../screenshots/extension-side-by-side.png", width: 100%),
  caption: [Tool Window mit Side by side, unten angedockt (eigene Darstellung)]
) <gui_side_by_side>

Unten angedockt war das Tool Window in D2 kaum brauchbar. Side by side stellt die
Trefferliste links neben Detailansicht und References.

#figure(
  image("../screenshots/extension-neues-label-vorschlaege.png", width: 100%),
  caption: [Neues Label mit Vorschlägen des simulierten Übersetzungsdienstes
            (eigene Darstellung)]
) <gui_neues_label>

Ein neues Label erscheint ohne ID, bis der Entwickler Create klickt. Vorschläge des Übersetzungsdienstes stehen kursiv, bis er sie
ändert oder bestätigt. Im Bild stammen sie vom simulierten Dienst des Debug-Builds,
der jeder Sprache ihren Code voranstellt. Übersetzt wird aus der Ausgangssprache,
die in den Optionen für alle Labels gilt, standardmässig de. Translate fragt den
Dienst noch einmal, wenn sich der Ausgangstext geändert hat.

#figure(
  image("../screenshots/extension-inline-platzhalter.png", width: 100%),
  caption: [Inline-Anzeige über den Zeilen mit Label-IDs (eigene Darstellung)]
) <gui_inline>

#figure(
  image("../screenshots/extension-tooltip.png", width: 100%),
  caption: [Tooltip über einer Label-ID (eigene Darstellung)]
) <gui_tooltip>

Die Inline-Anzeige in @gui_inline steht über jeder Zeile mit einer Label-ID. In einem
Aufruf von `strFmt` ersetzt sie die Platzhalter durch die Argumente, hier
`{_customerAccount}` statt `%1` (KA03). Sprachen und Reihenfolge lassen sich in den
Optionen getrennt von den geladenen Sprachen wählen. Der Tooltip in @gui_tooltip
zeigt alle geladenen Sprachen.

#figure(
  image("../screenshots/extension-kontextmenue.png", width: 60%),
  caption: [Commands im Kontextmenü des Editors (eigene Darstellung)]
) <gui_kontextmenue>

Das Kontextmenü des Editors in @gui_kontextmenue enthält die drei Commands aus dem
GUI-Design mit ihren Shortcuts. Alle Shortcuts der Extension beginnen mit
Strg+Shift+Alt, gefolgt von S für Save, I für Insert, A für Save and insert, N für
New, L für Search Label, E für Open in Label Window und X für Extract to Label. Die
Shortcuts des bestehenden Tools belegt Visual Studio @ms-vs-shortcuts. Die neuen
waren in D2 frei, nur S ist im Scope `VC Image Editor` belegt. Alle lassen sich in den
Optionen von Visual Studio umbelegen. Im Designer wurde das Kontextmenü wegen technischen Limitierungen nicht erweitert, als Ersatz wird Extract im Tool Window verwendet. Wählt der Entwickler dort eine Node, zeigt das Tool Window zwischen Suche und Trefferliste ihre
Label-Eigenschaften mit Open oder Extract (KA05).

#figure(
  image("../screenshots/extension-optionen.png", width: 85%),
  caption: [Optionsseite der Extension in den Optionen von Visual Studio (eigene
            Darstellung)]
) <gui_optionen>

Die Optionsseite in @gui_optionen weicht vom Wireframe @gui_einstellungen ab. Eine
Seite im Dialog Options von Visual Studio bringt ein Property Grid mit, das die
Einstellungen nach Gruppen und darin alphabetisch ordnet. Die Einstellungen sind
dieselben, dazu kommen die beiden Optionen der Inline-Anzeige. Mehrere
Package-Verzeichnisse trägt der Entwickler im String Collection Editor von Visual
Studio ein, ein Verzeichnis pro Zeile.

== Datenzugriff und externe Anbindung

=== Datenzugriff

Die Extension braucht weder Datenbank noch Server. Sie arbeitet mit den Dateien,
die Dynamics 365 und Visual Studio ohnehin haben.

#[
  #show figure: set align(left)
  #set text(size: 10pt)
  #figure(
    table(
      align: left,
      columns: (1fr, 1.2fr, 1.2fr),
      table.header(
        [*Daten*], [*Lesen*], [*Schreiben*],
      ),
      [Label-Dateien `.label.txt`], [Eigener Parser, siehe @fachliche_regeln],
      [Nur geänderte Labels, über eine temporäre Datei, Kodierung und Zeilenenden
       bleiben],
      [Kompilierte Ressourcen `.Resources.dll`], [Direkt aus der Datei mit
       System.Reflection.Metadata, ohne die Assembly zu laden], [Nie, schreibgeschützt],
      [Descriptor der Models], [Layer und Locked], [Nie],
      [Metadaten-Konfiguration], [Package-Verzeichnisse aus der JSON-Datei in
       `XPPConfig`], [Nie],
      [XML-Dateien der Elemente], [Textsuche für Verwendungen und unbenutzte
       Labels, Labels eines Elements], [Nur die betroffene Stelle beim Umstellen
       von Referenzen, Ersetzen und Extrahieren, nur in gespeicherten Elementen],
      [`.xpp`-Dateien in `XppSource`], [Stelle für den Sprung in den Code], [Nie],
      [Einstellungen in `settings.json`], [Beim Start und nach dem Übernehmen der
       Optionen], [Beim Übernehmen der Optionen, API-Schlüssel verschlüsselt],
    ),
    caption: [Daten der Extension (eigene Darstellung)]
  ) <datenzugriff>
]

Visual Studio ist standardmässig nicht für lange Pfade ausgerichtet. Windows nimmt einen Pfad über 260 Zeichen dann nur mit dem Prefix `\\?\` an @ms-max-path. In D1 waren 18 Label-Dateien von Microsoft deshalb nicht lesbar. Seither setzt die
Extension den Prefix ab 248 Zeichen. Für Verzeichnisse liegt die Grenze 12 Zeichen
unter 260, damit noch ein Dateiname im Format 8.3 Platz hat @ms-max-path.

Die Verwendungssuche liest die XML-Dateien aller Elemente als Text und sucht darin
die Label-ID, mit denselben Mustern wie das bestehende Tool. In einem Element steht
eine ID zwischen zwei Tags, in Anführungszeichen oder in Klammern. In Reports kommt
sie zusätzlich nach einem Ausrufezeichen in Ausdrücken vor. Die Suche läuft
zuerst nacheinander über die eigenen Models und danach parallel über alle übrigen.
Die Fundstellen erscheinen alle 250 ms im Tool Window. Auf der Testumgebung sind die
eigenen Models in weniger als einer Sekunde durchsucht, alle 185 Models mit rund
199'000 Dateien dauerten in D1 24 bis 56 s.

In XML-Dateien schreibt die Extension nur, wenn das Element gespeichert ist. Sonst
nennt eine Rückfrage die ungespeicherten Elemente und bietet an, sie zuerst zu
speichern. Speichert der Designer ein Element mit ungespeicherten Änderungen, nachdem
die Datei von aussen geändert wurde, überschreibt er die Änderung von aussen (D1).
Ersetzt wird nur der Text der betroffenen Stelle, der Rest der Datei bleibt Zeichen für
Zeichen gleich.

=== Übersetzungsdienst

Jeder Übersetzungsdienst setzt `ITranslationService` um, mit Name, der Angabe, ob er
eingerichtet ist, und `TranslateAsync`. Ein Fehler kommt als Ergebnis mit Grund
zurück, nicht als Exception. Die Extension findet alle Klassen, die das Interface
umsetzen. Ein weiterer Dienst ist deshalb eine neue Klasse, an bestehenden Klassen
ändert sich nichts (NFA03, TC38).

Für DeepL schickt die Extension je Zielsprache eine Anfrage @deepl-translate, mit den
Sprachcodes aus @festlegungen. Der Schlüssel steht im Header, Schlüssel der freien
Variante gehen an `api-free.deepl.com` @deepl-auth. Antwortet DeepL nicht innerhalb
von 20 s, lehnt den Schlüssel ab oder meldet ein aufgebrauchtes Kontingent, nennen
Statuszeile und Output Window den Grund, und das neue Label behält den Ausgangstext.
Vorschläge gibt es nur für Sprachen, die die Label-Datei hat.

Den API-Schlüssel verschlüsselt die Extension mit der Data Protection API von Windows
für den angemeldeten Benutzer, nur derselbe Benutzer kann ihn wieder entschlüsseln
@ms-data-protection. Gelesen und entschlüsselt wird er erst für eine Übersetzung, in
keiner Meldung und keinem Feld steht er im Klartext (NFA06, TC23). Der Debug-Build
hat zusätzlich einen simulierten Dienst, damit sich die Vorschläge ohne Schlüssel
prüfen lassen.

== Abweichungen vom Konzept und vom bestehenden Tool

#[
  #show figure: set align(left)
  #set text(size: 9.5pt)
  #figure(
    table(
      align: left,
      columns: (1fr, 1fr),
      table.header(
        [*Abweichung*], [*Grund*],
      ),
      table.cell(colspan: 2)[*Gegenüber dem Konzept*],
      [Signaturen und zusätzliche Klassen im Klassenmodell, siehe
       @projekte und die Liste darunter.],
      [Was die Realisierung brauchte, etwa die Label-Dateien nicht geladener
       Sprachen für FA02.],

      [Die Optionsseite ist ein Property Grid statt der Seite aus
       @gui_einstellungen und hat keinen Button Apply.],
      [Eine Seite im Dialog Options von Visual Studio bringt das Property Grid und
       nur OK und Cancel mit.],

      [Der Sprung in eine Eigenschaft öffnet das Element, wählt aber die Node
       nicht.],
      [Die Developer Tools bieten kein Command, das eine Node wählt (D1). Das
       Konzept lässt diesen Fallback zu.],

      [Ein Button Translate beim neuen Label fragt den Übersetzungsdienst noch
       einmal.],
      [Ohne ihn bliebe nach einer Änderung des Ausgangstexts nur Cancel und New.],

      [Kopieren und Verschieben übernehmen den Text aus de-CH in de, wenn dem
       Ziel de-CH fehlt. FA03 sieht nur gemeinsame Sprachen vor.],
      [Anpassung aus D4. Die eine Label-Datei hatte nur de-CH, die andere nur de.],

      [Side by side, sortierbare Spalten, eigene Sprachen und Anzahl der
       Inline-Anzeige, Rückfrage beim Schliessen des Tool Window, Wiederherstellen
       gelöschter Labels, Delete-Taste, Search Label am Wort.],
      [Anpassungen aus D2 bis D4.],

      table.cell(colspan: 2)[*Gegenüber dem bestehenden Tool*],
      [Speichern schreibt nur geänderte Labels und erreicht auch Sprachen, die
       nicht geladen sind.],
      [FA03 verlangt Änderungen in allen Sprachen der Label-Datei.],

      [Match word wertet den Begriff als Text, nicht als Regex.],
      [Im bestehenden Tool findet "Kunde." auch "Kunden".],

      [Eine Label-Datei mit Zeilen, die weder ein Label noch ein Kommentar sind, wird mit
       Warnung übersprungen.],
      [Das bestehende Tool lädt sie und verliert die Zeilen beim nächsten
       Speichern.],

      [Kompilierte Ressourcen nur unter `Resources`, nicht im ganzen
       Package-Verzeichnis.],
      [In den Ordnern `bin` liegen Assemblies von .NET, welche für das Tool unnötig sind.],

      [Die Kodierung jeder Label-Datei bleibt beim Schreiben, wie sie war.],
      [Das bestehende Tool schreibt alle Dateien in derselben Form und ändert so
       auch Zeilen, die niemand bearbeitet hat.],
    ),
    caption: [Abweichungen vom Konzept und vom bestehenden Tool (eigene Darstellung)]
  ) <abweichungen_realisierung>
]

== Deployment und Installation

Der Code Freeze war am 19.10.2026, fünf Tage vor dem Plan. Der Release-Build aus
diesem Stand ist das Paket `BE.LabelExtension-1.0.0.vsix` in der Version 1.0.0. Im
Repository trägt der Stand das Tag `v1.0.0`, die Assemblies im Paket nennen den Commit
in ihrer Produktversion. Das Paket hat die SHA-256-Prüfsumme
`EBFCC8F3787BC6815CED43287C4B8871B1F37C50E20CC7716A326A18549E3E3B`.

Das Paket enthält die Lizenz des Codes, der VSIX Installer zeigt einen Link darauf.
Install bleibt der einzige Schritt (OA01). Im öffentlichen Repository liegt das Paket
nicht. Das SDK packt Bibliotheken von VisualStudio.Extensibility hinein, deren Lizenz
das Weitergeben und Veröffentlichen untersagt @ms-extensibility-license. Das Paket
liegt der Abgabe bei und wird bei BE-terna intern verteilt.

#[
  #show figure: set align(left)
  #show figure: set block(breakable: false)
  #set text(size: 10pt)
  #figure(
    table(
      align: left,
      columns: (auto, 1fr),
      table.header(
        [*Voraussetzung*], [*Version und Zweck*],
      ),
      [Visual Studio], [2026 (18.x) oder 2022 ab 17.14, jede Edition, 64 Bit],
      [.NET Framework], [4.8],
      [Developer Tools von Dynamics 365], [Für Tooltip, Inline-Anzeige, Sprung an
       Fundstellen und den Bereich zum Designer. Suche und Bearbeitung brauchen nur
       die Package-Verzeichnisse.],
      [API-Schlüssel von DeepL], [Nur für Übersetzungsvorschläge],
    ),
    caption: [Voraussetzungen der Extension (eigene Darstellung)]
  ) <voraussetzungen>
]

Installiert wird in drei Schritten, siehe auch @einfuehrung.

+ Visual Studio schliessen.
+ Doppelklick auf das VSIX-Paket, Install, danach Close.
+ In Visual Studio Extensions > BE-LabelExtension Window öffnen und beim ersten
  Mal in Settings die Sprachen prüfen. Auf der Unified Developer Experience nimmt die
  Extension die zuletzt geänderte Metadaten-Konfiguration, auf der klassischen
  Entwicklungs-VM schlägt die Optionsseite das gefundene Package-Verzeichnis vor.

Ein Update ist ein neues Paket mit höherer Version, das auf dieselbe Weise
installiert wird. Einstellungen und API-Schlüssel bleiben dabei erhalten.
Deinstalliert wird über Extensions > Manage Extensions. Die Label-Dateien bleiben
unverändert, die Einstellungen in `%LOCALAPPDATA%\BE.LabelExtension` löscht nur, wer
sie nicht mehr braucht.

Ohne Dynamics 365 lässt sich die Extension mit dem synthetischen Datensatz aus dem
Repository ausprobieren.

+ Den Ordner `source/BE.LabelExtension.Tests/TestData/PackagesLocalDirectory` an
  einen eigenen Ort kopieren, weil die Extension in die Label-Dateien schreibt.
+ Das VSIX-Paket installieren und in Visual Studio Extensions > BE-LabelExtension
  Window öffnen.
+ Settings öffnen, bei Additional package directories auf "..." klicken, im String
  Collection Editor den Pfad des kopierten Ordners `PackagesLocalDirectory` eintragen
  und beide Dialoge mit OK schliessen. Die Extension lädt sofort neu, 57 Labels aus 6
  Label-Dateien in 4 Models.

So geprüft am 20.10.2026 mit dem Release-Build in der experimentellen Instanz.
Suche, Bearbeiten, Anlegen, Kopieren und Verschieben, die Verwendungssuche und die
unbenutzten Labels funktionieren. Go to öffnet die XML-Datei des Elements als Text an
der Fundstelle, die Statuszeile verweist dabei unnötig auf den Application Explorer.
Tooltip, Inline-Anzeige, der Sprung in den X++-Editor und der Bereich zum Designer
brauchen die Developer Tools. Der Debug-Build bietet dafür einen Ersatz, den
`source/README.md` beschreibt.

Geprüft ist die Installation auf der Unified Developer Experience mit Visual Studio
2026 in D1 bis D4 und auf der klassischen Entwicklungs-VM mit Visual Studio 2026 in
D2 und D3 sowie mit Visual Studio 2022 in D1 und D4, in D4 mit dem Release-Build. Für
die Weiterentwicklung beschreibt `source/README.md` Aufbau, Build und
Erweiterungspunkte (OA02).

== Testprotokoll <testprotokoll>

=== Unit Tests

Beim Code Freeze liefen 394 Unit Tests in 30 Testklassen ohne Fehler, im Debug- und
im Release-Build unter .NET Framework 4.8 mit xUnit. Sie arbeiten auf einer Kopie
eines synthetischen Datensatzes mit vier Models. Zwei sind beschreibbar, eines ist
gesperrt und eines liegt in einem Layer, der schreibgeschützt ist. Die Label-Dateien
enthalten beide Formen der Label-ID, Kommentare, leere Texte, fehlende Übersetzungen
und eine `_Extension`-Datei.

#[
  #show figure: set align(left)
  #set text(size: 10pt)
  #figure(
    table(
      align: left,
      columns: (auto, 1fr),
      table.header(
        [*Testfall*], [*Testklassen*],
      ),
      [TC01], [`LabelFileFormatTests`],
      [TC02], [`LabelIdTests`],
      [TC03], [`LabelKeyGeneratorTests`],
      [TC04], [`ExistingToolComparisonTests`, `LabelSearchTests`, `CharFoldingTests`],
      [TC05], [`LabelReferencesTests`, `LabelOperationsTests`],
      [TC06], [`LabelChangesTests`, `LabelOperationsTests`],
      [TC07], [`LabelChangesTests`, `LabelOperationsTests`, `ModelAccessRuleTests`],
      [TC08], [`LabelStoreTests`],
      [TC09], [`LabelFileWatcherTests`, `LabelStoreTests`],
      [TC10], [`LabelStoreTests`, `ErrorBoundaryTests`],
      [TC11], [`DeepLTranslationServiceTests`],
      [TC12], [`LabelStoreTests`],
      [TC13], [`ModelDiscoveryTests`],
      [TC14], [`ModelDiscoveryTests`],
    ),
    caption: [Unit Tests der Testfälle TC01 bis TC14 (eigene Darstellung)]
  ) <unit_tests>
]

TC04 vergleicht jede Suche mit dem Ergebnis des bestehenden Tools auf denselben
Testdaten, erfasst mit dem Skript `Capture-ExistingToolResults.ps1`. TC11 prüft die
Anfragen an DeepL mit simulierten Antworten, auch mit den Fehlern 403, 429, 456 und
500.

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

#[
  #show figure: set align(left)
  #set text(size: 9.5pt)
  #figure(
    table(
      align: left,
      columns: (auto, 1fr, auto, auto),
      table.header(
        [*ID*], [*Testfall*], [*Ergebnis*], [*Nachweis*],
      ),
      table.cell(colspan: 4)[*Unit Tests der Kernlogik*],
      [TC01], [Label-Datei lesen und unverändert schreiben], [Bestanden], [Unit Test],
      [TC02], [Label-IDs beider Formen parsen und finden], [Bestanden], [Unit Test],
      [TC03], [10'000 neue Label-IDs erzeugen], [Bestanden], [Unit Test],
      [TC04], [Suchmodi gegen das bestehende Tool], [Bestanden], [Unit Test],
      [TC05], [Verwendungen suchen und ersetzen], [Bestanden], [Unit Test],
      [TC06], [Kopieren und Verschieben mit Referenzen], [Bestanden], [Unit Test],
      [TC07], [Schreibgeschützte Labels ändern, löschen, kopieren], [Bestanden],
       [Unit Test],
      [TC08], [Labels aus kompilierten Ressourcen lesen], [Bestanden], [Unit Test],
      [TC09], [Änderung von aussen bei ungespeichertem Label], [Bestanden],
       [Unit Test],
      [TC10], [Laden mit beschädigten, fehlenden und gesperrten Dateien],
       [Bestanden], [Unit Test],
      [TC11], [Simulierter Übersetzungsdienst], [Bestanden], [Unit Test],
      [TC12], [Sprache aus Ordner und Dateiname], [Bestanden], [Unit Test],
      [TC13], [Model zu XML- und `.xpp`-Datei finden], [Bestanden], [Unit Test],
      [TC14], [Package-Verzeichnisse mit und ohne Konfiguration], [Bestanden],
       [Unit Test],
      table.cell(colspan: 4)[*Systemtests auf dem privaten Gerät*],
      [TC15], [Installation und Tool Window], [Bestanden], [Privat, D1],
      [TC16], [Suche in jedem Suchmodus], [Bestanden], [Privat],
      [TC17], [Ändern und Löschen], [Bestanden], [Privat],
      [TC18], [Neues Label per Shortcut], [Bestanden], [Privat],
      [TC19], [Label-ID einfügen], [Bestanden], [Privat],
      [TC20], [Shortcuts auslösen und umbelegen], [Bestanden], [Privat, D2],
      [TC21], [Sprachen und Metadaten-Konfiguration ändern], [Bestanden], [Privat],
      [TC22], [Fehler auslösen], [Bestanden], [Privat, D1],
      [TC23], [API-Schlüssel nirgends im Klartext], [Bestanden], [Privat, D3],
      [TC24], [Bestehendes Tool und Extension nebeneinander], [Bestanden],
       [Privat, D4],
      table.cell(colspan: 4)[*Systemtests auf der Testumgebung*],
      [TC25], [Laden neben den Developer Tools], [Bestanden], [D1, D4],
      [TC26], [Tooltip beider Label-Formen], [Bestanden], [D2 bis D4],
      [TC27], [Inline-Anzeige ein- und ausschalten], [Bestanden], [D2, D4],
      [TC28], [Suche aus dem Editor, Label im Tool Window öffnen], [Bestanden],
       [D2, D4],
      [TC29], [Verwendungen suchen und anspringen], [Bestanden], [D2 bis D4],
      [TC30], [Verschieben bei offenen Elementen], [Bestanden], [D2, D4],
      [TC31], [Extraktion im X++-Editor], [Bestanden], [D3, D4],
      [TC32], [Extraktion im Designer], [Bestanden], [D3, D4],
      [TC33], [Übersetzungsdienst mit und ohne Schlüssel], [Bestanden], [D3, D4],
      [TC34], [Kompilieren nach Änderungen], [Bestanden], [D3, D4],
      [TC35], [Suchdauer und Laden], [Bestanden], [D4],
      [TC36], [Systemtests unter Visual Studio 2022], [Teilweise], [D1, D4],
      [TC37], [Klassische Entwicklungs-VM], [Bestanden], [D2 bis D4],
      table.cell(colspan: 4)[*Reviews*],
      [TC38], [Kernlogik ohne Visual Studio, zweiter Übersetzungsdienst],
       [Bestanden], [Review],
      [TC39], [Build ohne Warnungen, öffentliche Klassen dokumentiert],
       [Bestanden], [Review],
      [TC40], [Repository ohne fremden Code und ohne Daten des Arbeitgebers],
       [Bestanden], [Review],
    ),
    caption: [Ergebnisse aller Testfälle (eigene Darstellung)]
  ) <testergebnisse>
]

Bis auf TC36 haben alle Testfälle bestanden. TC29 bestand in D2 auf der Unified
Developer Experience nicht, weil die Extension die `.xpp`-Datei im falschen Ordner
suchte. Nach der Korrektur bestand er in D3 und D4. TC26 und TC28 hatten in D2
Befunde, die D3 nachprüfte. Die Systemtests TC15 bis TC24 liefen am 19.10.2026 auf
dem Stand des Code Freeze noch einmal in der experimentellen Instanz.

#[
  #show figure: set align(left)
  #figure(
    table(
      columns: (auto, 1fr),
      [*ID*], [TC36],
      [*Beschreibung*],
      [Alle Systemtests unter Visual Studio 2022 wiederholen.],
      [*Vorgehen*],
      [Die klassische Entwicklungs-VM lief in D1 und D4 mit Visual Studio 2022
       17.14. In D1 mit dem Debug-Build, in D4 mit dem Release-Build. Die übrigen
       Systemtests liefen nur unter Visual Studio 2026.],
      [*Erwartetes Ergebnis*],
      [Das Testprotokoll hält das Ergebnis jedes Testfalls fest.],
      [*Tatsächliches Ergebnis*],
      [Unter Visual Studio 2022 liessen sich Debug- und Release-Build per
       Doppelklick installieren. In D4 bestanden TC24, TC25 und TC37 auf echten
       Daten. Für die übrigen Systemtests gibt es unter Visual Studio 2022 kein
       Ergebnis.],
      [*Status*],
      [Teilweise. Visual Studio 2022 wurde während des Projekts optional, die
       Zielplattform ist Visual Studio 2026.],
      [*Referenz*], [NFA01, D1, D4],
    ),
    caption: [Testergebnis TC36 (eigene Darstellung)]
  ) <tc_36>
]

== Zielerreichung

#[
  #show figure: set align(left)
  #set text(size: 10pt)
  #figure(
    table(
      align: left,
      columns: (auto, 1fr, auto, 1.4fr),
      table.header(
        [*ID*], [*Ziel*], [*Erreicht*], [*Bemerkung*],
      ),
      [Z1], [Feature Parity zum bestehenden Tool], [Ja],
      [Alle Funktionen aus @ist_funktionen, TC04 mit denselben Treffern. Die
       Abweichungen stehen in @abweichungen_realisierung.],
      [Z2], [Übersetzungen im Code einsehbar], [Ja],
      [Tooltip und Inline-Anzeige mit allen konfigurierten Sprachen (TC26, TC27).],
      [Z3], [Suche aus dem Editor], [Ja], [Search Label im Kontextmenü und per
       Shortcut (TC28).],
      [Z4], [Bearbeiten aus dem Code heraus], [Ja], [Open in Label Window öffnet das
       Label in der Detailansicht (TC28).],
      [Z5], [Verwendungssuche in der IDE], [Ja], [Fundstellen mit Model, Datei, Zeile
       und Spalte, Sprung in Editor und Designer (TC29, ab D3).],
      [Z6], [Extraktion hardcodierter Texte], [Ja], [Im X++-Editor und im Designer
       (TC31, TC32).],
      [Z7], [Automatische Übersetzung], [Ja], [Vorschläge von DeepL, vor dem
       Speichern änderbar (TC33).],
      [Z8], [Erweiterbare Architektur], [Ja], [Kernlogik ohne Visual Studio, 394 Unit
       Tests, zweiter Übersetzungsdienst ohne Änderung bestehender Klassen (TC38).],
    ),
    caption: [Zielerreichung (eigene Darstellung)]
  ) <zielerreichung>
]

Alle Ziele aus @projektziele sind erreicht, auch die der Stufen 2 und 3. Damit ist
das Erfolgskriterium aus @aufgabenstellung erfüllt. Die Funktionen des bestehenden
Tools sind übernommen (Z1) und um die IDE-Funktionen erweitert (Z2 bis Z7).

#[
  #show figure: set align(left)
  #set text(size: 10pt)
  #figure(
    table(
      align: left,
      columns: (auto, auto, 1.6fr),
      table.header(
        [*Anforderung*], [*Erfüllt*], [*Nachweis*],
      ),
      [NFA01 Kompatibilität], [Teilweise], [Unified Developer Experience und
       klassische Entwicklungs-VM mit Visual Studio 2026 (TC25, TC37). Unter Visual
       Studio 2022 nur Installation, TC24, TC25 und TC37, siehe @tc_36.],
      [NFA02 Performance], [Ja], [Längste Suche 253 ms, das Laden blockiert die
       Oberfläche nicht (TC35).],
      [NFA03 Erweiterbarkeit], [Ja], [Kernlogik ohne Visual Studio, zweiter
       Übersetzungsdienst ohne Änderung bestehender Klassen (TC38).],
      [NFA04 Stabilität], [Ja], [Error Boundary an jedem Einstiegspunkt, Visual
       Studio lief in allen Durchgängen weiter (TC10, TC22, TC25).],
      [NFA05 Wartbarkeit], [Ja], [Build ohne Warnungen, öffentliche Typen
       dokumentiert (TC39).],
      [NFA06 Sicherheit], [Ja], [API-Schlüssel nirgends im Klartext (TC23).],
      [OA01 Bereitstellung], [Ja], [Ein VSIX-Paket, Installation per Doppelklick in
       D1 bis D4 (TC15).],
      [OA02 Dokumentation], [Ja], [`source/README.md` mit Aufbau, Build und
       Erweiterungspunkten (TC40).],
      [OA03 Quellcodeablage], [Ja], [Kein fremder Code, synthetische Testdaten
       (TC40).],
      [OA04 Ablösung], [Offen], [Die Voraussetzung, alle Anforderungen zu Z1, ist
       erfüllt. Über den Umstieg entscheidet BE-terna nach Projektabschluss.],
    ),
    caption: [Erfüllung der nicht-funktionalen und organisatorischen Anforderungen
              (eigene Darstellung)]
  ) <zielerreichung_nfa_oa>
]
 Von den
zehn Kann-Anforderungen sind fünf umgesetzt, KA03, KA04, KA05, KA08 und KA09. Sie
kamen nach AP4.1 dazu, weil die Realisierung vor dem Plan lag.

== Empfehlungen

- Pilotphase mit einzelnen Entwicklern, wie in @einfuehrung vorgesehen. Das
  bestehende Tool kann daneben weiterlaufen, TC24 zeigt, dass beide dieselben Dateien
  bearbeiten können. Vorher den Release-Build einmal auf der Unified Developer
  Experience mit Visual Studio 2026 prüfen, auf der Testumgebung lief er nur unter
  Visual Studio 2022.
- Das Paket darf nicht öffentlich liegen. BE-terna braucht dafür einen internen
  Ablageort, von dem Entwickler neue Versionen holen.
- Welcher Übersetzungsdienst im Betrieb läuft und wie die API-Schlüssel verteilt
  werden, muss BE-terna festlegen. Klassische Entwicklungs-VMs nutzen oft mehrere
  Personen mit demselben Windows-Benutzer. Die Data Protection API schützt den
  Schlüssel dann gegen andere Benutzer, nicht gegen die anderen Personen auf derselben
  VM. Soll jede Person einen eigenen Schlüssel haben, muss die Ablage des Schlüssels
  umgebaut werden.
- Die Verwendungssuche über alle Models dauert bis knapp eine Minute. Stört das im
  Alltag, ist die Cross-Reference-Datenbank aus E4 der nächste Schritt.
- Die Labels belegen im Prozess von Visual Studio rund 0,9 GB. Wird der Speicher
  knapp, ist ein eigener Index die Variante C aus E2.
- Die aktive Metadaten-Konfiguration erkennt die Extension nicht, sie nimmt die
  zuletzt geänderte. Wer oft zwischen Konfigurationen wechselt, muss sie in den
  Optionen wählen. Eine automatische Erkennung wäre eine Verbesserung.
- Offen sind KA01 und KA02 für fehlende Übersetzungen im Code, KA06 für die Suche im
  Code nach dem Labeltext und KA07 für die Extraktion als Vorschlag im Editor. Sie
  sollten nach dem Projekt mit den Erfahrungen aus der Pilotphase neu beurteilt
  werden, bevor jemand sie umsetzt. KA10,
  GitHub Copilot als Übersetzungsdienst, war für dieses Projekt zu aufwändig und
  bleibt zurückgestellt. Nach dem Projekt lässt sie sich noch einmal prüfen.
- Nutzt BE-terna Visual Studio 2022 weiter, sollten die Systemtests dort noch laufen,
  siehe @tc_36.
