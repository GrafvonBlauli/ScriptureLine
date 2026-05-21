# Bibelstudienprogramm – Ausführliches Konzept

**Arbeitstitel:** `ScriptureLine`  
**Alternativnamen:** `BibelStamm`, `BibelStudium Genealogie`, `BibleStudyGenealogy`  
**Stand:** 20.05.2026  
**Zielplattform:** Lokale Desktop-App  
**Empfohlener technischer Ansatz:** .NET / C# / Avalonia / SQLite / MVVM  
**Oberfläche:** Deutsch  
**Code, Modelle, Tabellen, Klassen:** Englisch  
**Startzustand:** Keine vorbefüllten biblischen Daten. Der Benutzer erfasst alle Inhalte selbst.

---

## 1. Leitidee

Das Programm soll ein interaktives Studienwerkzeug für biblische Personen, Beziehungen, Orte, Ereignisse, Zeiträume, Karten, Routen, Bibelstellen, Bilder, PDFs und eigene Ausarbeitungen werden.

Es ist keine fertige Bibeldatenbank und kein vorgegebenes Lexikon, sondern ein persönliches Forschungs- und Studienprogramm.

Der Benutzer soll selbst eintragen können:

- Personen
- Familienbeziehungen
- Partner, Kinder, Geschwister und Vorfahren
- Ereignisse
- Bibelstellen
- Orte
- Reisen und Routen
- Berufe, Rollen und Titel
- Bilder
- PDFs
- Kommentare
- Forschungsfragen
- eigene Ausarbeitungen
- verschiedene Deutungen und Unsicherheiten

Die App soll daraus interaktive Ansichten erzeugen:

- Stammbaum
- Personenakte
- Zeitstrahl
- Kartenansicht
- Routenansicht
- Ereignisübersicht
- Orteübersicht
- Bibelstellenübersicht
- Studienarchiv
- Auswertungen pro Person oder Zeitraum

---

## 2. Grundprinzipien

### 2.1 Lokal zuerst

Die App soll zuerst vollständig lokal funktionieren.

Das bedeutet:

- keine Serverpflicht
- keine Anmeldung nötig
- keine Cloud-Abhängigkeit
- Daten liegen auf dem eigenen Gerät
- Projektdateien können gesichert und exportiert werden
- spätere Weitergabe an andere Geräte über Projektpakete

### 2.2 Benutzer trägt Daten selbst ein

Es sollen keine biblischen Daten automatisch eingefügt werden.

Gründe:

- biblische Chronologie ist häufig auslegungsabhängig
- viele Geburts- und Sterbedaten sind unklar
- Ortszuordnungen können unsicher sein
- Personenbeziehungen sind nicht immer eindeutig
- Bibelübersetzungen sind urheberrechtlich unterschiedlich geschützt
- der Benutzer möchte selbst forschen und ausarbeiten

Die App stellt also nur die Struktur bereit.

### 2.3 Unsicherheit ist ein Kernbestandteil

Das Programm darf nicht so tun, als wären alle Angaben eindeutig.

Deshalb bekommt fast jede relevante Information Felder für:

- Sicherheit
- Quelle
- Kommentar
- Alternativen
- eigene Arbeitshypothese
- Datierungsmodell
- Begründung

Beispiele:

- „Geburt ungefähr um 2000 v. Chr.“
- „Ort wahrscheinlich identisch mit ...“
- „Beziehung traditionell angenommen“
- „Datierung abhängig von Chronologie A“
- „Diese Route ist rekonstruiert und nicht ausdrücklich beschrieben“

### 2.4 Deutsch für den Benutzer, Englisch im Code

Die Oberfläche wird deutsch gestaltet:

- „Personen“
- „Stammbaum“
- „Ereignisse“
- „Orte“
- „Zeitstrahl“
- „Karte“
- „Bibelstellen“
- „Notizen“
- „Forschungsfragen“

Im Code werden englische Namen verwendet:

- `Person`
- `Relationship`
- `Event`
- `Place`
- `TimelineEntry`
- `BibleReference`
- `ResearchQuestion`
- `MediaFile`
- `Route`
- `RouteStop`

Das hält das Projekt technisch sauber und erleichtert spätere Erweiterungen.

---

## 3. Zielbild der App

Langfristig soll die App wie ein interaktiver Bibelstudienraum funktionieren.

