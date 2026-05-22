using Microsoft.Data.Sqlite;

namespace BibleStudyGenealogy.Infrastructure.Repositories;

internal static class SqliteConnectionFactory
{
    public static async Task<SqliteConnection> OpenAsync(string databasePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Pooling = false
        };

        var connection = new SqliteConnection(builder.ToString());
        await connection.OpenAsync(cancellationToken);
        await EnableForeignKeysAsync(connection, cancellationToken);
        return connection;
    }

    private static async Task EnableForeignKeysAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON;";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
