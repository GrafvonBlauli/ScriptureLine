# ScriptureLine Entwicklungsprotokoll

**Zweck:** Laufendes Protokoll der Arbeitsschritte an ScriptureLine.  
**Zeitzone:** Europe/Berlin  
**Zuletzt aktualisiert:** 2026-05-22 00:57:41 +02:00  
**Aktueller Branch:** `main`  
**GitHub-Remote:** `origin -> https://github.com/GrafvonBlauli/ScriptureLine.git`  

## Regel Für Die Weitere Pflege

- Dieses Protokoll wird vor jedem Commit aktualisiert.
- Build-, Test-, Commit- und Push-Ergebnisse werden mit Datum/Uhrzeit festgehalten.
- Nicht exakt aus Git oder Dateisystem belegbare frühe Schritte werden als Sitzungsabschnitte dokumentiert.

## Chronologie

### 2026-05-21, ca. 19:20 +02:00 - Projektgrundlage geprüft

**Kategorie:** Planung / Dokumentation  
**Aktion:** Die vorhandenen Markdown-Dateien im Projekt wurden gelesen.  
**Geänderte Bereiche:** Keine.  
**Ergebnis:** Das Projektziel wurde aus `Bibelstudienprogramm_Konzept.md` und `Bibelstudienprogramm_Roadmap.md` übernommen: lokale Avalonia-Desktop-App, deutsche UI, englische Codebegriffe, SQLite, keine vorbefüllten Bibeldaten.  
**Offene Punkte:** App-Grundgerüst, lokale Projektanlage, Personenverwaltung und GitHub-Synchronisation waren noch nicht vorhanden.

### 2026-05-21 - Designrichtung festgelegt

**Kategorie:** Design  
**Aktion:** Die Designrichtung wurde anhand der Referenzbilder abgestimmt.  
**Geänderte Bereiche:** Keine.  
**Ergebnis:** Mischung aus ruhigem Heritage-Stil und warmem Artisan-Stil: dunkle App-Schale, Pergamentflächen, Terrakotta-Akzente, Salbei/Creme und illustrative historische Landschaften.  
**Offene Punkte:** Design musste noch in Avalonia-Theme und Dashboard übertragen werden.

### 2026-05-21 - Avalonia-Grundgerüst erstellt

**Kategorie:** Projektstruktur / UI  
**Aktion:** Eine .NET/Avalonia-Solution mit App, Core, Infrastructure, Rendering, Maps und Tests wurde angelegt.  
**Geänderte Bereiche:** `src/`, `tests/`, `BibleStudyGenealogy.slnx`.  
**Ergebnis:** Erste Desktop-App-Struktur, zentrale Theme-Datei und Dashboard-Prototyp mit dunkler Navigation und warmen Kartenflächen.  
**Verifikation:** `dotnet build` lief erfolgreich mit 0 Warnungen und 0 Fehlern. `dotnet test` lief erfolgreich mit dem damaligen Beispieltest.

### 2026-05-21 bis 2026-05-22 - Lokale Projektgrundlage erstellt

**Kategorie:** Infrastruktur / SQLite  
**Aktion:** Lokales Projektformat mit `manifest.json`, `project.sqlite`, Medien-, Thumbnail- und Backup-Ordnern wurde umgesetzt.  
**Geänderte Bereiche:** Core-Projektmodelle, `LocalProjectService`, erste Tests.  
**Ergebnis:** Die App kann lokale Projekte anlegen und öffnen. Projektsettings werden in SQLite gespeichert.  
**Offene Punkte:** Ein Test zeigte zunächst eine SQLite-Dateisperre beim Cleanup.

### 2026-05-22 00:17:07 +02:00 - Initialer Commit

**Kategorie:** Git  
**Aktion:** Lokales Git-Repository wurde initialisiert und der erste Commit erstellt.  
**Commit:** `58db29c Initial ScriptureLine foundation`  
**Geänderte Bereiche:** Solution, Dokumentation, App-Theme, Dashboard, Core-Modelle, Infrastructure-Service, Tests, `.gitignore`.  
**Ergebnis:** Sauberer initialer lokaler Stand auf Branch `main`.  
**Offene Punkte:** GitHub-Push scheiterte später, weil das Remote-Repository noch nicht vorhanden oder nicht erreichbar war.

### 2026-05-22 - GitHub-Remote vorbereitet