Man öffnet eine Person, zum Beispiel Abraham, und sieht:

- Bild oder Symbolbild
- Namen und Namensvarianten
- Lebensdaten oder Zeitrahmen
- Familie
- wichtige Ereignisse
- Orte
- Bibelstellen
- Reisen
- eigene Notizen
- PDFs und Ausarbeitungen
- offene Forschungsfragen
- Zeitleiste
- Kartenansicht
- Stammbaumposition

Von dort kann man weitergehen zu:

- Sara
- Isaak
- Lot
- Haran
- Ur
- Kanaan
- Ereignissen
- Bibelstellen
- Routen

Die App soll sich wie ein großes, verknüpftes Studiennetz anfühlen.

---

## 4. Zielgruppe

### 4.1 Erste Zielgruppe

Zuerst ist die App für den persönlichen Gebrauch gedacht.

Schwerpunkte:

- private Bibelstudien
- eigene Notizen
- eigener Forschungsstand
- persönliche Chronologie
- übersichtliches Erkunden
- lokale Datensicherheit

### 4.2 Spätere Zielgruppe

Später könnte die App auch für andere nutzbar gemacht werden.

Dann werden wichtiger:

- einfache Benutzerführung
- saubere Fehlerhinweise
- Projektimport/export
- Dokumentation
- Beispieldatei ohne urheberrechtlich geschützte Inhalte
- stabiler Installer
- eventuell Mehrbenutzerfähigkeit oder geteilte Projekte

---

## 5. Technische Grundentscheidung

### 5.1 Desktop-App statt Web-App

Die App soll keine Web-App werden.

Vorteile einer Desktop-App:

- lokale Datenhaltung
- keine Servereinrichtung
- gute Arbeit mit Dateien, Bildern und PDFs
- stabil für private Forschung
- unabhängig vom Internet nutzbar
- leichter als eigenes Werkzeug kontrollierbar

### 5.2 Empfohlener Stack

| Bereich | Entscheidung |
|---|---|
| Sprache | C# |
| UI | Avalonia |
| Architektur | MVVM |
| Datenbank | SQLite |
| Datenzugriff | Entity Framework Core oder leichtgewichtige Repository-Schicht |
| IDE | VS Code |
| KI-Unterstützung | Codex |
| Karten später | Mapsui prüfen |
| Zeichnungen / Tree Rendering | Avalonia Custom Controls, später ggf. SkiaSharp |
| Export | ZIP-basiertes Projektpaket |
| Projektdatei | `.biblestudy` |

### 5.3 Warum Avalonia?

Avalonia ist geeignet, weil:

- Desktop-App möglich
- XAML-ähnliches UI
- C#-basiert
- plattformübergreifend
- gut zu MVVM passt
- du bereits in diese Richtung gearbeitet hast
- moderne UI möglich ist
- lokale App ohne Browser entsteht

### 5.4 Warum SQLite?

SQLite ist geeignet, weil:

- lokale Datei-Datenbank
- keine Serverinstallation
- gut für Desktop-Apps
- leicht zu sichern
- gut exportierbar
- ausreichend für viele Personen, Orte und Ereignisse
- kompatibel mit späterem Projektpaket

---

## 6. Projektstruktur

Empfohlene Solution-Struktur:

```text
BibleStudyGenealogy/
  src/
    BibleStudyGenealogy.App/
      Views/
      ViewModels/
      Controls/
      Assets/
      Themes/
      Converters/
      DesignData/

    BibleStudyGenealogy.Core/
      Models/
      Enums/
      ValueObjects/
      Services/
      Validation/
      Rules/

    BibleStudyGenealogy.Infrastructure/
      Database/
      Repositories/
      FileStorage/
      ImportExport/
      Backup/
      Migrations/

    BibleStudyGenealogy.Rendering/
      TreeLayout/
      Timeline/
      DiagramDrawing/

    BibleStudyGenealogy.Maps/
      MapModels/
      RouteRendering/
      HistoricalLayers/

  tests/
    BibleStudyGenealogy.Tests/

  docs/
    Konzept.md
    Roadmap.md
    Datenmodell.md
    UI-Skizzen.md
    Codex-Aufgaben.md
```

---

## 7. Hauptbereiche der Oberfläche

Die App bekommt eine feste Hauptnavigation.

```text
Dashboard
Personen
Stammbaum
Zeitstrahl
Karte
Ereignisse
Orte
Bibelstellen
Mediathek
Auswertungen
Forschungsfragen
Einstellungen
```

