using BibleStudyGenealogy.Core.Models;
using Microsoft.Data.Sqlite;

namespace BibleStudyGenealogy.Infrastructure.Repositories;

public sealed class MediaRepository : IMediaRepository
{
    private readonly string _databasePath;

    public MediaRepository(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        _databasePath = databasePath;
    }

    public async Task<IReadOnlyList<MediaFile>> SearchAsync(string searchText, CancellationToken cancellationToken = default)
    {
        await using var connection = await SqliteConnectionFactory.OpenAsync(_databasePath, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id,
                   OriginalFileName,
                   RelativePath,
                   MediaType,
                   MimeType,
                   FileSizeBytes,
                   Description,
                   CreatedAtUtc,
                   UpdatedAtUtc
            FROM MediaFiles
            WHERE $searchText = ''
               OR OriginalFileName LIKE $searchPattern
               OR Description LIKE $searchPattern
               OR RelativePath LIKE $searchPattern
            ORDER BY UpdatedAtUtc DESC, OriginalFileName COLLATE NOCASE;
            """;
        command.Parameters.AddWithValue("$searchText", searchText.Trim());
        command.Parameters.AddWithValue("$searchPattern", $"%{searchText.Trim()}%");

        var mediaFiles = new List<MediaFile>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            mediaFiles.Add(ReadMediaFile(reader));
        }

        return mediaFiles;
    }

    public async Task<MediaFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await SqliteConnectionFactory.OpenAsync(_databasePath, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id,
                   OriginalFileName,
                   RelativePath,
                   MediaType,
                   MimeType,
                   FileSizeBytes,
                   Description,
                   CreatedAtUtc,
                   UpdatedAtUtc
            FROM MediaFiles
            WHERE Id = $id;
            """;
        command.Parameters.AddWithValue("$id", id.ToString());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? ReadMediaFile(reader)
            : null;
    }

    public async Task SaveAsync(MediaFile mediaFile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mediaFile);
        ArgumentException.ThrowIfNullOrWhiteSpace(mediaFile.OriginalFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(mediaFile.RelativePath);

        var now = DateTimeOffset.UtcNow;
        if (mediaFile.CreatedAtUtc == default)
        {
            mediaFile.CreatedAtUtc = now;
        }

        mediaFile.UpdatedAtUtc = now;

        await using var connection = await SqliteConnectionFactory.OpenAsync(_databasePath, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO MediaFiles (
                Id,
                OriginalFileName,
                RelativePath,
                MediaType,
                MimeType,
                FileSizeBytes,
                Description,
                CreatedAtUtc,
                UpdatedAtUtc
            )
            VALUES (
                $id,
                $originalFileName,
                $relativePath,
                $mediaType,
                $mimeType,
                $fileSizeBytes,
                $description,
                $createdAtUtc,
                $updatedAtUtc
            )
            ON CONFLICT(Id) DO UPDATE SET
                OriginalFileName = excluded.OriginalFileName,
                RelativePath = excluded.RelativePath,
                MediaType = excluded.MediaType,
                MimeType = excluded.MimeType,
                FileSizeBytes = excluded.FileSizeBytes,
                Description = excluded.Description,
                UpdatedAtUtc = excluded.UpdatedAtUtc;
            """;
        command.Parameters.AddWithValue("$id", mediaFile.Id.ToString());
        command.Parameters.AddWithValue("$originalFileName", mediaFile.OriginalFileName.Trim());
        command.Parameters.AddWithValue("$relativePath", mediaFile.RelativePath.Trim());
        command.Parameters.AddWithValue("$mediaType", mediaFile.MediaType.ToString());
        command.Parameters.AddWithValue("$mimeType", mediaFile.MimeType.Trim());
        command.Parameters.AddWithValue("$fileSizeBytes", mediaFile.FileSizeBytes);
        command.Parameters.AddWithValue("$description", mediaFile.Description.Trim());
        command.Parameters.AddWithValue("$createdAtUtc", mediaFile.CreatedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$updatedAtUtc", mediaFile.UpdatedAtUtc.ToString("O"));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await SqliteConnectionFactory.OpenAsync(_databasePath, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM MediaFiles;";
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task LinkAsync(Guid mediaFileId, LinkedEntityType entityType, Guid entityId, CancellationToken cancellationToken = default)
    {
        await using var connection = await SqliteConnectionFactory.OpenAsync(_databasePath, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT OR IGNORE INTO MediaLinks (
                MediaFileId,
                EntityType,
                EntityId,
                CreatedAtUtc
            )
            VALUES (
                $mediaFileId,
                $entityType,
                $entityId,
                $createdAtUtc
            );
            """;
        command.Parameters.AddWithValue("$mediaFileId", mediaFileId.ToString());
        command.Parameters.AddWithValue("$entityType", entityType.ToString());
        command.Parameters.AddWithValue("$entityId", entityId.ToString());
        command.Parameters.AddWithValue("$createdAtUtc", DateTimeOffset.UtcNow.ToString("O"));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MediaFile>> GetForEntityAsync(LinkedEntityType entityType, Guid entityId, CancellationToken cancellationToken = default)
    {
        await using var connection = await SqliteConnectionFactory.OpenAsync(_databasePath, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT m.Id,
                   m.OriginalFileName,
                   m.RelativePath,
                   m.MediaType,
                   m.MimeType,
                   m.FileSizeBytes,
                   m.Description,
                   m.CreatedAtUtc,
                   m.UpdatedAtUtc
            FROM MediaFiles m
            INNER JOIN MediaLinks l ON l.MediaFileId = m.Id
            WHERE l.EntityType = $entityType
              AND l.EntityId = $entityId
            ORDER BY m.UpdatedAtUtc DESC, m.OriginalFileName COLLATE NOCASE;
            """;
        command.Parameters.AddWithValue("$entityType", entityType.ToString());
        command.Parameters.AddWithValue("$entityId", entityId.ToString());

        var mediaFiles = new List<MediaFile>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            mediaFiles.Add(ReadMediaFile(reader));
        }

        return mediaFiles;
    }

    private static MediaFile ReadMediaFile(SqliteDataReader reader)
    {
        return new MediaFile
        {
            Id = Guid.Parse(reader.GetString(0)),
            OriginalFileName = reader.GetString(1),
            RelativePath = reader.GetString(2),
            MediaType = Enum.Parse<MediaType>(reader.GetString(3)),
            MimeType = reader.GetString(4),
            FileSizeBytes = reader.GetInt64(5),
            Description = reader.GetString(6),
            CreatedAtUtc = DateTimeOffset.Parse(reader.GetString(7)),
            UpdatedAtUtc = DateTimeOffset.Parse(reader.GetString(8))
        };
    }
}
