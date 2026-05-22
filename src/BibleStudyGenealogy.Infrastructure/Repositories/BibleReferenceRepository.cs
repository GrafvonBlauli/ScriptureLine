using BibleStudyGenealogy.Core.Models;
using Microsoft.Data.Sqlite;

namespace BibleStudyGenealogy.Infrastructure.Repositories;

public sealed class BibleReferenceRepository : IBibleReferenceRepository
{
    private readonly string _databasePath;

    public BibleReferenceRepository(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        _databasePath = databasePath;
    }

    public async Task<IReadOnlyList<BibleReference>> SearchAsync(string searchText, CancellationToken cancellationToken = default)
    {
        await using var connection = await SqliteConnectionFactory.OpenAsync(_databasePath, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id,
                   Translation,
                   Book,
                   ChapterStart,
                   VerseStart,
                   ChapterEnd,
                   VerseEnd,
                   ReferenceText,
                   UserSummary,
                   UserComment,
                   CreatedAtUtc,
                   UpdatedAtUtc
            FROM BibleReferences
            WHERE $searchText = ''
               OR Book LIKE $searchPattern
               OR ReferenceText LIKE $searchPattern
               OR UserSummary LIKE $searchPattern
               OR UserComment LIKE $searchPattern
            ORDER BY Book COLLATE NOCASE, ChapterStart, VerseStart;
            """;
        command.Parameters.AddWithValue("$searchText", searchText.Trim());
        command.Parameters.AddWithValue("$searchPattern", $"%{searchText.Trim()}%");

        var references = new List<BibleReference>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            references.Add(ReadBibleReference(reader));
        }

        return references;
    }

    public async Task<BibleReference?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await SqliteConnectionFactory.OpenAsync(_databasePath, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id,
                   Translation,
                   Book,
                   ChapterStart,
                   VerseStart,
                   ChapterEnd,
                   VerseEnd,
                   ReferenceText,
                   UserSummary,
                   UserComment,
                   CreatedAtUtc,
                   UpdatedAtUtc
            FROM BibleReferences
            WHERE Id = $id;
            """;
        command.Parameters.AddWithValue("$id", id.ToString());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? ReadBibleReference(reader)
            : null;
    }

    public async Task SaveAsync(BibleReference bibleReference, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bibleReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(bibleReference.Book);

        if (bibleReference.ChapterStart < 1)
        {
            throw new InvalidOperationException("Eine Bibelstelle braucht mindestens ein Buch und ein Startkapitel.");
        }

        ValidateReferenceRange(bibleReference);

        var now = DateTimeOffset.UtcNow;
        if (bibleReference.CreatedAtUtc == default)
        {
            bibleReference.CreatedAtUtc = now;
        }

        bibleReference.UpdatedAtUtc = now;

        await using var connection = await SqliteConnectionFactory.OpenAsync(_databasePath, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO BibleReferences (
                Id,
                Translation,
                Book,
                ChapterStart,
                VerseStart,
                ChapterEnd,
                VerseEnd,
                ReferenceText,
                UserSummary,
                UserComment,
                CreatedAtUtc,
                UpdatedAtUtc
            )
            VALUES (
                $id,
                $translation,
                $book,
                $chapterStart,
                $verseStart,
                $chapterEnd,
                $verseEnd,
                $referenceText,
                $userSummary,
                $userComment,
                $createdAtUtc,
                $updatedAtUtc
            )
            ON CONFLICT(Id) DO UPDATE SET
                Translation = excluded.Translation,
                Book = excluded.Book,
                ChapterStart = excluded.ChapterStart,
                VerseStart = excluded.VerseStart,
                ChapterEnd = excluded.ChapterEnd,
                VerseEnd = excluded.VerseEnd,
                ReferenceText = excluded.ReferenceText,
                UserSummary = excluded.UserSummary,
                UserComment = excluded.UserComment,
                UpdatedAtUtc = excluded.UpdatedAtUtc;
            """;

        command.Parameters.AddWithValue("$id", bibleReference.Id.ToString());
        command.Parameters.AddWithValue("$translation", bibleReference.Translation.Trim());
        command.Parameters.AddWithValue("$book", bibleReference.Book.Trim());
        command.Parameters.AddWithValue("$chapterStart", bibleReference.ChapterStart);
        command.Parameters.AddWithValue("$verseStart", bibleReference.VerseStart ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$chapterEnd", bibleReference.ChapterEnd ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$verseEnd", bibleReference.VerseEnd ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$referenceText", bibleReference.ReferenceText.Trim());
        command.Parameters.AddWithValue("$userSummary", bibleReference.UserSummary.Trim());
        command.Parameters.AddWithValue("$userComment", bibleReference.UserComment.Trim());
        command.Parameters.AddWithValue("$createdAtUtc", bibleReference.CreatedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$updatedAtUtc", bibleReference.UpdatedAtUtc.ToString("O"));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await SqliteConnectionFactory.OpenAsync(_databasePath, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM BibleReferences;";
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task LinkEventAsync(Guid eventId, Guid bibleReferenceId, CancellationToken cancellationToken = default)
    {
        await using var connection = await SqliteConnectionFactory.OpenAsync(_databasePath, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT OR IGNORE INTO EventBibleReferences (EventId, BibleReferenceId)
            VALUES ($eventId, $bibleReferenceId);
            """;
        command.Parameters.AddWithValue("$eventId", eventId.ToString());
        command.Parameters.AddWithValue("$bibleReferenceId", bibleReferenceId.ToString());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BibleReference>> GetForEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        await using var connection = await SqliteConnectionFactory.OpenAsync(_databasePath, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT b.Id,
                   b.Translation,
                   b.Book,
                   b.ChapterStart,
                   b.VerseStart,
                   b.ChapterEnd,
                   b.VerseEnd,
                   b.ReferenceText,
                   b.UserSummary,
                   b.UserComment,
                   b.CreatedAtUtc,
                   b.UpdatedAtUtc
            FROM BibleReferences b
            INNER JOIN EventBibleReferences ebr ON ebr.BibleReferenceId = b.Id
            WHERE ebr.EventId = $eventId
            ORDER BY b.Book COLLATE NOCASE, b.ChapterStart, b.VerseStart;
            """;
        command.Parameters.AddWithValue("$eventId", eventId.ToString());

        var references = new List<BibleReference>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            references.Add(ReadBibleReference(reader));
        }

        return references;
    }

    private static BibleReference ReadBibleReference(SqliteDataReader reader)
    {
        return new BibleReference
        {
            Id = Guid.Parse(reader.GetString(0)),
            Translation = reader.GetString(1),
            Book = reader.GetString(2),
            ChapterStart = reader.GetInt32(3),
            VerseStart = reader.IsDBNull(4) ? null : reader.GetInt32(4),
            ChapterEnd = reader.IsDBNull(5) ? null : reader.GetInt32(5),
            VerseEnd = reader.IsDBNull(6) ? null : reader.GetInt32(6),
            ReferenceText = reader.GetString(7),
            UserSummary = reader.GetString(8),
            UserComment = reader.GetString(9),
            CreatedAtUtc = DateTimeOffset.Parse(reader.GetString(10)),
            UpdatedAtUtc = DateTimeOffset.Parse(reader.GetString(11))
        };
    }

    private static void ValidateReferenceRange(BibleReference bibleReference)
    {
        if (bibleReference.VerseStart <= 0 || bibleReference.ChapterEnd <= 0 || bibleReference.VerseEnd <= 0)
        {
            throw new InvalidOperationException("Kapitel und Verse müssen positive Zahlen sein.");
        }

        if (bibleReference.ChapterEnd is not null && bibleReference.ChapterEnd < bibleReference.ChapterStart)
        {
            throw new InvalidOperationException("Das Endkapitel darf nicht vor dem Startkapitel liegen.");
        }

        var effectiveEndChapter = bibleReference.ChapterEnd ?? bibleReference.ChapterStart;
        if (effectiveEndChapter == bibleReference.ChapterStart
            && bibleReference.VerseStart is not null
            && bibleReference.VerseEnd is not null
            && bibleReference.VerseEnd < bibleReference.VerseStart)
        {
            throw new InvalidOperationException("Der Endvers darf nicht vor dem Startvers liegen.");
        }
    }
}