### 7.1 Dashboard

Das Dashboard zeigt eine ruhige Übersicht.

Mögliche Inhalte:

- zuletzt bearbeitete Personen
- zuletzt hinzugefügte Ereignisse
- offene Forschungsfragen
- Personen ohne Bild
- Orte ohne Koordinaten
- unsichere Beziehungen
- Projektstatistik
- Schnellaktionen

Beispiele für Kennzahlen:

```text
Personen: 0
Beziehungen: 0
Ereignisse: 0
Orte: 0
Bibelstellen: 0
Medien: 0
Offene Forschungsfragen: 0
```

### 7.2 Personenbereich

Ansichten:

- Personenliste
- Suchfeld
- Filter
- Detailbereich
- Personenakte
- Schnellverknüpfungen zu Familie, Ereignissen, Orten, Bibelstellen und Medien

### 7.3 Stammbaum

Ansichten:

- interaktiver Stammbaum
- Vorfahrenansicht
- Nachkommenansicht
- Familienansicht
- freie Netzwerkansicht

Funktionen:

- Zoomen
- Verschieben
- Person anklicken
- Details öffnen
- Beziehung anzeigen
- unsichere Verbindungen markieren
- Linien nach Beziehungstyp unterscheiden

### 7.4 Zeitstrahl

Ansichten:

- Zeitstrahl einer Person
- Zeitstrahl eines Ortes
- Zeitstrahl eines Ereignistyps
- Zeitstrahl eines biblischen Buches
- Zeitstrahl eines frei gewählten Zeitabschnitts

### 7.5 Karte

Ansichten:

- Orte
- Ereignisse
- Reisen
- Routen
- Aufenthaltsorte
- zeitabhängige Kartendarstellung

### 7.6 Mediathek

Verwaltung für:

- Bilder
- PDFs
- Karten
- Scans
- eigene Ausarbeitungen
- Skizzen
- Notizen
- externe Dokumente

---

## 8. Datenmodell – Überblick

Die wichtigsten Kernmodelle:

```text
Person
Relationship
Event
Place
Route
RouteStop
BibleReference
MediaFile
Note
ResearchQuestion
Tag
Source
ChronologyModel
DateInfo
CertaintyLevel
ProjectSettings
```

---

## 9. Person

### 9.1 Zweck

Eine Person ist das zentrale Element der App.

Alle Informationen können direkt oder indirekt mit Personen verbunden werden.

### 9.2 Felder

```text
Person
- Id
- MainName
- DisplayName
- AlternativeNames
- HebrewName
- GreekName
- NameMeaning
- Gender
- BirthDateInfoId
- DeathDateInfoId
- LifeSpanText
- PrimaryRole
- Occupation
- TribeOrGroup
- ShortDescription
- LongDescription
- PortraitMediaFileId
- Status
- CreatedAt
- UpdatedAt
```

### 9.3 Status

```text
Active
Uncertain
Archived
Rejected
DuplicateCandidate
```

### 9.4 Beispiele für Rollen

```text
Patriarch
Matriarch
Prophet
King
Judge
Priest
Apostle
Disciple
Shepherd
Fisherman
Tentmaker
Pharaoh
Governor
Scribe
Warrior
Unknown
Custom
```

---

## 10. Relationship

### 10.1 Zweck

Beziehungen verbinden Personen.

Die Beziehung wird nicht nur als einfache Linie gespeichert, sondern mit Bedeutung, Quelle und Sicherheit.

### 10.2 Felder

```text
Relationship
- Id
- PersonAId
- PersonBId
- RelationshipType
- Direction
- CertaintyLevel
- SourceNote
- Comment
- DateInfoId
- CreatedAt
- UpdatedAt
```

### 10.3 Beziehungstypen

```text
ParentChild
Spouse
Sibling
Ancestor
Descendant
AdoptiveParent
LegalParent
TribeMember
Mentor
Companion
Opponent
UnknownRelated
Custom
```

### 10.4 Darstellung im Stammbaum

| Beziehung | Darstellung |
|---|---|
| sichere Eltern-Kind-Beziehung | durchgezogene Linie |
| unsichere Beziehung | gestrichelte Linie |
| rechtliche/adoptive Beziehung | andere Linienart |
| Ehe/Partnerschaft | horizontale Verbindung |
| traditionelle Beziehung | markierte Linie mit Hinweis |
| verworfene Beziehung | standardmäßig ausgeblendet |

