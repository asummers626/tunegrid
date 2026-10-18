namespace TuneGrid.Core.Models;

public enum PlaylistSourceType
{
    M3uUrl,
    M3uFile,
    Xtream,
}

/// <summary>A configured playlist source. Credentials, if any, live only in SQLite locally.</summary>
public sealed class Playlist
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required PlaylistSourceType SourceType { get; init; }

    /// <summary>M3U URL, or local file path, depending on <see cref="SourceType"/>.</summary>
    public string? M3uLocation { get; init; }

    public XtreamCredentials? Xtream { get; init; }

    /// <summary>Optional XMLTV URL to merge EPG data from (independent of the playlist source).</summary>
    public Uri? EpgUrl { get; init; }

    public DateTimeOffset? LastRefreshedAt { get; init; }
}

/// <summary>Xtream Codes API credentials. Never logged — see CONSTRAINTS.md.</summary>
public sealed class XtreamCredentials
{
    public required string ServerUrl { get; init; }
    public required string Username { get; init; }
    public required string Password { get; init; }

    public override string ToString() => $"XtreamCredentials({ServerUrl}, user=***)";
}
