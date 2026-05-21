using BibleStudyGenealogy.Core.Models;
using Microsoft.Data.Sqlite;

namespace BibleStudyGenealogy.Infrastructure.Repositories;

public sealed class RelationshipRepository : IRelationshipRepository
{
    private readonly string _databasePath;

    public RelationshipRepository(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        _databasePath = databasePath;
    }

    public async Task<IReadOnlyList<Relationship>> GetForPersonAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(CreateConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id,
                   PersonAId,
                   PersonBId,
                   RelationshipType,
                   Direction,
                   CertaintyLevel,
                   SourceNote,
                   Comment,
                   Status,
                   CreatedAtUtc,
                   UpdatedAtUtc
            FROM Relationships
            WHERE Status = $status
              AND (PersonAId = $personId OR PersonBId = $personId)
            ORDER BY UpdatedAtUtc DESC;
            """;
        command.Parameters.AddWithValue("$personId", personId.ToString());
        command.Parameters.AddWithValue("$status", RelationshipStatus.Active.ToString());

        var relationships = new List<Relationship>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            relationships.Add(ReadRelationship(reader));
        }

        return relationships;
    }

    public async Task<Relationship?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(CreateConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id,
                   PersonAId,
                   PersonBId,
                   RelationshipType,
                   Direction,
                   CertaintyLevel,
                   SourceNote,
                   Comment,
                   Status,
                   CreatedAtUtc,
                   UpdatedAtUtc
            FROM Relationships
            WHERE Id = $id;
            """;
        command.Parameters.AddWithValue("$id", id.ToString());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? ReadRelationship(reader)
            : null;
    }

    public async Task SaveAsync(Relationship relationship, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(relationship);

        if (relationship.PersonAId == Guid.Empty || relationship.PersonBId == Guid.Empty)
        {
            throw new InvalidOperationException("Eine Beziehung braucht zwei Personen.");
        }

        if (relationship.PersonAId == relationship.PersonBId)
        {
            throw new InvalidOperationException("Eine Person kann nicht mit sich selbst verknüpft werden.");
        }

        await using var connection = new SqliteConnection(CreateConnectionString());
        await connection.OpenAsync(cancellationToken);

        if (await HasDuplicateAsync(connection, relationship, cancellationToken))
        {
            throw new InvalidOperationException("Diese Beziehung ist bereits vorhanden.");
        }

        var now = DateTimeOffset.UtcNow;
        if (relationship.CreatedAtUtc == default)
        {
            relationship.CreatedAtUtc = now;
        }

        relationship.UpdatedAtUtc = now;

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Relationships (
                Id,
                PersonAId,
                PersonBId,
                RelationshipType,
                Direction,
                CertaintyLevel,
                SourceNote,
                Comment,
                Status,
                CreatedAtUtc,
                UpdatedAtUtc
            )
            VALUES (
                $id,
                $personAId,
                $personBId,
                $relationshipType,
                $direction,
                $certaintyLevel,
                $sourceNote,
                $comment,
                $status,
                $createdAtUtc,
                $updatedAtUtc
            )
            ON CONFLICT(Id) DO UPDATE SET
                PersonAId = excluded.PersonAId,
                PersonBId = excluded.PersonBId,
                RelationshipType = excluded.RelationshipType,
                Direction = excluded.Direction,
                CertaintyLevel = excluded.CertaintyLevel,
                SourceNote = excluded.SourceNote,
                Comment = excluded.Comment,
                Status = excluded.Status,
                UpdatedAtUtc = excluded.UpdatedAtUtc;
            """;

        command.Parameters.AddWithValue("$id", relationship.Id.ToString());
        command.Parameters.AddWithValue("$personAId", relationship.PersonAId.ToString());
        command.Parameters.AddWithValue("$personBId", relationship.PersonBId.ToString());
        command.Parameters.AddWithValue("$relationshipType", relationship.RelationshipType.ToString());
        command.Parameters.AddWithValue("$direction", relationship.Direction.ToString());
        command.Parameters.AddWithValue("$certaintyLevel", relationship.CertaintyLevel.ToString());
        command.Parameters.AddWithValue("$sourceNote", relationship.SourceNote.Trim());
        command.Parameters.AddWithValue("$comment", relationship.Comment.Trim());
        command.Parameters.AddWithValue("$status", relationship.Status.ToString());
        command.Parameters.AddWithValue("$createdAtUtc", relationship.CreatedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$updatedAtUtc", relationship.UpdatedAtUtc.ToString("O"));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(CreateConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Relationships
            SET Status = $status,
                UpdatedAtUtc = $updatedAtUtc
            WHERE Id = $id;
            """;
        command.Parameters.AddWithValue("$id", id.ToString());
        command.Parameters.AddWithValue("$status", RelationshipStatus.Archived.ToString());
        command.Parameters.AddWithValue("$updatedAtUtc", DateTimeOffset.UtcNow.ToString("O"));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(CreateConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Relationships WHERE Status = $status;";
        command.Parameters.AddWithValue("$status", RelationshipStatus.Active.ToString());
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    private async Task<bool> HasDuplicateAsync(SqliteConnection connection, Relationship relationship, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM Relationships
            WHERE Id <> $id
              AND RelationshipType = $relationshipType
              AND Status = $status
              AND (
                    (PersonAId = $personAId AND PersonBId = $personBId)
                 OR (PersonAId = $personBId AND PersonBId = $personAId)
              );
            """;
        command.Parameters.AddWithValue("$id", relationship.Id.ToString());
        command.Parameters.AddWithValue("$relationshipType", relationship.RelationshipType.ToString());
        command.Parameters.AddWithValue("$status", RelationshipStatus.Active.ToString());
        command.Parameters.AddWithValue("$personAId", relationship.PersonAId.ToString());
        command.Parameters.AddWithValue("$personBId", relationship.PersonBId.ToString());

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    private string CreateConnectionString()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            Pooling = false
        };

        return builder.ToString();
    }

    private static Relationship ReadRelationship(SqliteDataReader reader)
    {
        return new Relationship
        {
            Id = Guid.Parse(reader.GetString(0)),
            PersonAId = Guid.Parse(reader.GetString(1)),
            PersonBId = Guid.Parse(reader.GetString(2)),
            RelationshipType = Enum.Parse<RelationshipType>(reader.GetString(3)),
            Direction = Enum.Parse<RelationshipDirection>(reader.GetString(4)),
            CertaintyLevel = Enum.Parse<CertaintyLevel>(reader.GetString(5)),
            SourceNote = reader.GetString(6),
            Comment = reader.GetString(7),
            Status = Enum.Parse<RelationshipStatus>(reader.GetString(8)),
            CreatedAtUtc = DateTimeOffset.Parse(reader.GetString(9)),
            UpdatedAtUtc = DateTimeOffset.Parse(reader.GetString(10))
        };
    }
}
