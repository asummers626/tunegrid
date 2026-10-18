using System.Globalization;
using System.Xml;
using TuneGrid.Core.Models;

namespace TuneGrid.Core.Parsing;

public sealed record XmltvParseResult(IReadOnlyList<EpgProgram> Programs, IReadOnlyList<string> Warnings);

/// <summary>
/// Streaming parser for XMLTV guide files (the de facto standard EPG format used by most
/// IPTV providers). Uses <see cref="XmlReader"/> (not a loaded DOM, and DTD/external
/// entities disabled) so a large or hostile guide file can't exhaust memory or trigger
/// XXE, and a malformed &lt;programme&gt; is skipped with a warning rather than aborting
/// the whole file.
/// </summary>
public static class XmltvParser
{
    // XMLTV datetime: "YYYYMMDDHHMMSS [+-]HHMM", offset part optional.
    private const string DateFormatWithOffset = "yyyyMMddHHmmss zzz";
    private const string DateFormatNoOffset = "yyyyMMddHHmmss";

    public static XmltvParseResult Parse(Stream xmlStream)
    {
        var programs = new List<EpgProgram>();
        var warnings = new List<string>();

        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Ignore,
            XmlResolver = null, // disables external entity/DTD resolution (XXE)
            IgnoreComments = true,
            IgnoreWhitespace = true,
        };

        using var reader = XmlReader.Create(xmlStream, settings);

        try
        {
            while (reader.Read())
            {
                if (reader.NodeType != XmlNodeType.Element || !string.Equals(reader.Name, "programme", StringComparison.Ordinal))
                {
                    continue;
                }

                var channelId = reader.GetAttribute("channel");
                var startRaw = reader.GetAttribute("start");
                var stopRaw = reader.GetAttribute("stop");

                if (string.IsNullOrWhiteSpace(channelId) || string.IsNullOrWhiteSpace(startRaw) || string.IsNullOrWhiteSpace(stopRaw))
                {
                    warnings.Add("Skipped <programme> missing channel/start/stop attribute.");
                    reader.Skip();
                    continue;
                }

                if (!TryParseXmltvDate(startRaw, out var start) || !TryParseXmltvDate(stopRaw, out var stop))
                {
                    warnings.Add($"Skipped <programme channel=\"{channelId}\"> with unparsable start/stop timestamp.");
                    reader.Skip();
                    continue;
                }

                string? title = null;
                string? description = null;

                using (var sub = reader.ReadSubtree())
                {
                    sub.Read(); // move onto <programme> itself
                    while (sub.Read())
                    {
                        if (sub.NodeType != XmlNodeType.Element)
                        {
                            continue;
                        }

                        switch (sub.Name)
                        {
                            case "title":
                                title ??= sub.ReadElementContentAsString();
                                break;
                            case "desc":
                                description ??= sub.ReadElementContentAsString();
                                break;
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(title))
                {
                    warnings.Add($"Skipped <programme channel=\"{channelId}\"> with no <title>.");
                    continue;
                }

                programs.Add(new EpgProgram
                {
                    EpgChannelId = channelId!,
                    Title = title!,
                    Description = description,
                    Start = start,
                    Stop = stop,
                });
            }
        }
        catch (XmlException ex)
        {
            warnings.Add($"XML parse error, stopping at the point of failure: {ex.Message}");
        }

        return new XmltvParseResult(programs, warnings);
    }

    private static bool TryParseXmltvDate(string raw, out DateTimeOffset result)
    {
        var trimmed = raw.Trim();

        // Normalize "+0000"/"-0500" style offsets (no colon) to "zzz" ("+00:00") which
        // DateTimeOffset.TryParseExact expects, and tolerate a missing offset entirely.
        var corePart = trimmed.Length >= 14 ? trimmed[..14] : trimmed;
        var offsetPart = trimmed.Length > 14 ? trimmed[14..].Trim() : string.Empty;

        if (offsetPart.Length == 5 && (offsetPart[0] == '+' || offsetPart[0] == '-'))
        {
            offsetPart = $"{offsetPart[..3]}:{offsetPart[3..]}";
        }

        if (offsetPart.Length > 0)
        {
            return DateTimeOffset.TryParseExact(
                $"{corePart} {offsetPart}", DateFormatWithOffset, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out result);
        }

        if (DateTime.TryParseExact(
                corePart, DateFormatNoOffset, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal, out var utc))
        {
            result = new DateTimeOffset(utc, TimeSpan.Zero);
            return true;
        }

        result = default;
        return false;
    }
}
