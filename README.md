# Diplomarbeit

Diplomarbeit von Adrian Aeschlimann an der TEKO Schweizerische Fachschule, Studiengang Software Engineering HF, Durchführung Herbst 2026.

Der definitive Titel steht noch nicht fest.

## Thema

TODO

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

TODO