**Kategorie:** GitHub  
**Aktion:** Remote `origin` wurde auf `https://github.com/GrafvonBlauli/ScriptureLine.git` gesetzt.  
**Ergebnis:** Lokales Repository ist für GitHub vorbereitet.  
**Blocker:** `git push -u origin main` meldete `Repository not found`. Das private GitHub-Repository muss noch erstellt oder erreichbar gemacht werden.

### 2026-05-22 00:24:58 +02:00 - Personenverwaltungs-Grundlage

**Kategorie:** Personenverwaltung / SQLite / UI  
**Aktion:** Personen-Repository und erste Personen-UI wurden umgesetzt.  
**Commit:** `0922e2f Add person management foundation`  
**Geänderte Bereiche:** `IPersonRepository`, `PersonRepository`, Dashboard-Personenformular, Personenliste, Suche, deutsche Enum-Anzeigen, Tests.  
**Ergebnis:** Personen können gespeichert, gesucht, geladen und aktualisiert werden. Dashboard-Zähler liest aus SQLite.  
**Verifikation:** `dotnet build` erfolgreich mit 0 Warnungen und 0 Fehlern. `dotnet test --no-build` erfolgreich mit 4 Tests.  
**Offene Punkte:** GitHub-Push scheiterte weiterhin mit `Repository not found`.

### 2026-05-22 00:29:00 +02:00 - Dashboard-Assets eingeordnet

**Kategorie:** Design / Assets  
**Aktion:** Importierte PNG-Dateien wurden im Projekt gefunden und eingeordnet.  
**Geänderte Bereiche:** `src/BibleStudyGenealogy.App/Assets/Images/Dashboardpicture.png`, `docs/design/Overlay.png`.  
**Ergebnis:** `Dashboardpicture.png` ist als Startseitenbild vorgesehen. `Overlay.png` dient als Designreferenz.  
**Offene Punkte:** Dashboard-XAML musste noch auf das echte Bild umgestellt werden.

### 2026-05-22 00:31:41 +02:00 - Protokoll angelegt und Dashboard-Umbau begonnen

**Kategorie:** Dokumentation / UI  
**Aktion:** Dieses Entwicklungsprotokoll wurde angelegt. Das Dashboard wurde auf ein echtes Hero-Bild und Overlay-nahe Kartenstruktur vorbereitet.  
**Geänderte Bereiche:** `Entwicklungsprotokoll.md`, App-Projektdatei, `MainWindow.axaml`, `MainWindow.axaml.cs`.  
**Ergebnis:** Die nächsten Schritte sind Build, Test, abschließende Protokollaktualisierung, Commit und erneuter GitHub-Push-Versuch.  
**Offene Punkte:** Build/Test/Commit/Push-Ergebnis stehen noch aus.

### 2026-05-22 00:34:00 +02:00 - Dashboard-Assets eingebunden und geprüft

**Kategorie:** UI / Assets / Qualitätssicherung  
**Aktion:** Das Dashboard wurde auf `Dashboardpicture.png` als echtes Avalonia-Asset umgestellt. `Overlay.png` wurde als Designreferenz nach `docs/design` verschoben.  
**Geänderte Bereiche:** `src/BibleStudyGenealogy.App/BibleStudyGenealogy.App.csproj`, `src/BibleStudyGenealogy.App/MainWindow.axaml`, `src/BibleStudyGenealogy.App/MainWindow.axaml.cs`, `src/BibleStudyGenealogy.App/Assets/Images/Dashboardpicture.png`, `docs/design/Overlay.png`.  
**Ergebnis:** Dashboard-Hero nutzt das importierte Startbild. Die Dashboard-Struktur wurde näher an die Overlay-Referenz geführt: Hero, Projektübersicht, zuletzt bearbeitet, Zeitleiste, Karte, offene Aufgaben, Forschungsfragen und Schnellzugriff.  
**Verifikation:** `dotnet build` erfolgreich mit 0 Warnungen und 0 Fehlern. `dotnet test --no-build` erfolgreich mit 4 Tests.  
**Offene Punkte:** Commit und GitHub-Push-Versuch stehen noch aus.

### 2026-05-22 00:32:14 +02:00 - Dashboard-Assets committed

