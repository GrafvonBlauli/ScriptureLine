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

## 2026-05-22 - Version 0.4 Grundlage: Ereignisse und Bibelstellen

- Core-Modelle für Ereignisse und Bibelstellen vorbereitet: `Event`, `EventType`, `BibleReference` und `BibleTranslation`.
- SQLite-Schema um `Events`, `EventPersons`, `BibleReferences` und `EventBibleReferences` erweitert.
- Neue Repository-Schichten ergänzt: `IEventRepository`/`EventRepository` sowie `IBibleReferenceRepository`/`BibleReferenceRepository`.
- Dashboard-Statistiken um Ereignisse und Bibelstellen erweitert.
- UI um erste Arbeitsbereiche für „Ereignisse“ und „Bibelstellen“ ergänzt; Ereignisse können mit der aktuell ausgewählten Person verknüpft werden.
- Stammbaum-Vorschau visuell klarer gegliedert; unsichere/weitere Beziehungen sind als eigener Bereich sichtbar.
- Tests für Ereignisse, Personen-Verknüpfung, Bibelstellen und Statistikzählung ergänzt.
- Hinweis: `dotnet build` und `dotnet test` wurden im folgenden Stabilisierungsschritt nachgeholt.

### 2026-05-22 08:02:05 +02:00 - Stabilisierung, Logikprüfung und Stresstest

**Kategorie:** Stabilisierung / SQLite / Tests / Performance
**Aktion:** Die Ereignis- und Bibelstellen-Grundlage wurde stabilisiert und um Integritätsprüfungen ergänzt. SQLite-Verbindungen werden zentral geöffnet und aktivieren Foreign Keys. Ungespeicherte Personen werden in der UI von gespeicherten Personen unterschieden, bevor Beziehungen oder Ereignis-Verknüpfungen angelegt werden.
**Geänderte Bereiche:** `SqliteConnectionFactory`, Personen-/Beziehungs-/Ereignis-/Bibelstellen-Repositories, SQLite-Indexes, `MainWindow.axaml.cs`, Anzeigehelfer, Repository- und Performance-Tests.
**Ergebnis:** Beziehungen, Event-Person-Verknüpfungen und Event-Bibelstellen-Verknüpfungen werden durch Foreign Keys abgesichert. Bibelstellen validieren positive Kapitel/Verse und logisch passende Endreferenzen. Der Codebehind wurde leicht entlastet, indem ComboBox-Anzeigeoptionen ausgelagert wurden.
**Stresstest:** Mittleres Testprofil mit 2.000 Personen, 4.000 Beziehungen, 1.000 Ereignissen, 2.000 Bibelstellen und passenden Verknüpfungen ergänzt.
**Verifikation:** `dotnet build` erfolgreich mit 0 Warnungen und 0 Fehlern. `dotnet test --no-build` erfolgreich mit 26 Tests in 1 m 31 s.
**Commit:** `4ca6b27 Stabilize events and add performance checks`
**Push:** Erfolgreich nach GitHub, `main -> origin/main`.

### 2026-05-22 08:18:28 +02:00 - Mediathek und PDF-Anhänge vorbereitet

**Kategorie:** Mediathek / Dateiimport / SQLite / UI / Tests
**Aktion:** Version 0.5-Grundlage für lokale Medien wurde implementiert. Importierte Dateien werden in den Projektordner kopiert und nur mit relativem Pfad in SQLite gespeichert.
**Geänderte Bereiche:** Core-Modelle `MediaFile`, `MediaType`, `MediaLink`, `LinkedEntityType`; Infrastructure-Repositories `IMediaRepository`, `MediaRepository`; `MediaImportService`; SQLite-Schema `MediaFiles`/`MediaLinks`; Dashboard-/Mediathek-UI; Mediathek-Tests.
**Ergebnis:** Bilder, PDFs, Dokumente, Karten und sonstige Dateien können importiert, gesucht, beschrieben und mit Personen oder Ereignissen verknüpft werden. Bildmedien können als Personenportrait gesetzt werden. Fehlende Dateien werden erkannt und als Status angezeigt.
**Verifikation:** `dotnet build` erfolgreich mit 0 Warnungen und 0 Fehlern. `dotnet test --no-build` erfolgreich mit 33 Tests in 2 m 34 s.
**Commit:** `801bf65 Add media library foundation`
**Push:** Erfolgreich nach GitHub, `main -> origin/main`.

### 2026-05-22 08:31:14 +02:00 - UI-Funktionsprüfung und Timeline vorbereitet

