using TuneGrid.Core.Parsing;
using Xunit;

namespace TuneGrid.Core.Tests;

public class M3uPlaylistParserTests
{
    [Fact]
    public void Parse_WellFormedPlaylist_ReturnsChannelsWithAttributes()
    {
        const string m3u = """
            #EXTM3U
            #EXTINF:-1 tvg-id="bbc1.uk" tvg-logo="http://x/bbc1.png" group-title="UK",BBC One
            http://example.com/bbc1.ts
            #EXTINF:-1 tvg-id="cnn.us" group-title="News",CNN
            http://example.com/cnn.m3u8
            """;

        var result = M3uPlaylistParser.Parse(m3u, "pl1");

        Assert.Empty(result.Warnings);
        Assert.Equal(2, result.Channels.Count);

        var bbc = result.Channels[0];
        Assert.Equal("bbc1.uk", bbc.Id);
        Assert.Equal("BBC One", bbc.Name);
        Assert.Equal("UK", bbc.Group);
        Assert.Equal("http://x/bbc1.png", bbc.LogoUrl);
        Assert.Equal("http://example.com/bbc1.ts", bbc.StreamUrl.ToString());
        Assert.Equal("bbc1.uk", bbc.EpgChannelId);
    }

    [Fact]
    public void Parse_EmptyInput_ReturnsNoChannelsAndAWarning()
    {
        var result = M3uPlaylistParser.Parse("", "pl1");

        Assert.Empty(result.Channels);
        Assert.Single(result.Warnings);
    }

    [Theory]
    [InlineData("#EXTM3U\n#EXTINF:-1,Orphan Name\n")] // #EXTINF with no following URL line
    [InlineData("#EXTM3U\nhttp://example.com/no-extinf.ts\n")] // URL with no preceding #EXTINF
    [InlineData("#EXTM3U\n#EXTINF:-1,Bad Scheme\nftp://example.com/stream.ts\n")] // non-HTTP(S) scheme rejected
    [InlineData("#EXTM3U\n#EXTINF:-1,No Scheme\nnot a url at all\n")] // unparsable URL
    public void Parse_MalformedEntries_AreSkippedNotThrown(string malformedM3u)
    {
        var result = M3uPlaylistParser.Parse(malformedM3u, "pl1");

        Assert.Empty(result.Channels);
        Assert.NotEmpty(result.Warnings);
    }

    [Fact]
    public void Parse_MissingTvgId_FallsBackToSyntheticId()
    {
        const string m3u = """
            #EXTM3U
            #EXTINF:-1,No Id Channel
            http://example.com/stream.ts
            """;

        var result = M3uPlaylistParser.Parse(m3u, "pl1");

        Assert.Single(result.Channels);
        Assert.Equal("pl1:0", result.Channels[0].Id);
        Assert.Null(result.Channels[0].EpgChannelId);
    }
}
