# Bibelstudienprogramm – Roadmap

**Arbeitstitel:** `ScriptureLine` / `BibleStudyGenealogy`  
**Stand:** 20.05.2026  
**Entwicklungsprinzip:** Erst funktional und stabil, danach schön und erweitert.  
**Oberfläche:** Deutsch  
**Code/Modelle:** Englisch  
**Startdaten:** Keine vorbefüllten biblischen Daten.  

---

## 1. Entwicklungsstrategie

Das Projekt soll nicht als riesiger Block begonnen werden.

Stattdessen wird in kleinen Versionen gearbeitet.

Jede Version soll:

- startbar sein
- speicherbar sein
- einen sichtbaren Nutzen bringen
- möglichst wenige Baustellen gleichzeitig öffnen
- mit Codex in klaren Einzelaufgaben umsetzbar sein
- am Ende getestet werden

Der beste Weg:

```text
Grundgerüst -> Personen -> Beziehungen -> Stammbaum -> Ereignisse -> Timeline -> Orte -> Karte -> Routen -> Export
```

---

## 2. Vorbereitung vor dem ersten Code

### 2.1 Installation prüfen

Benötigt:

- Visual Studio Code
- .NET SDK
- Git
- Avalonia Templates
- Codex-Erweiterung für VS Code
- optional: DB Browser for SQLite
- optional: Avalonia-Erweiterung für VS Code
- optional: C# Dev Kit oder C#-Erweiterung

### 2.2 Terminalprüfung

Nach der Installation im Terminal prüfen:

```powershell
dotnet --version
git --version
```

Avalonia Templates installieren:

```powershell
dotnet new install Avalonia.Templates
```

Prüfen, ob Avalonia-Vorlagen verfügbar sind:

```powershell
dotnet new list avalonia
```

---

## 3. Phase 0 – Projektstart und Grundstruktur

### Ziel

Eine saubere Solution mit mehreren Projekten erstellen.

### Ergebnis

Das Projekt lässt sich öffnen, bauen und starten.

### Aufgaben

1. Ordner erstellen:

```text
BibleStudyGenealogy/
```

2. Solution erstellen:

```powershell
dotnet new sln -n BibleStudyGenealogy
```

3. Projekte erstellen:

```powershell
dotnet new avalonia.app -n BibleStudyGenealogy.App
dotnet new classlib -n BibleStudyGenealogy.Core
dotnet new classlib -n BibleStudyGenealogy.Infrastructure
dotnet new classlib -n BibleStudyGenealogy.Rendering
dotnet new classlib -n BibleStudyGenealogy.Maps
dotnet new xunit -n BibleStudyGenealogy.Tests
```

4. Projekte zur Solution hinzufügen:

```powershell
dotnet sln add .\BibleStudyGenealogy.App\BibleStudyGenealogy.App.csproj
dotnet sln add .\BibleStudyGenealogy.Core\BibleStudyGenealogy.Core.csproj
dotnet sln add .\BibleStudyGenealogy.Infrastructure\BibleStudyGenealogy.Infrastructure.csproj
dotnet sln add .\BibleStudyGenealogy.Rendering\BibleStudyGenealogy.Rendering.csproj
dotnet sln add .\BibleStudyGenealogy.Maps\BibleStudyGenealogy.Maps.csproj
dotnet sln add .\BibleStudyGenealogy.Tests\BibleStudyGenealogy.Tests.csproj
```

5. Projektverweise setzen:

```powershell
dotnet add .\BibleStudyGenealogy.App\BibleStudyGenealogy.App.csproj reference .\BibleStudyGenealogy.Core\BibleStudyGenealogy.Core.csproj
dotnet add .\BibleStudyGenealogy.App\BibleStudyGenealogy.App.csproj reference .\BibleStudyGenealogy.Infrastructure\BibleStudyGenealogy.Infrastructure.csproj
dotnet add .\BibleStudyGenealogy.App\BibleStudyGenealogy.App.csproj reference .\BibleStudyGenealogy.Rendering\BibleStudyGenealogy.Rendering.csproj
dotnet add .\BibleStudyGenealogy.App\BibleStudyGenealogy.App.csproj reference .\BibleStudyGenealogy.Maps\BibleStudyGenealogy.Maps.csproj

dotnet add .\BibleStudyGenealogy.Infrastructure\BibleStudyGenealogy.Infrastructure.csproj reference .\BibleStudyGenealogy.Core\BibleStudyGenealogy.Core.csproj
dotnet add .\BibleStudyGenealogy.Rendering\BibleStudyGenealogy.Rendering.csproj reference .\BibleStudyGenealogy.Core\BibleStudyGenealogy.Core.csproj
dotnet add .\BibleStudyGenealogy.Maps\BibleStudyGenealogy.Maps.csproj reference .\BibleStudyGenealogy.Core\BibleStudyGenealogy.Core.csproj
dotnet add .\BibleStudyGenealogy.Tests\BibleStudyGenealogy.Tests.csproj reference .\BibleStudyGenealogy.Core\BibleStudyGenealogy.Core.csproj
```