**Kategorie:** Git / UI / Assets  
**Aktion:** Ein zusätzlicher Commit wurde im lokalen Verlauf festgestellt.  
**Commit:** `4c667ad Initial commit`  
**Geänderte Bereiche:** `docs/design/Overlay.png`, `src/BibleStudyGenealogy.App/Assets/Images/Dashboardpicture.png`, App-Projektdatei, `MainWindow.axaml`, `MainWindow.axaml.cs`.  
**Ergebnis:** Dashboardbild und Overlay-Referenz sind bereits im Git-Verlauf erfasst.  
**Hinweis:** Der Arbeitsbaum ist danach bis auf dieses Protokoll sauber.

### 2026-05-22 00:36:10 +02:00 - Protokoll finalisiert

**Kategorie:** Dokumentation / Qualitätssicherung  
**Aktion:** Das Protokoll wurde an den tatsächlichen Git-Verlauf angepasst und um den Commit `4c667ad` ergänzt.  
**Geänderte Bereiche:** `Entwicklungsprotokoll.md`.  
**Ergebnis:** Das Protokoll enthält jetzt die bisherige Historie, die Bildintegration, die Build-/Test-Ergebnisse und den aktuellen GitHub-Blocker.  
**Offene Punkte:** Protokoll committen und GitHub-Push erneut versuchen.

### 2026-05-22 00:37:49 +02:00 - GitHub-Synchronisation erfolgreich

**Kategorie:** GitHub / Git  
**Aktion:** Der Branch `main` wurde erfolgreich nach GitHub gepusht.  
**Commit vor Push:** `d3415f3 Add dashboard assets and development log`  
**Remote:** `https://github.com/GrafvonBlauli/ScriptureLine.git`  
**Ergebnis:** `main` trackt jetzt `origin/main`. GitHub meldete `4c667ad..d3415f3 main -> main`.  
**Offene Punkte:** Diese Protokollaktualisierung wird als eigener kleiner Folgecommit gespeichert und anschließend erneut gepusht.

### 2026-05-22 00:47:22 +02:00 - Beziehungsverwaltung umgesetzt

**Kategorie:** Beziehungen / SQLite / UI / Tests  
**Aktion:** Version 0.2-Grundlage für Beziehungen wurde implementiert. Personen können über Beziehungen verbunden werden.  
**Geänderte Bereiche:** Core-Modelle `Relationship`, `RelationshipType`, `RelationshipDirection`; Infrastructure-Repositories `IRelationshipRepository`, `RelationshipRepository`; SQLite-Schema `Relationships`; Dashboard-/Personen-UI; Beziehungstests.  
**Ergebnis:** Beziehungen können gespeichert, aktualisiert, für beide Personen geladen und als Familienliste sowie einfache Stammbaum-Vorschau angezeigt werden. Der Dashboard-Zähler für Beziehungen liest echte SQLite-Daten. Doppelte Beziehungen werden verhindert.  
**Verifikation:** `dotnet build` erfolgreich mit 0 Warnungen und 0 Fehlern. `dotnet test --no-build` erfolgreich mit 9 Tests.  
**Offene Punkte:** Commit `Add relationship management foundation` erstellen und nach GitHub pushen.

### 2026-05-22 00:57:41 +02:00 - Stammbaum-Vorschau ausgebaut

**Kategorie:** Stammbaum / Beziehungen / Rendering / UI / Tests  
**Aktion:** Beziehungen wurden bearbeitbar und archivierbar gemacht. Zusätzlich wurde ein testbarer `FamilyTreeBuilder` im Rendering-Projekt ergänzt.  
**Geänderte Bereiche:** Core-Modell `RelationshipStatus`, `Relationship` mit Status, `RelationshipRepository` mit Archivierung und Aktiv-Filter, SQLite-Schema-Migration, `FamilyTreeBuilder` samt DTOs, UI für Beziehungsauswahl/-bearbeitung/-archivierung und Stammbaum-Vorschau.  
**Ergebnis:** Aktive Beziehungen erscheinen in Listen und Zählern. Archivierte Beziehungen bleiben geladen, werden aber aus Standardlisten und Zählern ausgeblendet. Die App zeigt Eltern, Fokusperson, Partner, Kinder sowie weitere/unsichere Beziehungen als einfache Stammbaum-Vorschau.  
**Verifikation:** `dotnet build` erfolgreich mit 0 Warnungen und 0 Fehlern. `dotnet test --no-build` erfolgreich mit 13 Tests.  
**Offene Punkte:** Commit `Add simple family tree preview` erstellen und nach GitHub pushen.
