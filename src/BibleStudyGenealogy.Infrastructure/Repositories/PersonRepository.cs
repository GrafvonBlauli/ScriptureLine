using BibleStudyGenealogy.Core.Models;
using Microsoft.Data.Sqlite;

namespace BibleStudyGenealogy.Infrastructure.Repositories;

public sealed class PersonRepository : IPersonRepository
{
    private readonly string _databasePath;

    public PersonRepository(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        _databasePath = databasePath;
    }

    public async Task<IReadOnlyList<Person>> SearchAsync(string searchText, CancellationToken cancellationToken = default)
    {
        await using var connection = await SqliteConnectionFactory.OpenAsync(_databasePath, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id,
                   MainName,
                   AlternativeNames,
                   HebrewName,
                   GreekName,
                   NameMeaning,
                   Gender,
                   PrimaryRole,
                   Occupation,
                   ShortDescription,
                   LongDescription,
                   BirthDateText,
                   BirthYear,
                   BirthIsBeforeChrist,
                   DeathDateText,
                   DeathYear,
                   DeathIsBeforeChrist,
                   AgeAtDeath,
                   BirthPlaceText,
                   DeathPlaceText,
                   PortraitMediaFileId,
                   Status,
                   CreatedAtUtc,
                   UpdatedAtUtc
            FROM Persons
            WHERE $searchText = ''
               OR MainName LIKE $searchPattern
               OR AlternativeNames LIKE $searchPattern
               OR PrimaryRole LIKE $searchPattern
            ORDER BY MainName COLLATE NOCASE;
            """;
        command.Parameters.AddWithValue("$searchText", searchText.Trim());
        command.Parameters.AddWithValue("$searchPattern", $"%{searchText.Trim()}%");

        var people = new List<Person>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            people.Add(ReadPerson(reader));
        }

        return people;
    }

    public async Task<Person?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await SqliteConnectionFactory.OpenAsync(_databasePath, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id,
                   MainName,
                   AlternativeNames,
                   HebrewName,
                   GreekName,
                   NameMeaning,
                   Gender,
                   PrimaryRole,
                   Occupation,
                   ShortDescription,
                   LongDescription,
                   BirthDateText,
                   BirthYear,
                   BirthIsBeforeChrist,
                   DeathDateText,
                   DeathYear,
                   DeathIsBeforeChrist,
                   AgeAtDeath,
                   BirthPlaceText,
                   DeathPlaceText,
                   PortraitMediaFileId,
                   Status,
                   CreatedAtUtc,
                   UpdatedAtUtc
            FROM Persons
            WHERE Id = $id;
            """;
        command.Parameters.AddWithValue("$id", id.ToString());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? ReadPerson(reader)
            : null;
    }

    public async Task SaveAsync(Person person, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(person);
        ArgumentException.ThrowIfNullOrWhiteSpace(person.MainName);

        var now = DateTimeOffset.UtcNow;
        if (person.CreatedAtUtc == default)
        {
            person.CreatedAtUtc = now;
        }

        person.UpdatedAtUtc = now;

        await using var connection = await SqliteConnectionFactory.OpenAsync(_databasePath, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Persons (
                Id,
                MainName,
                AlternativeNames,
                HebrewName,
                GreekName,
                NameMeaning,
                Gender,
                PrimaryRole,
                Occupation,
                ShortDescription,
                LongDescription,
                BirthDateText,
                BirthYear,
                BirthIsBeforeChrist,
                DeathDateText,
                DeathYear,
                DeathIsBeforeChrist,
                AgeAtDeath,
                BirthPlaceText,
                DeathPlaceText,
                PortraitMediaFileId,
                Status,
                CreatedAtUtc,
                UpdatedAtUtc
            )
            VALUES (
                $id,
                $mainName,
                $alternativeNames,
                $hebrewName,
                $greekName,
                $nameMeaning,
                $gender,
                $primaryRole,
                $occupation,
                $shortDescription,
                $longDescription,
                $birthDateText,
                $birthYear,
                $birthIsBeforeChrist,
                $deathDateText,
                $deathYear,
                $deathIsBeforeChrist,
                $ageAtDeath,
                $birthPlaceText,
                $deathPlaceText,
                $portraitMediaFileId,
                $status,
                $createdAtUtc,
                $updatedAtUtc
            )
            ON CONFLICT(Id) DO UPDATE SET
                MainName = excluded.MainName,
                AlternativeNames = excluded.AlternativeNames,
                HebrewName = excluded.HebrewName,
                GreekName = excluded.GreekName,
                NameMeaning = excluded.NameMeaning,
                Gender = excluded.Gender,
                PrimaryRole = excluded.PrimaryRole,
                Occupation = excluded.Occupation,
                ShortDescription = excluded.ShortDescription,
                LongDescription = excluded.LongDescription,
                BirthDateText = excluded.BirthDateText,
                BirthYear = excluded.BirthYear,
                BirthIsBeforeChrist = excluded.BirthIsBeforeChrist,
                DeathDateText = excluded.DeathDateText,
                DeathYear = excluded.DeathYear,
                DeathIsBeforeChrist = excluded.DeathIsBeforeChrist,
                AgeAtDeath = excluded.AgeAtDeath,
                BirthPlaceText = excluded.BirthPlaceText,
                DeathPlaceText = excluded.DeathPlaceText,
                PortraitMediaFileId = excluded.PortraitMediaFileId,
                Status = excluded.Status,
                UpdatedAtUtc = excluded.UpdatedAtUtc;
            """;

        command.Parameters.AddWithValue("$id", person.Id.ToString());
        command.Parameters.AddWithValue("$mainName", person.MainName.Trim());
        command.Parameters.AddWithValue("$alternativeNames", person.AlternativeNames.Trim());
        command.Parameters.AddWithValue("$hebrewName", person.HebrewName.Trim());
        command.Parameters.AddWithValue("$greekName", person.GreekName.Trim());
        command.Parameters.AddWithValue("$nameMeaning", person.NameMeaning.Trim());
        command.Parameters.AddWithValue("$gender", person.Gender.ToString());
        command.Parameters.AddWithValue("$primaryRole", person.PrimaryRole.Trim());
        command.Parameters.AddWithValue("$occupation", person.Occupation.Trim());
        command.Parameters.AddWithValue("$shortDescription", person.ShortDescription.Trim());
        command.Parameters.AddWithValue("$longDescription", person.LongDescription.Trim());
        command.Parameters.AddWithValue("$birthDateText", person.BirthDateInfo?.ApproximationText.Trim() ?? string.Empty);
        command.Parameters.AddWithValue("$birthYear", person.BirthDateInfo?.Year ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$birthIsBeforeChrist", person.BirthDateInfo?.IsBeforeChrist == true ? 1 : 0);
        command.Parameters.AddWithValue("$deathDateText", person.DeathDateInfo?.ApproximationText.Trim() ?? string.Empty);
        command.Parameters.AddWithValue("$deathYear", person.DeathDateInfo?.Year ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$deathIsBeforeChrist", person.DeathDateInfo?.IsBeforeChrist == true ? 1 : 0);
        command.Parameters.AddWithValue("$ageAtDeath", person.AgeAtDeath ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$birthPlaceText", person.BirthPlaceText.Trim());
        command.Parameters.AddWithValue("$deathPlaceText", person.DeathPlaceText.Trim());
        command.Parameters.AddWithValue("$portraitMediaFileId", person.PortraitMediaFileId?.ToString() ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$status", person.Status.ToString());
        command.Parameters.AddWithValue("$createdAtUtc", person.CreatedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$updatedAtUtc", person.UpdatedAtUtc.ToString("O"));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await SqliteConnectionFactory.OpenAsync(_databasePath, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Persons;";
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
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
            BirthDateInfo = ReadDateInfo(reader.GetString(11), reader.IsDBNull(12) ? null : reader.GetInt32(12), reader.GetInt32(13) == 1),
            DeathDateInfo = ReadDateInfo(reader.GetString(14), reader.IsDBNull(15) ? null : reader.GetInt32(15), reader.GetInt32(16) == 1),
            AgeAtDeath = reader.IsDBNull(17) ? null : reader.GetInt32(17),
            BirthPlaceText = reader.GetString(18),
            DeathPlaceText = reader.GetString(19),
            PortraitMediaFileId = reader.IsDBNull(20) ? null : Guid.Parse(reader.GetString(20)),
            Status = Enum.Parse<PersonStatus>(reader.GetString(21)),
            CreatedAtUtc = DateTimeOffset.Parse(reader.GetString(22)),
            UpdatedAtUtc = DateTimeOffset.Parse(reader.GetString(23))
        };
    }

    private static DateInfo? ReadDateInfo(string approximationText, int? year, bool isBeforeChrist)
    {
        if (string.IsNullOrWhiteSpace(approximationText) && year is null)
        {
            return null;
        }

        return new DateInfo
        {
            DateType = string.IsNullOrWhiteSpace(approximationText) ? DateType.ExactYear : DateType.TextOnly,
            ApproximationText = approximationText,
            Year = year,
            IsBeforeChrist = isBeforeChrist || ContainsBeforeChristMarker(approximationText),
            CertaintyLevel = CertaintyLevel.Unknown
        };
    }

    private static bool ContainsBeforeChristMarker(string value)
    {
        return value.Contains("v. Chr", StringComparison.CurrentCultureIgnoreCase)
            || value.Contains("v.Chr", StringComparison.CurrentCultureIgnoreCase)
            || value.Contains("bc", StringComparison.OrdinalIgnoreCase)
            || value.Contains("bce", StringComparison.OrdinalIgnoreCase);
    }
}
