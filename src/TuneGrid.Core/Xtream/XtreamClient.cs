using System.Net.Http.Json;
using System.Text.Json.Serialization;
using TuneGrid.Core.Models;

namespace TuneGrid.Core.Xtream;

/// <summary>
/// Minimal client for the Xtream Codes "player_api.php" panel API. Builds every request
/// URL with <see cref="UriBuilder"/>/query-string escaping — never by interpolating the
/// user-supplied server/username/password into a format string — because those three
/// values come from whatever the user typed into the playlist-setup screen, i.e. they are
/// attacker-controlled input by definition (see CONSTRAINTS.md).
/// </summary>
public sealed class XtreamClient
{
    private readonly HttpClient _http;

    public XtreamClient(HttpClient http)
    {
        _http = http;
        if (_http.Timeout == Timeout.InfiniteTimeSpan)
        {
            _http.Timeout = TimeSpan.FromSeconds(20);
        }
    }

    public async Task<IReadOnlyList<Channel>> GetLiveChannelsAsync(
        XtreamCredentials credentials, string playlistId, CancellationToken ct = default)
    {
        var categories = await GetAsync<List<XtreamCategory>>(credentials, "get_live_categories", ct)
            ?? [];
        var categoryNames = categories.ToDictionary(c => c.CategoryId, c => c.CategoryName);

        var streams = await GetAsync<List<XtreamLiveStream>>(credentials, "get_live_streams", ct)
            ?? [];

        var channels = new List<Channel>(streams.Count);
        foreach (var s in streams)
        {
            if (string.IsNullOrWhiteSpace(s.Name) || s.StreamId <= 0)
            {
                continue; // malformed entry from the server; skip rather than throw
            }

            var streamUrl = BuildStreamUrl(credentials, s.StreamId);
            categoryNames.TryGetValue(s.CategoryId ?? string.Empty, out var groupName);

            channels.Add(new Channel
            {
                Id = s.StreamId.ToString(),
                PlaylistId = playlistId,
                Name = s.Name,
                LogoUrl = string.IsNullOrWhiteSpace(s.StreamIcon) ? null : s.StreamIcon,
                Group = groupName,
                StreamUrl = streamUrl,
                EpgChannelId = string.IsNullOrWhiteSpace(s.EpgChannelId) ? null : s.EpgChannelId,
            });
        }

        return channels;
    }

    private static Uri BuildStreamUrl(XtreamCredentials c, int streamId)
    {
        var baseUri = new Uri(c.ServerUrl, UriKind.Absolute);
        var builder = new UriBuilder(baseUri)
        {
            Path = $"{baseUri.AbsolutePath.TrimEnd('/')}/live/{Uri.EscapeDataString(c.Username)}/{Uri.EscapeDataString(c.Password)}/{streamId}.ts",
        };
        return builder.Uri;
    }

    private async Task<T?> GetAsync<T>(XtreamCredentials c, string action, CancellationToken ct)
    {
        var baseUri = new Uri(c.ServerUrl, UriKind.Absolute);
        var builder = new UriBuilder(baseUri)
        {
            Path = $"{baseUri.AbsolutePath.TrimEnd('/')}/player_api.php",
            Query =
                $"username={Uri.EscapeDataString(c.Username)}" +
                $"&password={Uri.EscapeDataString(c.Password)}" +
                $"&action={Uri.EscapeDataString(action)}",
        };

        using var response = await _http.GetAsync(builder.Uri, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
    }

    private sealed class XtreamCategory
    {
        [JsonPropertyName("category_id")]
        public string CategoryId { get; set; } = string.Empty;

        [JsonPropertyName("category_name")]
        public string CategoryName { get; set; } = string.Empty;
    }

    private sealed class XtreamLiveStream
    {
        [JsonPropertyName("stream_id")]
        public int StreamId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("stream_icon")]
        public string? StreamIcon { get; set; }

        [JsonPropertyName("category_id")]
        public string? CategoryId { get; set; }

        [JsonPropertyName("epg_channel_id")]
        public string? EpgChannelId { get; set; }
    }
}
