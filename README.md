# Diplomarbeit

Diplomarbeit von Adrian Aeschlimann an der TEKO Schweizerische Fachschule, Studiengang Software Engineering HF, Durchführung Herbst 2026.

Der Titel der Arbeit ist *IDE-Erweiterung zentraler Entwicklungsprozesse*.

## Thema

Gebaut wird BE-LabelExtension, eine Extension für Visual Studio, mit der sich Labels für Dynamics 365 direkt in der IDE suchen, anlegen, bearbeiten und übersetzen lassen. Sie löst das eigenständige Tool BE-LabelEditor der BE-terna AG ab, für das bei jedem Lokalisierungsvorgang die IDE verlassen werden muss.

## Termine

| Ereignis | Datum |
| --- | --- |
| Start | 04.09.2026 |
| Abgabe | 02.11.2026, 17.00 Uhr |
| Präsentation | 13.11.2026 |

## Aufbau

| Ordner | Inhalt |
| --- | --- |
| `documentation/` | Projektdokumentation in Typst, Diagramme in PlantUML |
| `source/` | Quellcode |

## Dokumentation bauen

Benötigt werden [Typst](https://typst.app/) und für die Diagramme [PlantUML](https://plantuml.com/).

```bash
cd documentation && typst compile main.typ
```

Die gerenderten Diagramme sind versioniert, für einen reinen Dokumentations-Build wird PlantUML also nicht gebraucht. Nach einer Änderung an einer `.puml`-Datei muss das zugehörige PNG neu erzeugt werden.

```bash
cd documentation/diagrams && plantuml -tpng -charset UTF-8 <datei>.puml
```

Details stehen in [documentation/README.md](documentation/README.md).

## Hinweise zur Veröffentlichung

Dieses Repository ist öffentlich. Es enthält die eigene Arbeit und keine Bestandteile der Standardanwendung von Microsoft Dynamics 365. Beispiele und Screenshots stützen sich nicht auf Betriebsdaten.

## Lizenz

MIT, siehe [LICENSE](LICENSE).
