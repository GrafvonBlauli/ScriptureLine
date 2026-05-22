using System.Text.Json;
using BibleStudyGenealogy.Core.Models;
using BibleStudyGenealogy.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;

namespace BibleStudyGenealogy.Infrastructure.Services;

public sealed class LocalProjectService : IProjectService
{
    private const string ManifestFileName = "manifest.json";
    private const string DatabaseFileName = "project.sqlite";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public async Task<ProjectWorkspace> CreateProjectAsync(ProjectCreationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ParentDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ProjectName);

        var rootDirectory = CreateUniqueProjectDirectory(request.ParentDirectory, request.ProjectName);
        Directory.CreateDirectory(rootDirectory);
        CreateProjectFolders(rootDirectory);

        var now = DateTimeOffset.UtcNow;
        var metadata = new ProjectMetadata
        {
            ProjectName = request.ProjectName.Trim(),
            CreatedAtUtc = now,
            LastOpenedAtUtc = now
        };

        var settings = new ProjectSettings
        {
            ProjectName = metadata.ProjectName,
            Description = request.Description.Trim(),
            Language = request.Language.Trim(),
            PreferredBibleTranslation = request.PreferredBibleTranslation.Trim(),
            CreatedAtUtc = now,
            LastOpenedAtUtc = now
        };

        var databasePath = Path.Combine(rootDirectory, DatabaseFileName);
        await InitializeDatabaseAsync(databasePath, settings, cancellationToken);

        var manifestPath = Path.Combine(rootDirectory, ManifestFileName);
        await SaveManifestAsync(manifestPath, metadata, cancellationToken);

