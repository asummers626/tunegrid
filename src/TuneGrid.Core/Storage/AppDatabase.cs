using Microsoft.Data.Sqlite;
using TuneGrid.Core.Models;

namespace TuneGrid.Core.Storage;

/// <summary>
/// Local SQLite storage for playlists, channels, EPG cache, and favorites. Fully
/// offline/local — nothing here talks to a network, and there is no sync service
/// (see SPEC.md non-goals). All queries are parameterized; no user-supplied value
/// (playlist name, channel name, Xtream credentials, etc.) is ever concatenated into
/// a SQL string.
/// </summary>
public sealed class AppDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public AppDatabase(string databasePath)
    {
        _connection = new SqliteConnection($"Data Source={databasePath}");
        _connection.Open();
        Migrate();
    }

    private void Migrate()
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS playlists (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                source_type TEXT NOT NULL,
                m3u_location TEXT,
                xtream_server TEXT,
                xtream_username TEXT,
                xtream_password TEXT,
                epg_url TEXT,
                last_refreshed_at TEXT
            );

            CREATE TABLE IF NOT EXISTS channels (
                id TEXT NOT NULL,
                playlist_id TEXT NOT NULL REFERENCES playlists(id) ON DELETE CASCADE,
                name TEXT NOT NULL,
                logo_url TEXT,
                group_name TEXT,
                stream_url TEXT NOT NULL,
                epg_channel_id TEXT,
                PRIMARY KEY (id, playlist_id)
            );

            CREATE TABLE IF NOT EXISTS epg_programs (
                epg_channel_id TEXT NOT NULL,
                title TEXT NOT NULL,
                description TEXT,
                start_utc TEXT NOT NULL,
                stop_utc TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_epg_channel_time
                ON epg_programs(epg_channel_id, start_utc);

            CREATE TABLE IF NOT EXISTS favorite_channels (
                channel_id TEXT NOT NULL,
                playlist_id TEXT NOT NULL,
                PRIMARY KEY (channel_id, playlist_id)
            );
            """;

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    public void UpsertPlaylist(Playlist playlist)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO playlists (id, name, source_type, m3u_location, xtream_server, xtream_username, xtream_password, epg_url, last_refreshed_at)
            VALUES ($id, $name, $sourceType, $m3uLocation, $xtreamServer, $xtreamUsername, $xtreamPassword, $epgUrl, $lastRefreshedAt)
            ON CONFLICT(id) DO UPDATE SET
                name = excluded.name,
                source_type = excluded.source_type,
                m3u_location = excluded.m3u_location,
                xtream_server = excluded.xtream_server,
                xtream_username = excluded.xtream_username,
                xtream_password = excluded.xtream_password,
                epg_url = excluded.epg_url,
                last_refreshed_at = excluded.last_refreshed_at;
            """;

        cmd.Parameters.AddWithValue("$id", playlist.Id);
        cmd.Parameters.AddWithValue("$name", playlist.Name);
        cmd.Parameters.AddWithValue("$sourceType", playlist.SourceType.ToString());
        cmd.Parameters.AddWithValue("$m3uLocation", playlist.M3uLocation ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$xtreamServer", playlist.Xtream?.ServerUrl ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$xtreamUsername", playlist.Xtream?.Username ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$xtreamPassword", playlist.Xtream?.Password ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$epgUrl", playlist.EpgUrl?.ToString() ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$lastRefreshedAt", playlist.LastRefreshedAt?.ToString("O") ?? (object)DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public void ReplaceChannels(string playlistId, IReadOnlyList<Channel> channels)
    {
        using var transaction = _connection.BeginTransaction();

        using (var del = _connection.CreateCommand())
        {
            del.Transaction = transaction;
            del.CommandText = "DELETE FROM channels WHERE playlist_id = $playlistId;";
            del.Parameters.AddWithValue("$playlistId", playlistId);
            del.ExecuteNonQuery();
        }

        foreach (var channel in channels)
        {
            using var insert = _connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT INTO channels (id, playlist_id, name, logo_url, group_name, stream_url, epg_channel_id)
                VALUES ($id, $playlistId, $name, $logoUrl, $group, $streamUrl, $epgChannelId);
                """;
            insert.Parameters.AddWithValue("$id", channel.Id);
            insert.Parameters.AddWithValue("$playlistId", playlistId);
            insert.Parameters.AddWithValue("$name", channel.Name);
            insert.Parameters.AddWithValue("$logoUrl", channel.LogoUrl ?? (object)DBNull.Value);
            insert.Parameters.AddWithValue("$group", channel.Group ?? (object)DBNull.Value);
            insert.Parameters.AddWithValue("$streamUrl", channel.StreamUrl.ToString());
            insert.Parameters.AddWithValue("$epgChannelId", channel.EpgChannelId ?? (object)DBNull.Value);
            insert.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public void Dispose() => _connection.Dispose();
}
