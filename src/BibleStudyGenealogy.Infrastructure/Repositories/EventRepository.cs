using BibleStudyGenealogy.Core.Models;
using Microsoft.Data.Sqlite;
using ScriptureEvent = BibleStudyGenealogy.Core.Models.Event;

namespace BibleStudyGenealogy.Infrastructure.Repositories;

public sealed class EventRepository : IEventRepository
{
    private readonly string _databasePath;

    public EventRepository(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        _databasePath = databasePath;
    }

    public async Task<IReadOnlyList<ScriptureEvent>> SearchAsync(string searchText, CancellationToken cancellationToken = default)
    {
        await using var connection = await SqliteConnectionFactory.OpenAsync(_databasePath, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id,
                   Title,
                   EventType,
                   DateText,
                   DateType,
                   DateCertaintyLevel,
                   ShortDescription,
                   LongDescription,
                   CertaintyLevel,
                   CreatedAtUtc,
                   UpdatedAtUtc
            FROM Events
            WHERE $searchText = ''
               OR Title LIKE $searchPattern
               OR ShortDescription LIKE $searchPattern
               OR LongDescription LIKE $searchPattern
            ORDER BY UpdatedAtUtc DESC, Title COLLATE NOCASE;
            """;
        command.Parameters.AddWithValue("$searchText", searchText.Trim());
        command.Parameters.AddWithValue("$searchPattern", $"%{searchText.Trim()}%");

        var events = new List<ScriptureEvent>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            events.Add(ReadEvent(reader));
        }

        return events;
    }

    public async Task<ScriptureEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await SqliteConnectionFactory.OpenAsync(_databasePath, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id,
                   Title,
                   EventType,
                   DateText,
                   DateType,
                   DateCertaintyLevel,
                   ShortDescription,
                   LongDescription,
                   CertaintyLevel,
                   CreatedAtUtc,
                   UpdatedAtUtc
            FROM Events
            WHERE Id = $id;
            """;
        command.Parameters.AddWithValue("$id", id.ToString());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? ReadEvent(reader)
            : null;
    }

    public async Task SaveAsync(ScriptureEvent scriptureEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scriptureEvent);
        ArgumentException.ThrowIfNullOrWhiteSpace(scriptureEvent.Title);

        var now = DateTimeOffset.UtcNow;
        if (scriptureEvent.CreatedAtUtc == default)
        {
            scriptureEvent.CreatedAtUtc = now;
        }

        scriptureEvent.UpdatedAtUtc = now;

        await using var connection = await SqliteConnectionFactory.OpenAsync(_databasePath, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Events (
                Id,
                Title,
                EventType,
                DateText,
                DateType,
                DateCertaintyLevel,
                ShortDescription,
                LongDescription,
                CertaintyLevel,
                CreatedAtUtc,
                UpdatedAtUtc
            )
            VALUES (
                $id,
                $title,
                $eventType,
                $dateText,
                $dateType,
                $dateCertaintyLevel,
                $shortDescription,
                $longDescription,
                $certaintyLevel,
                $createdAtUtc,
                $updatedAtUtc
            )
            ON CONFLICT(Id) DO UPDATE SET
                Title = excluded.Title,
                EventType = excluded.EventType,
                DateText = excluded.DateText,
                DateType = excluded.DateType,
                DateCertaintyLevel = excluded.DateCertaintyLevel,
                ShortDescription = excluded.ShortDescription,
                LongDescription = excluded.LongDescription,
                CertaintyLevel = excluded.CertaintyLevel,
                UpdatedAtUtc = excluded.UpdatedAtUtc;
            """;

        command.Parameters.AddWithValue("$id", scriptureEvent.Id.ToString());
        command.Parameters.AddWithValue("$title", scriptureEvent.Title.Trim());
        command.Parameters.AddWithValue("$eventType", scriptureEvent.EventType.ToString());
        command.Parameters.AddWithValue("$dateText", scriptureEvent.DateInfo?.ApproximationText.Trim() ?? string.Empty);
        command.Parameters.AddWithValue("$dateType", scriptureEvent.DateInfo?.DateType.ToString() ?? DateType.Unknown.ToString());
        command.Parameters.AddWithValue("$dateCertaintyLevel", scriptureEvent.DateInfo?.CertaintyLevel.ToString() ?? CertaintyLevel.Unknown.ToString());
        command.Parameters.AddWithValue("$shortDescription", scriptureEvent.ShortDescription.Trim());
        command.Parameters.AddWithValue("$longDescription", scriptureEvent.LongDescription.Trim());
        command.Parameters.AddWithValue("$certaintyLevel", scriptureEvent.CertaintyLevel.ToString());
        command.Parameters.AddWithValue("$createdAtUtc", scriptureEvent.CreatedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$updatedAtUtc", scriptureEvent.UpdatedAtUtc.ToString("O"));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await SqliteConnectionFactory.OpenAsync(_databasePath, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Events;";
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task LinkPersonAsync(Guid eventId, Guid personId, CancellationToken cancellationToken = default)
    {
        await using var connection = await SqliteConnectionFactory.OpenAsync(_databasePath, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT OR IGNORE INTO EventPersons (EventId, PersonId)
            VALUES ($eventId, $personId);
            """;
        command.Parameters.AddWithValue("$eventId", eventId.ToString());
        command.Parameters.AddWithValue("$personId", personId.ToString());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ScriptureEvent>> GetForPersonAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        await using var connection = await SqliteConnectionFactory.OpenAsync(_databasePath, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT e.Id,
                   e.Title,
                   e.EventType,
                   e.DateText,
                   e.DateType,
                   e.DateCertaintyLevel,
                   e.ShortDescription,
                   e.LongDescription,
                   e.CertaintyLevel,
                   e.CreatedAtUtc,
                   e.UpdatedAtUtc
            FROM Events e
            INNER JOIN EventPersons ep ON ep.EventId = e.Id
            WHERE ep.PersonId = $personId
            ORDER BY e.UpdatedAtUtc DESC, e.Title COLLATE NOCASE;
            """;
        command.Parameters.AddWithValue("$personId", personId.ToString());

        var events = new List<ScriptureEvent>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            events.Add(ReadEvent(reader));
        }

        return events;
    }

    public async Task<IReadOnlyList<Person>> GetPeopleForEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        await using var connection = await SqliteConnectionFactory.OpenAsync(_databasePath, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT p.Id,
                   p.MainName,
                   p.AlternativeNames,
                   p.HebrewName,
                   p.GreekName,
                   p.NameMeaning,
                   p.Gender,
                   p.PrimaryRole,
                   p.Occupation,
                   p.ShortDescription,
                   p.LongDescription,
                   p.PortraitMediaFileId,
                   p.Status,
                   p.CreatedAtUtc,
                   p.UpdatedAtUtc
            FROM Persons p
            INNER JOIN EventPersons ep ON ep.PersonId = p.Id
            WHERE ep.EventId = $eventId
            ORDER BY p.MainName COLLATE NOCASE;
            """;
        command.Parameters.AddWithValue("$eventId", eventId.ToString());

        var people = new List<Person>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            people.Add(ReadPerson(reader));
        }

        return people;
    }

    private static ScriptureEvent ReadEvent(SqliteDataReader reader)
    {
        var dateText = reader.GetString(3);

        return new ScriptureEvent
        {
            Id = Guid.Parse(reader.GetString(0)),
            Title = reader.GetString(1),
            EventType = Enum.Parse<EventType>(reader.GetString(2)),
            DateInfo = string.IsNullOrWhiteSpace(dateText)
                ? null
                : new DateInfo
                {
                    ApproximationText = dateText,
                    DateType = Enum.Parse<DateType>(reader.GetString(4)),
                    CertaintyLevel = Enum.Parse<CertaintyLevel>(reader.GetString(5))
                },
            ShortDescription = reader.GetString(6),
            LongDescription = reader.GetString(7),
            CertaintyLevel = Enum.Parse<CertaintyLevel>(reader.GetString(8)),
            CreatedAtUtc = DateTimeOffset.Parse(reader.GetString(9)),
            UpdatedAtUtc = DateTimeOffset.Parse(reader.GetString(10))
        };
    }

    private static Person ReadPerson(SqliteDataReader reader)
    {
        return new Person
        {
            Id = Guid.Parse(reader.GetString(0)),
            MainName = reader.GetString(1),
            AlternativeNames = reader.GetString(2),
            HebrewName = reader.GetString(3),
            GreekName = reader.GetString(4),
            NameMeaning = reader.GetString(5),
            Gender = Enum.Parse<Gender>(reader.GetString(6)),
            PrimaryRole = reader.GetString(7),
            Occupation = reader.GetString(8),
            ShortDescription = reader.GetString(9),
            LongDescription = reader.GetString(10),
            PortraitMediaFileId = reader.IsDBNull(11) ? null : Guid.Parse(reader.GetString(11)),
            Status = Enum.Parse<PersonStatus>(reader.GetString(12)),
            CreatedAtUtc = DateTimeOffset.Parse(reader.GetString(13)),
            UpdatedAtUtc = DateTimeOffset.Parse(reader.GetString(14))
        };
    }
}
