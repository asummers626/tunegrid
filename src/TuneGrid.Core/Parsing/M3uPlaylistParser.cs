using System.Text;
using System.Text.RegularExpressions;
using TuneGrid.Core.Models;

namespace TuneGrid.Core.Parsing;

public sealed record M3uParseResult(IReadOnlyList<Channel> Channels, IReadOnlyList<string> Warnings);

/// <summary>
/// Parses extended M3U ("#EXTM3U" / "#EXTINF") playlists, as served by IPTV providers.
/// Treats the input as untrusted: unknown attributes are ignored, malformed entries are
/// skipped and recorded as warnings rather than thrown, and nothing here executes or
/// evaluates any part of the file.
/// </summary>
public static class M3uPlaylistParser
{
    // tvg-id="..."  group-title="..."  tvg-logo="..."  (quotes required by the format; value may be empty)
    private static readonly Regex AttributePattern = new(
        @"(?<key>[\w-]+)=""(?<value>[^""]*)""",
        RegexOptions.Compiled);

    public static M3uParseResult Parse(string content, string playlistId)
    {
        var channels = new List<Channel>();
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(content))
        {
            warnings.Add("Playlist is empty.");
            return new M3uParseResult(channels, warnings);
        }

        using var reader = new StringReader(content);
        string? line = reader.ReadLine();
        int lineNumber = 1;

        if (line is null || !line.TrimStart().StartsWith("#EXTM3U", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Missing #EXTM3U header; attempting to parse anyway.");
        }

        string? pendingName = null;
        string? pendingTvgId = null;
        string? pendingLogo = null;
        string? pendingGroup = null;
        int entryStartLine = 0;

        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || (trimmed.StartsWith('#') && !trimmed.StartsWith("#EXTINF", StringComparison.OrdinalIgnoreCase)))
            {
                continue; // blank line or a non-#EXTINF comment/directive we don't need
            }

            if (trimmed.StartsWith("#EXTINF", StringComparison.OrdinalIgnoreCase))
            {
                entryStartLine = lineNumber;
                var colon = trimmed.IndexOf(':');
                if (colon < 0)
                {
                    warnings.Add($"Line {lineNumber}: malformed #EXTINF (no ':'), skipped.");
                    pendingName = null;
                    continue;
                }

                var rest = trimmed[(colon + 1)..];

                // Everything after the last comma on the #EXTINF line is the display name;
                // attributes (tvg-id etc.) precede it.
                var lastComma = rest.LastIndexOf(',');
                var attributesPart = lastComma >= 0 ? rest[..lastComma] : rest;
                var namePart = lastComma >= 0 ? rest[(lastComma + 1)..].Trim() : string.Empty;

                pendingName = string.IsNullOrWhiteSpace(namePart) ? null : namePart;
                pendingTvgId = ExtractAttribute(attributesPart, "tvg-id");
                pendingLogo = ExtractAttribute(attributesPart, "tvg-logo");
                pendingGroup = ExtractAttribute(attributesPart, "group-title");
                continue;
            }

            // Otherwise this non-comment line is the stream URL for the preceding #EXTINF.
            if (pendingName is null)
            {
                warnings.Add($"Line {lineNumber}: stream URL with no preceding #EXTINF, skipped.");
                continue;
            }

            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var streamUri) ||
                (streamUri.Scheme != Uri.UriSchemeHttp && streamUri.Scheme != Uri.UriSchemeHttps))
            {
                warnings.Add($"Line {entryStartLine}-{lineNumber}: invalid or non-HTTP(S) stream URL, skipped.");
                pendingName = null;
                continue;
            }

            channels.Add(new Channel
            {
                Id = string.IsNullOrWhiteSpace(pendingTvgId) ? $"{playlistId}:{channels.Count}" : pendingTvgId!,
                PlaylistId = playlistId,
                Name = pendingName!,
                LogoUrl = pendingLogo,
                Group = pendingGroup,
                StreamUrl = streamUri,
                EpgChannelId = string.IsNullOrWhiteSpace(pendingTvgId) ? null : pendingTvgId,
            });

            pendingName = null;
            pendingTvgId = null;
            pendingLogo = null;
            pendingGroup = null;
        }

        return new M3uParseResult(channels, warnings);
    }

    private static string? ExtractAttribute(string attributesSegment, string key)
    {
        foreach (Match m in AttributePattern.Matches(attributesSegment))
        {
            if (string.Equals(m.Groups["key"].Value, key, StringComparison.OrdinalIgnoreCase))
            {
                var value = m.Groups["value"].Value;
                return value.Length == 0 ? null : value;
            }
        }
        return null;
    }
}