---

## 11. Event

### 11.1 Zweck

Ereignisse sind eigene Datensätze.

Sie werden mit Personen, Orten, Bibelstellen, Medien und Notizen verknüpft.

### 11.2 Felder

```text
Event
- Id
- Title
- EventType
- DateInfoId
- PlaceId
- ShortDescription
- LongDescription
- CertaintyLevel
- ChronologyModelId
- CreatedAt
- UpdatedAt
```

### 11.3 Ereignistypen

```text
Birth
Death
Marriage
Calling
Journey
Battle
Exile
Return
Speech
Prophecy
Miracle
Meeting
Covenant
Construction
ReignStart
ReignEnd
Writing
Vision
Custom
```

### 11.4 Verknüpfungstabellen

```text
EventPerson
- EventId
- PersonId
- RoleInEvent

EventBibleReference
- EventId
- BibleReferenceId

EventMediaFile
- EventId
- MediaFileId

EventTag
- EventId
- TagId
```

---

## 12. Place

### 12.1 Zweck

Orte bilden die Grundlage für Karten, Reisen und Ereignisse.

### 12.2 Felder

```text
Place
- Id
- HistoricalName
- ModernName
- AlternativeNames
- Latitude
- Longitude
- LocationCertainty
- TimePeriod
- ShortDescription
- LongDescription
- CreatedAt
- UpdatedAt
```

### 12.3 Besonderheiten

Ein biblischer Ort kann mehrere Namen haben:

- historischer Name
- moderner Name
- alternative Schreibweisen
- hebräische/griechische Form
- Name in einer bestimmten Übersetzung
- unsicherer oder vermuteter Ort

### 12.4 Ortsunsicherheit

Beispiele:

```text
Known
Likely
Possible
Disputed
Symbolic
Unknown
```

---

## 13. Route und RouteStop

### 13.1 Zweck

Routen bilden Reisen ab.

Beispiele:

- Abrahams Reise
- Jakobs Wege
- Exodus
- Wüstenwanderung
- Reisen Jesu
- Missionsreisen des Paulus

### 13.2 Route

```text
Route
- Id
- Title
- PersonId
- EventId
- DateInfoId
- Description
- CertaintyLevel
- CreatedAt
- UpdatedAt
```

### 13.3 RouteStop

```text
RouteStop
- Id
- RouteId
- PlaceId
- SortOrder
- DateInfoId
- Description
- EventId
```

---

## 14. BibleReference

### 14.1 Zweck

Bibelstellen werden als Verweise gespeichert, nicht als automatisch eingefügter Bibelvolltext.

Das ist wichtig, weil viele moderne Übersetzungen urheberrechtlich geschützt sind.

Die Hauptübersetzung des Benutzers ist die **Revidierte Neue-Welt-Übersetzung**, aber das System soll mehrere Übersetzungen unterstützen.

### 14.2 Felder

```text
BibleReference
- Id
- TranslationName
- Book
- ChapterStart
- VerseStart
- ChapterEnd
- VerseEnd
- ReferenceText
- UserExcerpt
- UserSummary
- UserComment
- CreatedAt
- UpdatedAt
```

### 14.3 Übersetzungssystem

Die App soll nicht fest auf eine Übersetzung begrenzt sein.

Mögliche Felder:

```text
BibleTranslation
- Id
- Name
- Abbreviation
- Language
- CopyrightNote
- DefaultForProject
```

Beispiele:

```text
Revidierte Neue-Welt-Übersetzung
Luther 1912
Elberfelder
Einheitsübersetzung
King James Version
Benutzerdefiniert
```

Hinweis: Es sollen keine urheberrechtlich geschützten Volltexte automatisch ausgeliefert werden. Der Benutzer kann eigene Auszüge oder Zusammenfassungen eintragen, sofern er die jeweiligen Nutzungsrechte beachtet.

---

## 15. DateInfo – flexible Datumsangaben

### 15.1 Problem

Biblische Daten sind häufig nicht exakt.

Ein normales Datumsfeld reicht nicht.

### 15.2 Lösung

Ein eigenes `DateInfo`-Modell.

```text
DateInfo
- Id
- DateType
- ExactDate
- Year
- YearFrom
- YearTo
- ApproximationText
- IsBeforeChrist
- CertaintyLevel
- ChronologyModelId
- Comment
```

