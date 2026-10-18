namespace TuneGrid.Core.Models;

/// <summary>A single playable channel entry, as parsed from a playlist.</summary>
public sealed class Channel
{
    /// <summary>Stable identity within its playlist (e.g. M3U tvg-id, or Xtream stream_id).</summary>
    public required string Id { get; init; }

    public required string PlaylistId { get; init; }

    public required string Name { get; init; }

    public string? LogoUrl { get; init; }

    /// <summary>Group/category name (M3U group-title, or Xtream category name).</summary>
    public string? Group { get; init; }

    public required Uri StreamUrl { get; init; }

    /// <summary>
    /// Channel id used to look up EPG data, matched against XMLTV &lt;channel id="..."&gt;.
    /// May differ from <see cref="Id"/> (M3U's tvg-id maps here; Xtream's epg_channel_id maps here).
    /// </summary>
    public string? EpgChannelId { get; init; }
}