6. Build testen:

```powershell
dotnet build
```

7. App starten:

```powershell
dotnet run --project .\BibleStudyGenealogy.App
```

### Akzeptanzkriterien

- Solution öffnet in VS Code
- `dotnet build` läuft ohne Fehler
- App startet
- Git-Repository ist initialisiert
- erster Commit ist erstellt

### Codex-Aufgabe

```text
Richte eine saubere Solution-Struktur für eine Avalonia-Desktop-App mit Core, Infrastructure, Rendering, Maps und Tests ein. Die UI soll deutsch sein, Code und Modelle englisch. Erstelle nur das Grundgerüst und prüfe, dass dotnet build funktioniert.
```

---

## 4. Phase 1 – Projektdatei, Einstellungen und lokale Datenbank

### Ziel

Die App kann ein lokales Studienprojekt anlegen und öffnen.

### Ergebnis

Es gibt eine erste Projektstruktur mit SQLite-Datenbank und Medienordnern.

### Aufgaben

1. Projektmodell anlegen:

```text
ProjectSettings
ProjectMetadata
```

2. Projektordnerstruktur erzeugen:

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

3. Startbildschirm bauen:

- Neues Projekt erstellen
- Projekt öffnen
- zuletzt geöffnete Projekte
- Einstellungen

4. Datenbankzugriff vorbereiten:

- SQLite einbinden
- erste Tabellen erstellen
- einfache Migration vorbereiten

5. Projektdaten speichern:

- Projektname
- Beschreibung
- bevorzugte Bibelübersetzung
- Standardsprache
- Erstellungsdatum
- letzte Öffnung

### Akzeptanzkriterien

- Neues Projekt kann angelegt werden
- Projektordner wird erstellt
- SQLite-Datei wird erstellt
- Projekt kann wieder geöffnet werden
- bevorzugte Bibelübersetzung kann als Text gespeichert werden
- Standardwert kann „Revidierte Neue-Welt-Übersetzung“ sein, ohne Volltext mitzuliefern

### Codex-Aufgabe

```text
Erstelle ein lokales Projektformat für die App. Beim Anlegen eines Projekts sollen eine SQLite-Datei und die Ordner Media, Thumbnails und Backups erzeugt werden. Speichere ProjectSettings mit Projektname, Beschreibung, Sprache und bevorzugter Bibelübersetzung. Keine biblischen Inhalte vorbefüllen.
```

---

## 5. Phase 2 – Kernmodelle

### Ziel

Die wichtigsten Datenmodelle im Core-Projekt erstellen.

### Ergebnis

Die App besitzt saubere Klassen und Enums als Fundament.