### 15.3 DateType

```text
Unknown
ExactDate
ExactYear
ApproximateYear
YearRange
BeforeYear
AfterYear
BetweenEvents
RelativeToEvent
TextOnly
```

### 15.4 Beispiele

```text
unbekannt
ca. 2000 v. Chr.
zwischen 2100 und 1900 v. Chr.
vor dem Exodus
nach dem Exil
zur Zeit Davids
nach eigener Chronologie
```

---

## 16. ChronologyModel

### 16.1 Zweck

Mehrere Chronologien sollen möglich sein.

Zum Beispiel:

- eigene Chronologie
- konservative Datierung
- wissenschaftliche Datierung
- traditionelle Datierung
- projektinterne Arbeitshypothese

### 16.2 Felder

```text
ChronologyModel
- Id
- Name
- Description
- IsDefault
- CreatedAt
- UpdatedAt
```

### 16.3 Nutzen

Der Benutzer kann später in Zeitstrahlen und Karten zwischen Chronologie-Modellen wechseln.

Beispiel:

```text
Zeige Ereignisse nach:
- Eigene Studienchronologie
- Alternative Chronologie
- Nur sichere Datierungen
```

---

## 17. CertaintyLevel

### 17.1 Zweck

Fast jede Angabe kann mit einem Sicherheitsgrad versehen werden.

### 17.2 Vorgeschlagene Stufen

```text
ExplicitlyMentioned
Likely
Possible
Traditional
Disputed
UserHypothesis
Unknown
```

### 17.3 Deutsche Anzeige

| Code | Anzeige |
|---|---|
| `ExplicitlyMentioned` | ausdrücklich erwähnt |
| `Likely` | wahrscheinlich |
| `Possible` | möglich |
| `Traditional` | traditionell angenommen |
| `Disputed` | umstritten |
| `UserHypothesis` | eigene Arbeitshypothese |
| `Unknown` | unbekannt |

---

## 18. Forschungsfragen

### 18.1 Zweck

Die App soll nicht nur fertige Angaben speichern, sondern auch offene Fragen.

### 18.2 Felder

```text
ResearchQuestion
- Id
- Title
- QuestionText
- Status
- Priority
- LinkedPersonId
- LinkedPlaceId
- LinkedEventId
- LinkedBibleReferenceId
- Notes
- CreatedAt
- UpdatedAt
```

### 18.3 Status

```text
Open
InProgress
Answered
Rejected
Archived
```

### 18.4 Beispiele

```text
Ist diese Person identisch mit einer anderen erwähnten Person?
Welcher Ort ist hier gemeint?
Welche Chronologie passt zu diesem Ereignis?
Ist diese Verwandtschaft ausdrücklich erwähnt oder erschlossen?
Welche Bibelstelle stützt diese Angabe?
```

---

## 19. Medien und Dokumente

### 19.1 Zweck

Bilder, PDFs und Ausarbeitungen sollen mit Personen, Orten und Ereignissen verbunden werden können.

### 19.2 MediaFile

```text
MediaFile
- Id
- FileName
- OriginalFileName
- FilePath
- MediaType
- Title
- Description
- CopyrightNote
- CreatedAt
- UpdatedAt
```

### 19.3 MediaType

```text
Image
Pdf
Map
Scan
Document
Audio
Video
Other
```

### 19.4 Projektordner

```text
MeinBibelProjekt/
  project.sqlite
  Media/
    Persons/
    Places/
    Events/
    PDFs/
    Maps/
    Other/
  Thumbnails/
  Backups/
```

---

## 20. Notizen

### 20.1 Note

```text
Note
- Id
- Title
- Content
- LinkedPersonId
- LinkedPlaceId
- LinkedEventId
- LinkedBibleReferenceId
- LinkedMediaFileId
- Tags
- CreatedAt
- UpdatedAt
```

### 20.2 Notizarten

```text
General
Study
Interpretation
Question
Observation
ToDo
SourceNote
```

---

## 21. Tags

Tags dienen zur flexiblen Gruppierung.

Beispiele:

```text
Patriarchenzeit
Exodus
Königszeit
Apostelgeschichte
Paulus
Reise
Prophetie
Unsicher
Offen
Wichtig
```

---

## 22. Stammbaumansicht im Detail