        return new ProjectWorkspace(rootDirectory, databasePath, manifestPath, metadata, settings);
    }

    public async Task<ProjectWorkspace> OpenProjectAsync(string projectDirectory, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectDirectory);

        var manifestPath = Path.Combine(projectDirectory, ManifestFileName);
        var databasePath = Path.Combine(projectDirectory, DatabaseFileName);

        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException("Die Projektdatei manifest.json wurde nicht gefunden.", manifestPath);
        }

        if (!File.Exists(databasePath))
        {
            throw new FileNotFoundException("Die Datenbank project.sqlite wurde nicht gefunden.", databasePath);
        }

        await EnsureDatabaseSchemaAsync(databasePath, cancellationToken);

        var metadataJson = await File.ReadAllTextAsync(manifestPath, cancellationToken);
        var metadata = JsonSerializer.Deserialize<ProjectMetadata>(metadataJson, JsonOptions)
            ?? throw new InvalidOperationException("Die Projektdatei manifest.json konnte nicht gelesen werden.");

        var settings = await ReadSettingsAsync(databasePath, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        metadata.LastOpenedAtUtc = now;
        settings.LastOpenedAtUtc = now;

        await UpdateLastOpenedAsync(databasePath, now, cancellationToken);
        await SaveManifestAsync(manifestPath, metadata, cancellationToken);

        return new ProjectWorkspace(projectDirectory, databasePath, manifestPath, metadata, settings);
    }

    public async Task<ProjectStatistics> ReadStatisticsAsync(ProjectWorkspace workspace, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        var personRepository = new PersonRepository(workspace.DatabasePath);
        var relationshipRepository = new RelationshipRepository(workspace.DatabasePath);
        var eventRepository = new EventRepository(workspace.DatabasePath);
        var bibleReferenceRepository = new BibleReferenceRepository(workspace.DatabasePath);
        var personCount = await personRepository.CountAsync(cancellationToken);
        var relationshipCount = await relationshipRepository.CountAsync(cancellationToken);
        var eventCount = await eventRepository.CountAsync(cancellationToken);
        var bibleReferenceCount = await bibleReferenceRepository.CountAsync(cancellationToken);

        return new ProjectStatistics(
            personCount,
            relationshipCount,
            eventCount,
            bibleReferenceCount,
            PlaceCount: 0,
            ResearchQuestionCount: 0);
    }

    private static string CreateUniqueProjectDirectory(string parentDirectory, string projectName)
    {
        var safeName = SanitizeDirectoryName(projectName);
        var candidate = Path.Combine(parentDirectory, safeName);

        if (!Directory.Exists(candidate))
        {
            return candidate;
        }

        var suffix = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss");
        return Path.Combine(parentDirectory, $"{safeName}-{suffix}");
    }

    private static string SanitizeDirectoryName(string projectName)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var sanitized = new string(projectName.Trim()
            .Select(character => invalidCharacters.Contains(character) ? '-' : character)
            .ToArray());

        return string.IsNullOrWhiteSpace(sanitized)
            ? "ScriptureLine-Projekt"
            : sanitized;
    }

    private static void CreateProjectFolders(string rootDirectory)
    {
        var folders = new[]
        {
            "Media",
            Path.Combine("Media", "Persons"),
            Path.Combine("Media", "Places"),
            Path.Combine("Media", "Events"),
            Path.Combine("Media", "PDFs"),
            Path.Combine("Media", "Maps"),
            Path.Combine("Media", "Other"),
            "Thumbnails",
            "Backups"
        };

        foreach (var folder in folders)
        {
            Directory.CreateDirectory(Path.Combine(rootDirectory, folder));
        }
    }

    private static async Task InitializeDatabaseAsync(string databasePath, ProjectSettings settings, CancellationToken cancellationToken)
    {
        await using var connection = await SqliteConnectionFactory.OpenAsync(databasePath, cancellationToken);

        await using var schemaCommand = connection.CreateCommand();
        schemaCommand.CommandText = SchemaSql;
        await schemaCommand.ExecuteNonQueryAsync(cancellationToken);
        await EnsureRelationshipStatusColumnAsync(connection, cancellationToken);

        await using var insertCommand = connection.CreateCommand();
        insertCommand.CommandText = """
            INSERT INTO ProjectSettings (
                Id,
                ProjectName,
                Description,
                Language,
                PreferredBibleTranslation,
                CreatedAtUtc,
                LastOpenedAtUtc
            )
            VALUES (
                1,
                $projectName,
                $description,
                $language,
                $preferredBibleTranslation,
                $createdAtUtc,
                $lastOpenedAtUtc
            );

            INSERT INTO SchemaInfo (Id, SchemaVersion, AppliedAtUtc)
            VALUES (1, $schemaVersion, $appliedAtUtc);
            """;
        insertCommand.Parameters.AddWithValue("$projectName", settings.ProjectName);
        insertCommand.Parameters.AddWithValue("$description", settings.Description);
        insertCommand.Parameters.AddWithValue("$language", settings.Language);
        insertCommand.Parameters.AddWithValue("$preferredBibleTranslation", settings.PreferredBibleTranslation);
        insertCommand.Parameters.AddWithValue("$createdAtUtc", settings.CreatedAtUtc.ToString("O"));
        insertCommand.Parameters.AddWithValue("$lastOpenedAtUtc", settings.LastOpenedAtUtc.ToString("O"));
        insertCommand.Parameters.AddWithValue("$schemaVersion", ProjectDefaults.SchemaVersion);
        insertCommand.Parameters.AddWithValue("$appliedAtUtc", settings.CreatedAtUtc.ToString("O"));
        await insertCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<ProjectSettings> ReadSettingsAsync(string databasePath, CancellationToken cancellationToken)
    {
        await using var connection = await SqliteConnectionFactory.OpenAsync(databasePath, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT ProjectName,
                   Description,
                   Language,
                   PreferredBibleTranslation,
                   CreatedAtUtc,
                   LastOpenedAtUtc
            FROM ProjectSettings
            WHERE Id = 1;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("Die Projekteinstellungen wurden in der Datenbank nicht gefunden.");
        }

        return new ProjectSettings
        {
            ProjectName = reader.GetString(0),
            Description = reader.GetString(1),
            Language = reader.GetString(2),
            PreferredBibleTranslation = reader.GetString(3),
            CreatedAtUtc = DateTimeOffset.Parse(reader.GetString(4)),
            LastOpenedAtUtc = DateTimeOffset.Parse(reader.GetString(5))
        };
    }

    private static async Task UpdateLastOpenedAsync(string databasePath, DateTimeOffset lastOpenedAtUtc, CancellationToken cancellationToken)
    {
        await using var connection = await SqliteConnectionFactory.OpenAsync(databasePath, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE ProjectSettings
            SET LastOpenedAtUtc = $lastOpenedAtUtc
            WHERE Id = 1;
            """;
        command.Parameters.AddWithValue("$lastOpenedAtUtc", lastOpenedAtUtc.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task SaveManifestAsync(string manifestPath, ProjectMetadata metadata, CancellationToken cancellationToken)
    {
        var metadataJson = JsonSerializer.Serialize(metadata, JsonOptions);
        await File.WriteAllTextAsync(manifestPath, metadataJson, cancellationToken);
    }

    private static async Task EnsureDatabaseSchemaAsync(string databasePath, CancellationToken cancellationToken)
    {
        await using var connection = await SqliteConnectionFactory.OpenAsync(databasePath, cancellationToken);

        await using var schemaCommand = connection.CreateCommand();
        schemaCommand.CommandText = SchemaSql;
        await schemaCommand.ExecuteNonQueryAsync(cancellationToken);
        await EnsureRelationshipStatusColumnAsync(connection, cancellationToken);
    }

    private static async Task EnsureRelationshipStatusColumnAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var readColumnsCommand = connection.CreateCommand();
        readColumnsCommand.CommandText = "PRAGMA table_info(Relationships);";

        await using var reader = await readColumnsCommand.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (string.Equals(reader.GetString(1), "Status", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        await using var addColumnCommand = connection.CreateCommand();
        addColumnCommand.CommandText = "ALTER TABLE Relationships ADD COLUMN Status TEXT NOT NULL DEFAULT 'Active';";
        await addColumnCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private const string SchemaSql = """
        PRAGMA user_version = 1;

        CREATE TABLE IF NOT EXISTS ProjectSettings (
            Id INTEGER PRIMARY KEY CHECK (Id = 1),
            ProjectName TEXT NOT NULL,
            Description TEXT NOT NULL,
            Language TEXT NOT NULL,
            PreferredBibleTranslation TEXT NOT NULL,
            CreatedAtUtc TEXT NOT NULL,
            LastOpenedAtUtc TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS SchemaInfo (
            Id INTEGER PRIMARY KEY CHECK (Id = 1),
            SchemaVersion INTEGER NOT NULL,
            AppliedAtUtc TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS Persons (
            Id TEXT PRIMARY KEY,
            MainName TEXT NOT NULL,
            AlternativeNames TEXT NOT NULL DEFAULT '',
            HebrewName TEXT NOT NULL DEFAULT '',
            GreekName TEXT NOT NULL DEFAULT '',
            NameMeaning TEXT NOT NULL DEFAULT '',
            Gender TEXT NOT NULL,
            PrimaryRole TEXT NOT NULL DEFAULT '',
            Occupation TEXT NOT NULL DEFAULT '',
            ShortDescription TEXT NOT NULL DEFAULT '',
            LongDescription TEXT NOT NULL DEFAULT '',
            PortraitMediaFileId TEXT NULL,
            Status TEXT NOT NULL,
            CreatedAtUtc TEXT NOT NULL,
            UpdatedAtUtc TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS Relationships (
            Id TEXT PRIMARY KEY,
            PersonAId TEXT NOT NULL,
            PersonBId TEXT NOT NULL,
            RelationshipType TEXT NOT NULL,
            Direction TEXT NOT NULL,
            CertaintyLevel TEXT NOT NULL,
            SourceNote TEXT NOT NULL DEFAULT '',
            Comment TEXT NOT NULL DEFAULT '',
            Status TEXT NOT NULL DEFAULT 'Active',
            CreatedAtUtc TEXT NOT NULL,
            UpdatedAtUtc TEXT NOT NULL,
            CHECK (PersonAId <> PersonBId),
            FOREIGN KEY (PersonAId) REFERENCES Persons(Id),
            FOREIGN KEY (PersonBId) REFERENCES Persons(Id)
        );

        CREATE INDEX IF NOT EXISTS IX_Relationships_PersonAId ON Relationships(PersonAId);
        CREATE INDEX IF NOT EXISTS IX_Relationships_PersonBId ON Relationships(PersonBId);
        CREATE INDEX IF NOT EXISTS IX_Relationships_Status ON Relationships(Status);
        CREATE INDEX IF NOT EXISTS IX_Relationships_Status_PersonAId ON Relationships(Status, PersonAId);
        CREATE INDEX IF NOT EXISTS IX_Relationships_Status_PersonBId ON Relationships(Status, PersonBId);

        CREATE TABLE IF NOT EXISTS Events (
            Id TEXT PRIMARY KEY,
            Title TEXT NOT NULL,
            EventType TEXT NOT NULL,
            DateText TEXT NOT NULL DEFAULT '',
            DateType TEXT NOT NULL DEFAULT 'Unknown',
            DateCertaintyLevel TEXT NOT NULL DEFAULT 'Unknown',
            ShortDescription TEXT NOT NULL DEFAULT '',
            LongDescription TEXT NOT NULL DEFAULT '',
            CertaintyLevel TEXT NOT NULL,
            CreatedAtUtc TEXT NOT NULL,
            UpdatedAtUtc TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS EventPersons (
            EventId TEXT NOT NULL,
            PersonId TEXT NOT NULL,
            PRIMARY KEY (EventId, PersonId),
            FOREIGN KEY (EventId) REFERENCES Events(Id),
            FOREIGN KEY (PersonId) REFERENCES Persons(Id)
        );

        CREATE TABLE IF NOT EXISTS BibleReferences (
            Id TEXT PRIMARY KEY,
            Translation TEXT NOT NULL DEFAULT '',
            Book TEXT NOT NULL,
            ChapterStart INTEGER NOT NULL,
            VerseStart INTEGER NULL,
            ChapterEnd INTEGER NULL,
            VerseEnd INTEGER NULL,
            ReferenceText TEXT NOT NULL DEFAULT '',
            UserSummary TEXT NOT NULL DEFAULT '',
            UserComment TEXT NOT NULL DEFAULT '',
            CreatedAtUtc TEXT NOT NULL,
            UpdatedAtUtc TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS EventBibleReferences (
            EventId TEXT NOT NULL,
            BibleReferenceId TEXT NOT NULL,
            PRIMARY KEY (EventId, BibleReferenceId),
            FOREIGN KEY (EventId) REFERENCES Events(Id),
            FOREIGN KEY (BibleReferenceId) REFERENCES BibleReferences(Id)
        );

        CREATE INDEX IF NOT EXISTS IX_EventPersons_PersonId ON EventPersons(PersonId);
        CREATE INDEX IF NOT EXISTS IX_EventBibleReferences_BibleReferenceId ON EventBibleReferences(BibleReferenceId);
        CREATE INDEX IF NOT EXISTS IX_Events_UpdatedAtUtc ON Events(UpdatedAtUtc);
        CREATE INDEX IF NOT EXISTS IX_BibleReferences_Book ON BibleReferences(Book);
        """;

}