### Modelle

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
ProjectSettings
```

### Enums

```text
CertaintyLevel
RelationshipType
EventType
DateType
PersonStatus
MediaType
ResearchQuestionStatus
Gender
PlaceCertainty
```

### Akzeptanzkriterien

- Modelle liegen im Core-Projekt
- keine UI-Abhängigkeiten im Core-Projekt
- Enums sind englisch
- Kommentare erklären komplexe Modelle
- `dotnet build` läuft

### Codex-Aufgabe

```text
Erstelle im Core-Projekt die zentralen Modelle und Enums für Personen, Beziehungen, Ereignisse, Orte, Routen, Bibelstellen, Medien, Notizen, Forschungsfragen, Chronologien und flexible Datumsangaben. Verwende englische Code-Namen. Die App soll später eine deutsche Oberfläche haben.
```

---

## 6. Phase 3 – Personenverwaltung Version 0.1

### Ziel

Personen können angelegt, bearbeitet, gesucht und gespeichert werden.

### Ergebnis

Erster echter Nutzwert der App.

### Funktionen

- Personenliste
- neue Person anlegen
- Person bearbeiten
- Person löschen/archivieren
- Suchfeld
- Bild hinzufügen
- Grunddaten speichern

### Felder in erster Version

```text
MainName
AlternativeNames
HebrewName
GreekName
NameMeaning
Gender
BirthDateInfo
DeathDateInfo
PrimaryRole
Occupation
ShortDescription
LongDescription
PortraitMediaFileId
Status
```

### UI

Deutsch:

```text
Personen
Neue Person
Bearbeiten
Archivieren
Name
Alternative Namen
Geburt
Tod
Rolle
Beruf
Kurzbeschreibung
Notizen
Bild auswählen
```

### Akzeptanzkriterien

- Person kann erstellt werden
- Person kann gespeichert werden
- Person erscheint in Liste
- Person kann wieder geöffnet werden
- Person kann bearbeitet werden
- Bildpfad wird gespeichert
- App startet nach Neustart mit denselben Daten

### Codex-Aufgabe

```text
Baue eine Personenverwaltung mit deutscher Oberfläche. Personen sollen erstellt, bearbeitet, gesucht und gespeichert werden können. Die Datenmodelle bleiben englisch. Nutze die vorhandene SQLite-Projektstruktur. Es sollen keine Beispielpersonen eingefügt werden.
```

---

## 7. Phase 4 – Flexible Datumsangaben und Sicherheit

### Ziel

Biblische Unsicherheit sauber erfassen.

### Ergebnis

Personen, Ereignisse und Beziehungen können unsichere Angaben speichern.

### Funktionen

- exaktes Datum
- exaktes Jahr
- ungefährer Zeitraum
- Jahrbereich
- vor/nach Jahr
- textliche Angabe
- Sicherheitsgrad
- Chronologiemodell

### UI-Beispiele

```text
Datumsart:
- unbekannt
- exaktes Datum
- genaues Jahr
- ungefähr
- Zeitraum
- vor einem Jahr
- nach einem Jahr
- freie Angabe
```

Sicherheit:

```text
ausdrücklich erwähnt
wahrscheinlich
möglich
traditionell angenommen
umstritten
eigene Arbeitshypothese
unbekannt
```

### Akzeptanzkriterien

- Person kann Geburtsdaten mit Unsicherheit speichern
- Person kann Sterbedaten mit Unsicherheit speichern
- Datumsangaben werden verständlich angezeigt
- keine Pflicht zu exakten Daten
- Chronologiemodell kann später erweitert werden

### Codex-Aufgabe

```text
Erweitere die Personenverwaltung um flexible Datumsangaben mit DateInfo und CertaintyLevel. Die UI soll deutsche Auswahltexte anzeigen, intern aber englische Enums verwenden.
```

---

## 8. Phase 5 – Beziehungen Version 0.2

### Ziel

Personen können miteinander verbunden werden.

### Ergebnis

Familienbeziehungen sind erfassbar.

### Funktionen

- Eltern hinzufügen
- Kinder hinzufügen
- Partner hinzufügen
- Geschwister hinzufügen
- Beziehungstyp wählen
- Sicherheit wählen
- Kommentar zur Beziehung
- Quelle/Bibelstelle später vorbereiten

### UI

In der Personenakte:

```text
Familie
Eltern
Partner
Kinder
Geschwister
Weitere Beziehungen
```

### Akzeptanzkriterien

- Beziehung zwischen zwei Personen kann gespeichert werden
- Beziehung erscheint bei beiden Personen
- Beziehung kann bearbeitet werden
- Beziehung kann archiviert werden
- Sicherheit wird angezeigt
- keine doppelten Beziehungen ohne Warnung

### Codex-Aufgabe

```text
Füge Beziehungen zwischen Personen hinzu. Unterstütze ParentChild, Spouse, Sibling, AdoptiveParent, LegalParent, TribeMember, UnknownRelated und Custom. Jede Beziehung braucht CertaintyLevel und Kommentar. Zeige Beziehungen in der deutschen Personenakte an.
```

---

## 9. Phase 6 – Einfache Stammbaumansicht Version 0.3

### Ziel

Die erfassten Beziehungen werden sichtbar.

### Ergebnis

Erster interaktiver Stammbaum.

### Funktionen

- Personenkarten anzeigen
- Bild, Name, Lebenszeitraum
- Eltern-Kind-Verbindungen
- Partner-Verbindungen
- Fokusperson wählen
- Zoom
- Verschieben
- Person anklicken
- zur Personenakte springen

### Erstes Layout

Für den Anfang reicht ein einfaches Layout:

```text
Eltern oben
Fokusperson Mitte
Kinder unten
Partner daneben
```

Später kann ein besserer Algorithmus folgen.

### Akzeptanzkriterien

- Stammbaum zeigt Fokusperson
- Eltern werden angezeigt
- Partner werden angezeigt
- Kinder werden angezeigt
- Klick auf Karte öffnet Person
- Zoom/Pan funktioniert
- unsichere Beziehungen werden anders dargestellt

### Codex-Aufgabe

```text
Erstelle eine einfache Stammbaumansicht als Avalonia-View. Zeige eine Fokusperson mit Eltern, Partnern und Kindern. Verwende Personenkarten mit Bild, Name und Lebenszeit. Unterstütze Zoom und Verschieben. Unsichere Beziehungen sollen optisch unterscheidbar sein.
```

---

## 10. Phase 7 – Ereignisse und Bibelstellen Version 0.4

### Ziel

Personen erhalten Ereignisse und Bibelstellen.

### Ergebnis

Die App wird zum Studienwerkzeug.

### Ereignisfunktionen

- Ereignis erstellen
- Ereignistyp
- Datum/Zeitraum
- Ort optional
- beteiligte Personen
- Beschreibung
- Kommentar
- Sicherheit
- Medien optional

### Bibelstellenfunktionen

- Übersetzung
- Buch
- Kapitel
- Verse
- eigene Zusammenfassung
- eigener Auszug optional
- Kommentar
- Verknüpfung mit Person/Ereignis/Ort

### Akzeptanzkriterien

- Ereignis kann erstellt werden
- Ereignis kann mit Person verbunden werden
- Bibelstelle kann gespeichert werden
- Bibelstelle kann mit Ereignis verbunden werden
- Personenakte zeigt Ereignisse und Bibelstellen
- keine Volltexte werden automatisch eingefügt

### Codex-Aufgabe

```text
Füge Event und BibleReference zur App hinzu. Ereignisse sollen mit Personen verknüpft werden können. Bibelstellen werden nur als Referenzen mit optionaler eigener Zusammenfassung oder eigenem Auszug gespeichert. Keine vorbefüllten Bibeltexte.
```

---

## 11. Phase 8 – Mediathek und PDF-Anhänge Version 0.5

### Ziel

Bilder und PDFs können sauber verwaltet werden.

### Ergebnis

Die App wird zum Studienarchiv.

### Funktionen

- Medien importieren
- Dateien in Projektordner kopieren
- Medien Personen zuordnen
- Medien Orten zuordnen
- Medien Ereignissen zuordnen
- PDFs anhängen
- Bildvorschau
- Dateipfad prüfen
- fehlende Datei melden

### Akzeptanzkriterien

- Bild kann einer Person zugeordnet werden
- PDF kann einer Person zugeordnet werden
- Medien werden in Projektordner kopiert
- Projekt bleibt portabel
- fehlende Dateien werden erkannt
- Medienliste ist durchsuchbar

### Codex-Aufgabe

```text
Erstelle eine Mediathek für Bilder, PDFs und andere Dateien. Dateien sollen beim Import in den Projektordner kopiert und als MediaFile gespeichert werden. Medien können mit Personen, Ereignissen und Orten verknüpft werden.
```

---

## 12. Phase 9 – Zeitstrahl Version 0.6

### Ziel

Ereignisse können chronologisch erkundet werden.

### Ergebnis

Personen und Zeitabschnitte werden auswertbar.

### Funktionen

- Zeitstrahl einer Person
- globale Timeline
- Filter nach Person
- Filter nach Zeitraum
- Filter nach Ereignistyp
- unsichere Daten markieren
- Ereignisdetails öffnen

### Akzeptanzkriterien

- Person zeigt Lebensereignisse chronologisch
- Ereignisse ohne exaktes Datum werden sinnvoll einsortiert oder separat angezeigt
- unsichere Angaben sind sichtbar
- Klick öffnet Ereignis
- Timeline bleibt auch bei vielen Ereignissen bedienbar

### Codex-Aufgabe

```text
Erstelle eine Timeline-Ansicht. Sie soll Ereignisse einer Person chronologisch anzeigen und unsichere Datumsangaben sichtbar machen. Ereignisse ohne exaktes Datum sollen nicht verloren gehen, sondern separat oder in geschätzter Reihenfolge erscheinen.
```

---

## 13. Phase 10 – Orte Version 0.7

### Ziel

Orte können erfasst und mit Personen/Ereignissen verbunden werden.

### Ergebnis

Grundlage für Karten und Routen.

### Funktionen

- Ort anlegen
- historischer Name
- moderner Name
- alternative Namen
- Koordinaten
- Unsicherheit
- Beschreibung
- verknüpfte Ereignisse
- verknüpfte Personen
- verknüpfte Bibelstellen

### Akzeptanzkriterien

- Ort kann gespeichert werden
- Ort kann mit Ereignis verbunden werden
- Ort kann mit Person verbunden werden
- Orte ohne Koordinaten sind erlaubt
- Orte mit unsicherer Lokalisierung sind möglich
- Ortsliste ist durchsuchbar

### Codex-Aufgabe

```text
Baue eine Ortsverwaltung mit historischen Namen, modernen Namen, alternativen Namen, Koordinaten, Beschreibung und Lokalisierungs-Sicherheit. Orte sollen mit Personen und Ereignissen verbunden werden können.
```

---

## 14. Phase 11 – Erste Kartenansicht Version 0.8

### Ziel

Orte werden auf einer Karte sichtbar.

### Ergebnis

Geografische Studienansicht.

### Funktionen

- Karte anzeigen
- Marker für Orte
- Marker anklicken
- Detailfenster
- Filter nach Zeitraum oder Tag
- Sprung vom Ort zur Ortsakte
- Sprung von Ereignis zur Karte

### Akzeptanzkriterien

- Orte mit Koordinaten erscheinen auf Karte
- Orte ohne Koordinaten erscheinen in separater Liste
- Marker öffnen Details
- Karte bleibt bedienbar
- spätere Routenintegration ist vorbereitet

### Codex-Aufgabe

```text
Erstelle eine erste Kartenansicht. Orte mit Koordinaten sollen als Marker angezeigt werden. Beim Klick auf einen Marker sollen historischer Name, moderner Name, Beschreibung und verknüpfte Ereignisse sichtbar werden. Routen noch nicht umsetzen, aber vorbereiten.
```

---

## 15. Phase 12 – Routen Version 0.9

### Ziel

Reisen werden als Stationen und Linien sichtbar.

### Ergebnis

Biblische Reisen können rekonstruiert werden.

### Funktionen

- Route anlegen
- Route mit Person verbinden
- Route mit Ereignis verbinden
- Stationen hinzufügen
- Reihenfolge ändern
- Bibelstellen pro Station
- Kommentare pro Station
- Route auf Karte anzeigen

### Akzeptanzkriterien

- Route kann gespeichert werden
- Stationen können sortiert werden
- Route erscheint auf Karte
- Route kann gefiltert werden
- Stationen können Ereignisse haben
- Unsicherheit wird angezeigt

### Codex-Aufgabe

```text
Füge Routen und Routenstationen hinzu. Eine Route soll aus sortierten Orten bestehen und mit einer Person oder einem Ereignis verbunden werden können. Zeige die Route als Linie auf der Karte und die Stationen in einer Liste.
```

---

## 16. Phase 13 – Karte + Zeitsteuerung Version 0.10

### Ziel

Karte und Zeitstrahl arbeiten zusammen.

### Ergebnis

Dynamische historische Studienansicht.

### Funktionen

- Zeitregler unter Karte
- sichtbare Orte nach Zeitraum
- sichtbare Ereignisse nach Zeitraum
- sichtbare Routen nach Zeitraum
- Chronologiemodell wählen
- Sicherheit filtern

### Akzeptanzkriterien

- Zeitfilter beeinflusst Karte
- Ereignisse erscheinen je nach Zeitraum
- Routen erscheinen je nach Zeitraum
- unsichere Daten werden nicht falsch als exakt dargestellt
- Chronologiemodell kann berücksichtigt werden

### Codex-Aufgabe

```text
Verbinde Kartenansicht und Timeline-Filter. Die Karte soll Orte, Ereignisse und Routen abhängig vom gewählten Zeitraum anzeigen. Unsichere Datumsangaben sollen sichtbar bleiben und nicht als exakt behandelt werden.
```

---

## 17. Phase 14 – Auswertungen Version 0.11

### Ziel

Einzelne Personen und Zeiträume können ausgewertet werden.

### Ergebnis

Die App wird für Studienzusammenfassungen nützlich.

### Funktionen pro Person

- Kurzprofil
- Familienübersicht
- alle Ereignisse
- alle Bibelstellen
- alle Orte
- alle Routen
- alle Medien
- offene Forschungsfragen
- unsichere Angaben
- PDF-Ausgabe später

### Funktionen pro Zeitraum

- Personen im Zeitraum
- Ereignisse im Zeitraum
- Orte im Zeitraum
- Routen im Zeitraum
- Bibelstellen im Zeitraum

### Akzeptanzkriterien

- Personenbericht wird in der App angezeigt
- Daten werden aus verknüpften Tabellen gesammelt
- unsichere Angaben werden gesondert markiert
- offene Forschungsfragen sind sichtbar

### Codex-Aufgabe

```text
Erstelle eine Auswertungsansicht für einzelne Personen. Sammle Familie, Ereignisse, Orte, Bibelstellen, Medien, Routen, Notizen und Forschungsfragen in einer übersichtlichen deutschen Darstellung.
```

---

## 18. Phase 15 – Export/Import Version 0.12

### Ziel

Projekte können gesichert und auf andere Geräte übertragen werden.

### Ergebnis

Portables Projektpaket.

### Funktionen

- Projekt als `.biblestudy` exportieren
- Projektpaket importieren
- Medien einschließen
- Datenbank einschließen
- Manifest erstellen
- Version prüfen
- fehlende Dateien melden

### Paketstruktur

```text
MeinProjekt.biblestudy
  manifest.json
  project.sqlite
  Media/
  Thumbnails/
  Settings/
