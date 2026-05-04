using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace TrueFaceConnector;

public sealed class PunchQueue
{
    private readonly string _connectionString;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public PunchQueue(IOptions<ConnectorOptions> options)
    {
        SqliteConnectionStringBuilder builder = new()
        {
            DataSource = options.Value.QueueDatabasePath,
        };
        _connectionString = builder.ToString();
        Initialize();
    }

    public async Task EnqueueAsync(PunchRecord punch, CancellationToken cancellationToken)
    {
        await using SqliteConnection connection = new(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT OR IGNORE INTO punches (stable_key, device_id, payload, created_at, attempt_count)
            VALUES ($stable_key, $device_id, $payload, $created_at, 0)
            """;
        command.Parameters.AddWithValue("$stable_key", BuildStableKey(punch));
        command.Parameters.AddWithValue("$device_id", punch.DeviceSerial);
        command.Parameters.AddWithValue("$payload", JsonSerializer.Serialize(punch, _jsonOptions));
        command.Parameters.AddWithValue("$created_at", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<QueuedPunch>> GetBatchAsync(int maxRows, CancellationToken cancellationToken)
    {
        List<QueuedPunch> rows = [];
        await using SqliteConnection connection = new(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, device_id, payload
            FROM punches
            WHERE sent_at IS NULL
              AND (next_attempt_at IS NULL OR next_attempt_at <= $now)
            ORDER BY id
            LIMIT $limit
            """;
        command.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        command.Parameters.AddWithValue("$limit", maxRows);

        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            PunchRecord punch = JsonSerializer.Deserialize<PunchRecord>(reader.GetString(2), _jsonOptions)
                ?? throw new InvalidOperationException("Queued punch payload could not be deserialized.");
            rows.Add(new QueuedPunch(reader.GetInt64(0), reader.GetString(1), punch));
        }
        return rows;
    }

    public async Task MarkSentAsync(IEnumerable<long> ids, CancellationToken cancellationToken)
    {
        await using SqliteConnection connection = new(_connectionString);
        await connection.OpenAsync(cancellationToken);
        foreach (long id in ids)
        {
            await using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "UPDATE punches SET sent_at = $sent_at WHERE id = $id";
            command.Parameters.AddWithValue("$sent_at", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            command.Parameters.AddWithValue("$id", id);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task MarkFailedAsync(IEnumerable<long> ids, CancellationToken cancellationToken)
    {
        await using SqliteConnection connection = new(_connectionString);
        await connection.OpenAsync(cancellationToken);
        foreach (long id in ids)
        {
            await using SqliteCommand command = connection.CreateCommand();
            command.CommandText = """
                UPDATE punches
                SET attempt_count = attempt_count + 1,
                    next_attempt_at = $next_attempt_at
                WHERE id = $id
                """;
            command.Parameters.AddWithValue("$next_attempt_at", DateTimeOffset.UtcNow.AddMinutes(2).ToUnixTimeSeconds());
            command.Parameters.AddWithValue("$id", id);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private void Initialize()
    {
        using SqliteConnection connection = new(_connectionString);
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS punches (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                stable_key TEXT NOT NULL UNIQUE,
                device_id TEXT NOT NULL,
                payload TEXT NOT NULL,
                created_at INTEGER NOT NULL,
                sent_at INTEGER NULL,
                attempt_count INTEGER NOT NULL DEFAULT 0,
                next_attempt_at INTEGER NULL
            );
            CREATE INDEX IF NOT EXISTS idx_punches_pending ON punches(sent_at, next_attempt_at, id);
            """;
        command.ExecuteNonQuery();
    }

    private static string BuildStableKey(PunchRecord punch)
    {
        if (!string.IsNullOrWhiteSpace(punch.RecordNumber))
        {
            return $"{punch.DeviceSerial}:record:{punch.RecordNumber}";
        }
        if (!string.IsNullOrWhiteSpace(punch.EventId))
        {
            return $"{punch.DeviceSerial}:event:{punch.EventId}";
        }
        return $"{punch.DeviceSerial}:{punch.UserId}:{punch.PunchTime:O}:{punch.Direction}";
    }
}

public sealed record QueuedPunch(long Id, string DeviceId, PunchRecord Punch);