### 22.1 Ziel

Die Stammbaumansicht soll Personenbeziehungen sichtbar machen.

### 22.2 Hauptfunktionen

- Personenkarten mit Bild
- Name
- Lebenszeitraum
- Rolle
- Beziehungen
- Zoom
- Verschieben
- Fokus auf Person
- Vorfahren anzeigen
- Nachkommen anzeigen
- Familienkreis anzeigen
- unsichere Verbindungen markieren
- direkte Personenakte öffnen

### 22.3 Spätere Funktionen

- Export als Bild
- Export als PDF
- verschiedene Layouts
- große Stammbaumabschnitte
- Gruppierung nach Stamm, Familie oder Zeit
- Anzeige mehrerer Chronologien
- Einklappen von Nebenlinien
- Suche im Baum

---

## 23. Personenakte im Detail

### 23.1 Aufbau

```text
+---------------------------------------------------+
| Bild | Name | Lebensdaten | Rolle                  |
+---------------------------------------------------+
| Kurzbeschreibung                                  |
+---------------------------------------------------+
| Familie | Ereignisse | Orte | Bibelstellen          |
+---------------------------------------------------+
| Timeline / Notizen / Dokumente                    |
+---------------------------------------------------+
```

### 23.2 Tabs

```text
Überblick
Familie
Zeitstrahl
Orte & Reisen
Bibelstellen
Medien
Notizen
Forschungsfragen
Quellen
```

### 23.3 Auswertung einer Person

Die App soll automatisch eine Studienübersicht erzeugen:

- alle bekannten Daten
- Familienbeziehungen
- alle Ereignisse
- alle Bibelstellen
- alle Orte
- alle Reisen
- verknüpfte Medien
- offene Forschungsfragen
- unsichere Angaben
- Exportmöglichkeit

---

## 24. Zeitstrahl

### 24.1 Ziel

Der Zeitstrahl soll Ereignisse verständlich und chronologisch darstellen.

### 24.2 Modi

```text
Personen-Zeitstrahl
Orts-Zeitstrahl
Bibelbuch-Zeitstrahl
Themen-Zeitstrahl
Freier Zeitraum
Gesamtchronologie
```

### 24.3 Funktionen

- Ereignisse sortieren
- unsichere Daten markieren
- Zeiträume anzeigen
- Filter nach Person, Ort, Bibelbuch, Tag, Sicherheit
- Detailfenster für Ereignisse
- Sprung zur Karte
- Sprung zur Personenakte
- Export

---

## 25. Karte

### 25.1 Ziel

Orte und Reisen sollen geografisch sichtbar werden.

### 25.2 Funktionen

- heutige Kartengrundlage
- historische Ortsnamen als eigene Marker
- Orte mit Koordinaten
- Personenaufenthalte
- Ereignisorte
- Routen
- Zeitfilter
- Kartenmarker mit Detailansicht
- Verbindung zu Zeitstrahl

### 25.3 Historische Namen

Die App soll nicht versuchen, die moderne Karte selbst umzubenennen.

Stattdessen legt sie eine eigene historische Ebene darüber:

```text
Historischer Name
Moderner Name
Alternative Namen
Koordinaten
Zeitraum
Sicherheitsgrad
Kommentar
```

### 25.4 Routen

Eine Route besteht aus mehreren Stationen.

```text
Antiochia -> Seleuzia -> Zypern -> Perge -> Antiochia in Pisidien
```

Jede Station kann ein Ereignis, eine Bibelstelle und einen Kommentar haben.

---

## 26. Kombinierte Karte + Zeitleiste

### 26.1 Ziel

Eine besonders starke spätere Ansicht.

Oben:

```text
Karte
```

Unten:

```text
Zeitstrahl / Schieberegler
```

Links:

```text
Filter
```

Rechts:

```text
Details
```

### 26.2 Verhalten

Wenn der Benutzer die Zeit verändert:

- erscheinen oder verschwinden Orte
- Routen werden aktiv oder inaktiv
- Ereignisse werden eingeblendet
- Personen werden je nach Zeitraum sichtbar
- Marker ändern sich
- ausgewählte Chronologie beeinflusst Darstellung

---

## 27. Suche und Filter

### 27.1 Globale Suche

Eine Suchleiste durchsucht:

- Personen
- Orte
- Ereignisse
- Bibelstellen
- Notizen
- Forschungsfragen
- Medien
- Tags

