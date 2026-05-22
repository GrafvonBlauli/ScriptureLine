# ScriptureLine UI-Funktionsmatrix

Stand: 2026-05-22

Diese Matrix hält fest, welche sichtbaren Bedienelemente bereits eine echte Funktion haben und welche bewusst als Platzhalter markiert sind.

| Bereich | Element | Status | Prüfung |
|---|---|---|---|
| Projekt | Neues Projekt | aktiv | legt Projektordner, Manifest und SQLite-Datenbank an |
| Projekt | Projekt öffnen | aktiv | öffnet bestehenden Projektordner und lädt Daten |
| Dashboard | Person anlegen | aktiv nach Projektöffnung | bereitet neue Person vor |
| Dashboard | Ereignis erfassen | aktiv nach Projektöffnung | bereitet neues Ereignis vor |
| Dashboard | Ort anlegen | Platzhalter | bleibt deaktiviert und als kommende Funktion markiert |
| Sidebar | Dashboard | aktiv | zeigt Hero, Projektstatus, Zähler, Timeline-Vorschau und Schnellzugriff |
| Sidebar | Personen | aktiv | öffnet Personenliste und Personenformular |
| Sidebar | Stammbaum | aktiv | öffnet Beziehungsverwaltung und Stammbaum-Vorschau |
| Sidebar | Zeitstrahl | aktiv | öffnet eigene Timeline-Oberfläche mit Ereignisliste |
| Sidebar | Ereignisse | aktiv | öffnet Ereignisliste und Ereignisformular |
| Sidebar | Bibelstellen | aktiv | öffnet Bibelstellenliste und Erfassungsformular |
| Sidebar | Mediathek | aktiv | öffnet Medienliste, Import und Verknüpfungsaktionen |
| Sidebar | Karte, Orte, Forschungsfragen | Platzhalterseite | erreichbar, aber klar als noch nicht umgesetzt markiert |
| Personen | Suche, Auswahl, Speichern | aktiv | über Repository-Tests und Smoke-Test-Szenarien abgesichert |
| Beziehungen | Speichern, Auswählen, Archivieren | aktiv | Tests decken Speichern, Duplikate, Archivierung und FK-Integrität ab |
| Stammbaum | Vorschau | aktiv | `FamilyTreeBuilder`-Tests prüfen Eltern, Partner, Kinder und unsichere Beziehungen |
| Ereignisse | Speichern, Auswahl | aktiv | Tests decken Speichern, Update, Datierung und Personen-Verknüpfung ab |
| Bibelstellen | Speichern, Auswahl | aktiv | Tests decken Speichern, Suche, Bereichsvalidierung und Event-Verknüpfung ab |
| Mediathek | Import, Suche, Beschreibung, Verknüpfung, Portrait | aktiv | Tests decken Import, Pfade, Suche, Links, fehlende Dateien und PortraitId ab |
| Timeline | Dashboard-Vorschau und eigenes Modul | aktiv in Version 0.1 | zeigt Ereignisse, getrennt nach datiert und ohne Datierung |