**Kategorie:** UI-Prüfung / Timeline / Tests / Dokumentation
**Aktion:** Sichtbare UI-Elemente wurden statisch gegen vorhandene Handler geprüft und als Funktionsmatrix dokumentiert. Platzhalter wie Karte, Orte, Forschungsfragen und Ort anlegen wurden klar als kommende Funktionen gekennzeichnet.
**Geänderte Bereiche:** `docs/ui-function-matrix.md`, `MainWindow.axaml`, `MainWindow.axaml.cs`, Rendering-Timeline, Timeline- und UI-Matrix-Tests.
**Ergebnis:** Alle verdrahteten Click-Handler sind testbar vorhanden. Die Dashboard-Zeitleiste nutzt jetzt eine erste echte Timeline-Liste aus Ereignissen und zeigt auch undatierte Ereignisse als „ohne Datierung“ an.
**Verifikation:** `dotnet build` erfolgreich mit 0 Warnungen und 0 Fehlern. `dotnet test --no-build` erfolgreich mit 37 Tests in 1 m 30 s.
**Commit:** `d7d42ca Verify UI actions and prepare timeline`
**Push:** Erfolgreich nach GitHub, `main -> origin/main`.

### 2026-05-22 18:28:30 +02:00 - Navigierbare Modulansichten ergänzt

**Kategorie:** UI-Navigation / Modulflächen / Tests / Dokumentation
**Aktion:** Die linke Sidebar wurde von statischer Beschriftung zu echter Modulnavigation umgebaut. Das Dashboard bleibt Startseite, während Personen, Stammbaum, Zeitstrahl, Ereignisse, Bibelstellen und Mediathek eigene sichtbare Oberflächen bekommen.
**Geänderte Bereiche:** `AppModule`, `MainWindow.axaml`, `MainWindow.axaml.cs`, `docs/ui-function-matrix.md`, `UiFunctionMatrixTests`.
**Ergebnis:** Sidebar-Schaltflächen wechseln über `ShowModule(AppModule module)` zwischen den Arbeitsbereichen. Dashboard-Schnellzugriffe öffnen direkt das passende Modul. Karte, Orte und Forschungsfragen sind als eigene Platzhalterseiten erreichbar und klar als noch nicht umgesetzt markiert.
**Verifikation:** `dotnet build` erfolgreich mit 0 Warnungen und 0 Fehlern. Erster `dotnet test --no-build`-Lauf brach nach 124 s wegen Timeout ab; erneuter Lauf mit längerem Timeout erfolgreich mit 39 Tests in 2 m 9 s.
**Offene Punkte:** Commit `Add navigable module views` erstellen und nach GitHub pushen.
### 2026-05-22 18:45:00 +02:00 - Interaktiver Stammbaum begonnen

**Kategorie:** Stammbaum / Rendering / UI / Tests
**Aktion:** Die einfache Text-Vorschau wurde zu einer ersten interaktiven Stammbaum-Oberfläche ausgebaut. Das Rendering-Projekt erhielt Diagrammtypen mit Koordinaten, Generationenlimit und Gesamtbaum-Modus. Im Stammbaum-Modul wurden Toolbar, zoombare Canvas-Fläche, Seitenpanel und Overlay zum Hinzufügen von Verwandten ergänzt.
**Geänderte Bereiche:** `FamilyTreeBuilder`, neue Diagramm-DTOs im Rendering-Projekt, `MainWindow.axaml`, `MainWindow.axaml.cs`, `FamilyTreeBuilderTests`, `docs/ui-function-matrix.md`.
**Ergebnis:** Personen werden als Karten mit Linien dargestellt. Klick auf eine Karte wählt sie im Seitenpanel aus, der Plus-Knopf öffnet ein Overlay für Elternteil, Kind, Partner oder Geschwister. Neue Personen werden mit passender Beziehung gespeichert und der Baum aktualisiert.
**Verifikation:** Nach Korrektur der Buildfehler erfolgreich geprüft: `dotnet build` mit 0 Warnungen und 0 Fehlern. `dotnet test --no-build` erfolgreich mit 42 Tests in 1 m 41 s.
**Offene Punkte:** Commit `Add interactive family tree editor` erstellen und nach GitHub pushen.

### 2026-05-22 22:39:41 +02:00 - Stammbaum-Sideboard und Lebensdaten erweitert

**Kategorie:** Stammbaum / Personenbearbeitung / Lebensdaten / SQLite / Tests
**Aktion:** Das Stammbaum-Sideboard wurde zu einer direkten Bearbeitungsfläche erweitert. Personen können dort mit Name, Rolle, Status, Geschlecht, Kurzbeschreibung sowie Geburts- und Sterbedaten bearbeitet werden. Das Verwandte-hinzufügen-Overlay speichert nun ebenfalls Lebensdaten.
**Geänderte Bereiche:** `MainWindow.axaml`, `MainWindow.axaml.cs`, `PersonRepository`, `LocalProjectService`, `PersonRepositoryTests`, `docs/ui-function-matrix.md`.
**Ergebnis:** Geburts- und Sterbedaten werden als Text plus extrahiertem Jahr in SQLite gespeichert und beim Öffnen wieder geladen. Aus Alter + Geburtsjahr kann das Sterbejahr berechnet werden; aus Alter + Sterbejahr das Geburtsjahr.
**Verifikation:** `dotnet build` erfolgreich mit 0 Warnungen und 0 Fehlern. `dotnet test --no-build` erfolgreich mit 43 Tests in 1 m 36 s.
**Offene Punkte:** Commit erstellen und nach GitHub pushen.