### 27.2 Filter

Mögliche Filter:

- Zeitraum
- Bibelbuch
- Testament
- Rolle
- Beruf
- Ort
- Ereignistyp
- Sicherheitsgrad
- Chronologie
- hat Bild
- hat PDFs
- hat offene Fragen
- nur unsichere Angaben
- nur eigene Hypothesen

---

## 28. Export und Import

### 28.1 Projektpaket

Die App soll Projekte als Datei exportieren können.

Vorschlag:

```text
MeinProjekt.biblestudy
```

Intern ist das eine ZIP-Datei mit:

```text
manifest.json
project.sqlite
Media/
Thumbnails/
Settings/
```

### 28.2 Exportarten

| Export | Zweck |
|---|---|
| Projektpaket | vollständige Weitergabe |
| Datenbank-Backup | Sicherung |
| Personenbericht als PDF | Studienausgabe |
| Stammbaum als PNG/SVG/PDF | Druck oder Präsentation |
| Timeline als PDF/Bild | Auswertung |
| JSON/CSV | spätere Weiterverarbeitung |

### 28.3 Import

Import sollte ermöglichen:

- Projektpaket öffnen
- Projektpaket kopieren
- beschädigte Medien melden
- fehlende Dateien anzeigen
- Datenbankversion prüfen
- Migrationen ausführen

---

## 29. Backup-Konzept

### 29.1 Automatische Backups

Die App sollte Backups erzeugen:

- beim Öffnen eines Projekts
- beim Schließen
- vor Datenbankmigrationen
- manuell per Button
- optional täglich

### 29.2 Backup-Struktur

```text
Backups/
  2026-05-20_14-30_project.sqlite
  2026-05-20_14-30_manifest.json
```

### 29.3 Wiederherstellung

Eine spätere Wiederherstellungsfunktion sollte erlauben:

- Backup auswählen
- Vorschau anzeigen
- aktuelle Daten sichern
- Backup wiederherstellen

---

## 30. Rechte und Bibeltexte

Da viele moderne Bibelübersetzungen geschützt sind, soll die App keine geschützten Volltexte mitliefern.

Besonders wichtig:

- Bibelstellen zuerst nur als Referenzen speichern
- eigene Zusammenfassungen erlauben
- kurze eigene Auszüge nur durch den Benutzer
- Quellenhinweise speichern
- mehrere Übersetzungen unterstützen
- Übersetzung nicht fest in den Code schreiben

Die Revidierte Neue-Welt-Übersetzung kann als bevorzugte Übersetzung des Benutzers in den Projekteinstellungen hinterlegt werden, aber nicht automatisch als Volltext ausgeliefert werden.

---

## 31. Änderungsverlauf

Langfristig sollte die App einen Änderungsverlauf besitzen.

### 31.1 AuditLog

```text
AuditLog
- Id
- EntityType
- EntityId
- Action
- OldValue
- NewValue
- Comment
- CreatedAt
```

### 31.2 Nutzen

- Änderungen nachvollziehen
- Forschungsschritte dokumentieren
- Fehler leichter korrigieren
- verworfene Hypothesen nachvollziehbar halten

---

## 32. Studienmodus und Bearbeitungsmodus

### 32.1 Bearbeitungsmodus

Für Dateneingabe:

- Personen anlegen
- Beziehungen ändern
- Ereignisse bearbeiten
- Medien hochladen
- Notizen schreiben
- Daten löschen/archivieren

### 32.2 Studienmodus

Zum ruhigen Erkunden:

- keine versehentlichen Änderungen
- Personen ansehen
- Stammbaum erkunden
- Karte bewegen
- Timeline nutzen
- Notizen lesen
- PDF öffnen

---

## 33. Löschkonzept

Daten sollten nicht sofort hart gelöscht werden.

Besser:

```text
Active
Archived
Rejected
DeletedPending
```

Ein endgültiges Löschen kann später über eine Wartungsfunktion erfolgen.

Das ist wichtig, weil Forschung sich verändert und alte Hypothesen später wieder nützlich sein können.

---

## 34. Validierung und Datenqualität

Die App sollte Hinweise geben:

- Person ohne Namen
- Beziehung ohne Sicherheit
- Ort ohne Koordinaten
- Ereignis ohne Datum
- Bibelstelle unvollständig
- PDF-Datei fehlt
- doppelte Person möglich
- Route ohne Stationen
- Chronologie nicht gesetzt