```

### Akzeptanzkriterien

- Projekt exportiert erfolgreich
- Exportdatei enthält Datenbank und Medien
- Projekt kann in neuem Ordner importiert werden
- importiertes Projekt öffnet korrekt
- fehlerhafte Pakete werden verständlich gemeldet

### Codex-Aufgabe

```text
Implementiere Export und Import eines Projektpakets mit der Endung .biblestudy. Intern soll es ein ZIP-Paket mit manifest.json, project.sqlite, Media, Thumbnails und Settings sein.
```

---

## 19. Phase 16 – Backup und Wiederherstellung Version 0.13

### Ziel

Datenverlust vermeiden.

### Ergebnis

Automatische und manuelle Sicherungen.

### Funktionen

- Backup beim Öffnen
- Backup vor Migrationen
- manuelles Backup
- Backup-Liste
- Wiederherstellung
- alte Backups optional bereinigen

### Akzeptanzkriterien

- Backup wird erzeugt
- Backup lässt sich wiederherstellen
- App erstellt vor riskanten Aktionen automatisch ein Backup
- Fehler werden verständlich angezeigt

### Codex-Aufgabe

```text
Erstelle ein Backup-System für Projektdateien. Vor Datenbankmigrationen und auf Wunsch manuell soll eine Sicherung der SQLite-Datei und der Projekteinstellungen erstellt werden. Baue eine einfache Wiederherstellung ein.
```

---

## 20. Phase 17 – Studienmodus und Bearbeitungsmodus Version 0.14

### Ziel

Sicheres Erkunden ohne versehentliche Änderungen.

### Ergebnis

Zwei Arbeitsmodi.

### Bearbeitungsmodus

- Daten ändern
- Personen bearbeiten
- Beziehungen ändern
- Ereignisse bearbeiten
- Medien importieren

### Studienmodus

- nur ansehen
- Stammbaum erkunden
- Karten nutzen
- Timeline nutzen
- PDFs öffnen
- keine Bearbeitung

### Akzeptanzkriterien

- Modus kann gewechselt werden
- Studienmodus deaktiviert Bearbeitungsaktionen
- Modus ist sichtbar
- App merkt sich zuletzt verwendeten Modus

### Codex-Aufgabe

```text
Füge einen Studienmodus und einen Bearbeitungsmodus hinzu. Im Studienmodus sollen Bearbeitungsbuttons deaktiviert sein, damit man die Daten nur erkunden kann.
```

---

## 21. Phase 18 – Änderungsverlauf Version 0.15

### Ziel

Forschungsschritte nachvollziehbar machen.

### Ergebnis

AuditLog für wichtige Änderungen.

### Funktionen

- Änderungen an Personen protokollieren
- Änderungen an Beziehungen protokollieren
- Änderungen an Ereignissen protokollieren
- Kommentar zur Änderung optional
- Verlauf anzeigen

### Akzeptanzkriterien

- Änderungen werden gespeichert
- Verlauf ist pro Person einsehbar
- Beziehungshistorie ist nachvollziehbar
- alte Werte sind erkennbar

### Codex-Aufgabe

```text
Erstelle einen einfachen Änderungsverlauf für Personen, Beziehungen und Ereignisse. Speichere EntityType, EntityId, Aktion, alten Wert, neuen Wert, Kommentar und Zeitpunkt.
```

---

## 22. Phase 19 – Datenqualität Version 0.16

### Ziel

Der Benutzer findet Lücken und Unklarheiten leichter.

### Ergebnis

Hilfreiche Prüfungen.

### Prüfungen

- Person ohne Hauptname
- Person ohne Bild
- Beziehung ohne Sicherheitsgrad
- Ereignis ohne Datum
- Ort ohne Koordinaten
- Route ohne Stationen
- Bibelstelle ohne Kapitel/Vers
- Medienpfad fehlt
- mögliche doppelte Person
- Forschungsfrage offen

### Akzeptanzkriterien

- Dashboard zeigt Hinweise
- Hinweise sind nicht blockierend
- Klick führt zur betroffenen Stelle
- Hinweise können gefiltert werden

### Codex-Aufgabe

```text
Erstelle ein Datenqualitätsmodul. Es soll Hinweise zu fehlenden Namen, fehlenden Koordinaten, unvollständigen Bibelstellen, fehlenden Medien und offenen Forschungsfragen anzeigen.
```

---

## 23. Phase 20 – Optische Politur Version 1.0

### Ziel

Die App wird angenehm nutzbar.

### Ergebnis

Stabile persönliche Version 1.0.

### Aufgaben

- einheitliches Design
- bessere Abstände
- Icons
- Startseite aufräumen
- Personenakte schöner machen
- Stammbaum lesbarer machen
- Fehlertexte überarbeiten
- leere Zustände freundlich darstellen
- helle/dunkle Ansicht optional
- Einstellungen verbessern
- Dokumentation schreiben

### Akzeptanzkriterien

- App ist für den persönlichen Einsatz stabil
- neue Projekte sind leicht anzulegen
- Grundfunktionen sind verständlich
- keine kritischen Datenverluste
- Export funktioniert
- Backup funktioniert
- Personen, Beziehungen, Ereignisse, Orte und Medien sind nutzbar

### Codex-Aufgabe

```text
Überarbeite die Oberfläche der App mit Fokus auf ruhige, klare Bedienung. Keine neuen Großfunktionen hinzufügen. Verbessere Layout, leere Zustände, Buttontexte, Abstände, Personenakte und Dashboard.
```

---

## 24. Versionsübersicht

| Version | Schwerpunkt |
|---|---|
| 0.0 | Solution und Grundgerüst |
| 0.1 | Projektdatei und SQLite |
| 0.2 | Personenverwaltung |
| 0.3 | Beziehungen |
| 0.4 | einfacher Stammbaum |
| 0.5 | Ereignisse und Bibelstellen |
| 0.6 | Mediathek und PDFs |
| 0.7 | Timeline |
| 0.8 | Orte |
| 0.9 | Karte |
| 0.10 | Routen |
| 0.11 | Karte + Zeitsteuerung |
| 0.12 | Auswertungen |
| 0.13 | Export/Import |
| 0.14 | Backup |
| 0.15 | Studienmodus |
| 0.16 | Änderungsverlauf |
| 1.0 | stabile persönliche Version |

---

## 25. MVP-Empfehlung

Der erste wirklich sinnvolle MVP sollte enthalten:

1. Projekt anlegen
2. Projekt öffnen
3. Person anlegen
4. Person bearbeiten
5. Bild hinzufügen
6. Eltern/Kinder/Partner verknüpfen
7. einfache Stammbaumansicht
8. lokale Speicherung
9. Backup
10. Projekt exportieren

Noch nicht notwendig für den MVP:

- Karte
- Routen
- vollständige Timeline
- PDF-Berichte
- mehrere Chronologien im UI
- schöne Animationen
- automatische Bibeltexte

---

## 26. Git-Arbeitsweise

### 26.1 Branches

Für den Anfang reicht:

```text
main
```

Später:

```text
feature/person-management
feature/relationships
feature/tree-view
feature/events
feature/maps
```

### 26.2 Commit-Stil

Beispiele:

```text
Initial project structure
Add core models
Add person management view
Add relationship repository
Add simple tree view
Add project export package
```

### 26.3 Nach jedem Meilenstein

Immer ausführen:

```powershell
dotnet build
dotnet test
git status
```

Dann committen:

```powershell
git add .
git commit -m "Add person management"
```

---

## 27. Praktische Codex-Regeln

### 27.1 Gute Codex-Aufgaben

Klein und konkret:

```text
Erstelle nur die Modelle und Enums. Keine UI.
```

```text
Erstelle nur die ViewModel-Logik für Personenliste und Personendetails.
```

```text
Füge Tests für DateInfo-Formatierung hinzu.
```

```text
Behebe diesen Buildfehler und ändere möglichst wenig.
```

### 27.2 Schlechte Codex-Aufgaben

Zu groß:

```text
Baue die komplette App.
```

Zu unklar:

```text
Mach das schöner.
```

Besser:

```text
Verbessere die Personenakte: Bild links, Name und Lebensdaten oben, Tabs darunter. Keine Logik ändern.
```

---

## 28. Risiken und Gegenmaßnahmen

| Risiko | Gegenmaßnahme |
|---|---|
| Projekt wird zu groß | kleine Versionen |
| Datenmodell wird chaotisch | Core-Modelle früh sauber definieren |
| Stammbaumlayout wird schwierig | erst einfache Ansicht |
| Kartenmodul frisst Zeit | erst spät einbauen |
| Bibeltexte rechtlich schwierig | nur Referenzen speichern |
| Datenverlust | Backup früh einbauen |
| Codex ändert zu viel | kleine Aufgaben und Git-Commits |
| App wird unübersichtlich | Dashboard und klare Navigation |

---

## 29. Reihenfolge der ersten drei Codex-Aufgaben

### Aufgabe 1

```text
Erstelle eine neue Avalonia-Solution namens BibleStudyGenealogy mit den Projekten App, Core, Infrastructure, Rendering, Maps und Tests. Verwende deutsche UI-Texte, aber englische Projekt- und Klassennamen. Richte Projektverweise ein und stelle sicher, dass dotnet build funktioniert.
```

### Aufgabe 2

```text
Erstelle im Core-Projekt die Modelle Person, Relationship, Event, Place, BibleReference, MediaFile, Note, ResearchQuestion, ChronologyModel und DateInfo sowie passende Enums. Keine Daten vorbefüllen.
```

### Aufgabe 3

```text
Baue einen Startbildschirm für die App mit den Optionen Neues Projekt erstellen, Projekt öffnen und Zuletzt verwendet. Beim Erstellen eines Projekts sollen project.sqlite sowie Media-, Thumbnails- und Backups-Ordner angelegt werden.
```

---

## 30. Definition von Version 1.0

Version 1.0 ist erreicht, wenn die App für den persönlichen Einsatz stabil genug ist.

Mindestumfang:

- lokale Projekte
- SQLite-Datenbank
- Personen
- Beziehungen
- einfacher Stammbaum
- Ereignisse
- Bibelstellenreferenzen
- Orte
- Medien/PDFs
- Notizen
- Forschungsfragen
- Timeline
- erste Kartenansicht
- Export/Import
- Backup
- Studienmodus
- verständliche deutsche UI

Nicht zwingend für 1.0:

- perfekte historische Karten
- automatische Routenrekonstruktion
- automatische Bibeltextintegration
- Cloud-Synchronisation
- Mehrbenutzerbetrieb
- mobile App

---

## 31. Direkter nächster Schritt

Nach der Installation sollte zuerst nur Folgendes passieren:

1. Projektordner anlegen
2. Git initialisieren
3. Avalonia-Solution erstellen
4. Core-, Infrastructure-, Rendering-, Maps- und Tests-Projekte hinzufügen
5. Build testen
6. ersten Commit erstellen

Erst danach beginnt die eigentliche App-Logik.

Der erste Commit sollte nur das Grundgerüst enthalten.

Beispiel:

```powershell
git init
git add .
git commit -m "Initial Avalonia solution structure"
```