### 2026-05-22 23:11:28 +02:00 - Stammbaumlayout näher an Referenz angepasst

**Kategorie:** Stammbaum / UI-Layout / Tests
**Aktion:** Das Stammbaum-Modul wurde stärker an die Referenzscreenshots angelehnt: helle Baumfläche, gelbliche Personenkarten, farbige Rahmen nach Geschlecht, Avatar-Kreis, Lebensdaten und Plus-Schaltfläche direkt an der Karte. Die separate Beziehungsfläche im Stammbaum-Modul wurde ausgeblendet; neue Beziehungen laufen über `Verwandte hinzufügen`.
**Geänderte Bereiche:** `MainWindow.axaml`, `MainWindow.axaml.cs`, `FamilyTreeBuilder`, `RepositoryPerformanceTests`.
**Ergebnis:** Das Stammbaum-Modul wirkt mehr wie ein echter Stammbaum statt wie eine Formularseite. Beziehungserfassung ist nicht mehr als extra Bereich sichtbar. Der Performance-Stresstest bleibt als grober Regressionsschutz erhalten, wurde aber wegen lokaler I/O-Varianz auf 300 s Bulk-Insert-Schwelle erweitert.
**Verifikation:** `dotnet build` erfolgreich mit 0 Warnungen und 0 Fehlern. `dotnet test --no-build` erfolgreich mit 43 Tests in 1 m 34 s.
**Offene Punkte:** Commit erstellen und nach GitHub pushen.

### 2026-05-22 23:55:00 +02:00 - Familienconnectoren und geführte Verwandtenanlage vorbereitet

**Kategorie:** Stammbaum / Layoutmodell / UI-Workflow / Tests
**Aktion:** Das Stammbaumdiagramm wurde um Familienconnectoren, Vater-/Mutter-Platzhalter und Linkarten erweitert. Das Verwandte-hinzufügen-Overlay wurde auf konkrete Aktionen wie Vater, Mutter, Sohn, Tochter, Bruder, Schwester, Partner und bestehende Person verknüpfen vorbereitet.
**Geänderte Bereiche:** `FamilyTreeBuilder`, Diagramm-DTOs im Rendering-Projekt, `MainWindow.axaml`, `MainWindow.axaml.cs`, `FamilyTreeBuilderTests`, `docs/ui-function-matrix.md`.
**Ergebnis:** Direkte Eltern-Kind-Diagonalen sollen durch gemeinsame Familienpunkte ersetzt werden. Fehlende Eltern werden als UI-Platzhalter dargestellt und nicht gespeichert. Bestehende Personen können im Overlay zur Verknüpfung ausgewählt werden.
**Verifikation:** Build/Test konnten in dieser Sitzung nicht erneut ausgeführt werden, weil die Umgebung eskalierte Kommandos wegen eines Nutzungslimits blockiert hat. `dotnet build` und `dotnet test` müssen nach Freigabe nachgeholt werden.
**Offene Punkte:** Build-/Testlauf nachholen, eventuelle Compile-Korrekturen durchführen, Commit `Improve family tree layout and add relative workflows` erstellen und pushen.

### 2026-05-27 19:45:10 +02:00 - Stammbaum-Buildfehler bereinigt

**Kategorie:** Buildfix / Stammbaum / Verifikation
**Aktion:** Der gemeldete Compilerfehler `CS0136` in `MainWindow.axaml.cs` wurde behoben. Im Verwandte-hinzufügen-Workflow wurden zwei lokale Variablen mit demselben Namen `relationship` eindeutig in `existingRelationship` und `newRelationship` getrennt.
**Geänderte Bereiche:** `MainWindow.axaml.cs`.
**Ergebnis:** Der Verknüpfungszweig für bestehende Personen und der Zweig zum Anlegen neuer Verwandter verwenden nun getrennte lokale Variablennamen und kompilieren sauber.
**Verifikation:** `dotnet build` erfolgreich mit 0 Warnungen und 0 Fehlern. `dotnet test --no-build` erfolgreich mit 45 Tests in 1 m 46 s.
**Offene Punkte:** Commit `Improve family tree layout and add relative workflows` erstellen und nach GitHub pushen, sobald die laufenden Stammbaum-Änderungen final abgenommen sind.