Diese Hinweise sollen nicht nerven, sondern helfen.

---

## 35. Optik und Bediengefühl

Die App soll zuerst funktional sein, aber von Anfang an ruhig und ordentlich wirken.

### 35.1 Stil

- klare Navigation
- weiche Kontraste
- ruhiger Hintergrund
- dezente Karten und Linien
- gute Lesbarkeit
- keine überladenen Bildschirme
- übersichtliche Karten für Personen und Ereignisse

### 35.2 Spätere Politur

- dezente Animationen
- schöne Personen-Karten
- kleine Icons
- bessere Stammbaumdarstellung
- Themendesign hell/dunkel
- ruhiger Studienmodus

---

## 36. Mögliche erste Projektversionen

### 36.1 Version 0.1

- Projekt erstellen/öffnen
- SQLite-Datenbank
- Personenliste
- Person anlegen
- Person bearbeiten
- Bild hinzufügen
- einfache Speicherung

### 36.2 Version 0.2

- Beziehungen
- Eltern/Kinder/Partner
- Familienbereich in Personenakte
- einfache Beziehungsübersicht

### 36.3 Version 0.3

- einfache Stammbaumansicht
- Personenkarten
- Linien
- Zoom
- Verschieben
- Person öffnen

### 36.4 Version 0.4

- Ereignisse
- Bibelstellen
- Notizen
- Verbindung zu Personen

### 36.5 Version 0.5

- Zeitstrahl
- Auswertung einzelner Personen

### 36.6 Version 0.6

- Orte
- erste Kartenansicht

### 36.7 Version 0.7

- Routen
- Karte + Zeitfilter

### 36.8 Version 1.0

- stabiler persönlicher Einsatz
- Backup
- Export/Import
- saubere Auswertungen
- Dokumentation

---

## 37. Codex-Arbeitsweise

Codex sollte nicht mit riesigen Aufgaben überladen werden.

Gute Aufgaben:

```text
Erstelle die Modelle Person, Relationship, Event, Place und BibleReference im Core-Projekt.
```

```text
Baue eine Personenliste in Avalonia mit Suchfeld und Detailansicht.
```

```text
Erstelle eine SQLite-Repository-Schicht für Person.
```

```text
Füge eine einfache Stammbaumansicht mit zoombarem Canvas hinzu.
```

```text
Erstelle eine Datenstruktur für flexible Datumsangaben.
```

Schlechte Aufgabe:

```text
Baue mir die komplette App.
```

Die Entwicklung sollte in kleinen, testbaren Schritten erfolgen.

---

## 38. Nicht-Ziele für den Anfang

Für den Start nicht einbauen:

- automatische Bibeldatenbank
- automatische komplette Bibeltexte
- Online-Synchronisation
- Benutzerkonten
- Mehrbenutzerbetrieb
- perfekte historische Karten
- komplette Routenlogik
- KI-gestützte automatische Auslegung
- Cloudspeicher

Diese Dinge können später geprüft werden.

---

## 39. Erfolgskriterium für den ersten echten Prototyp

Der erste Prototyp ist erfolgreich, wenn Folgendes möglich ist:

1. App starten
2. neues Projekt anlegen
3. Person anlegen
4. Bild hinzufügen
5. Eltern/Kinder/Partner hinzufügen
6. Person in einfacher Stammbaumansicht sehen
7. Projekt schließen
8. Projekt wieder öffnen
9. Daten sind noch da
10. Projekt als Datei sichern

Wenn das funktioniert, steht das Fundament.

---

## 40. Zusammenfassung

Das Projekt soll eine lokale, interaktive Desktop-App für biblische Studien werden.

Kern:

- Personen
- Beziehungen
- Stammbaum
- Ereignisse
- Bibelstellen
- Orte
- Karten
- Routen
- Zeitstrahlen
- PDFs
- Notizen
- Forschungsfragen
- Unsicherheiten
- mehrere Chronologien
- Export/Import

Empfohlener Weg:

1. Grundgerüst
2. Personenverwaltung
3. Beziehungen
4. Stammbaum
5. Ereignisse und Bibelstellen
6. Timeline
7. Orte
8. Karte
9. Routen
10. Export und Auswertungen

Das Projekt ist groß, aber sehr gut machbar, wenn es schrittweise entwickelt wird.
