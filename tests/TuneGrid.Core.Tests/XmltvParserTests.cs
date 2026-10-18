using System.Text;
using TuneGrid.Core.Parsing;
using Xunit;

namespace TuneGrid.Core.Tests;

public class XmltvParserTests
{
    [Fact]
    public void Parse_WellFormedXmltv_ReturnsPrograms()
    {
        const string xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <tv>
              <channel id="bbc1.uk"><display-name>BBC One</display-name></channel>
              <programme start="20260101120000 +0000" stop="20260101130000 +0000" channel="bbc1.uk">
                <title>News at Noon</title>
                <desc>The latest headlines.</desc>
              </programme>
            </tv>
            """;

        var result = XmltvParser.Parse(ToStream(xml));

        Assert.Empty(result.Warnings);
        Assert.Single(result.Programs);
        var program = result.Programs[0];
        Assert.Equal("bbc1.uk", program.EpgChannelId);
        Assert.Equal("News at Noon", program.Title);
        Assert.Equal("The latest headlines.", program.Description);
        Assert.Equal(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero), program.Start);
        Assert.Equal(new DateTimeOffset(2026, 1, 1, 13, 0, 0, TimeSpan.Zero), program.Stop);
    }

    [Fact]
    public void Parse_EmptyDocument_ReturnsNoPrograms()
    {
        var result = XmltvParser.Parse(ToStream("<tv></tv>"));

        Assert.Empty(result.Programs);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Parse_ProgrammeMissingTitle_IsSkippedWithWarning()
    {
        const string xml = """
            <tv>
              <programme start="20260101120000 +0000" stop="20260101130000 +0000" channel="x">
                <desc>No title here.</desc>
              </programme>
            </tv>
            """;

        var result = XmltvParser.Parse(ToStream(xml));

        Assert.Empty(result.Programs);
        Assert.Single(result.Warnings);
    }

    [Fact]
    public void Parse_ProgrammeWithUnparsableTimestamp_IsSkippedNotThrown()
    {
        const string xml = """
            <tv>
              <programme start="not-a-date" stop="20260101130000 +0000" channel="x">
                <title>Whatever</title>
              </programme>
            </tv>
            """;

        var result = XmltvParser.Parse(ToStream(xml));

        Assert.Empty(result.Programs);
        Assert.Single(result.Warnings);
    }

    [Fact]
    public void Parse_MalformedXml_StopsGracefullyRatherThanThrowing()
    {
        const string xml = "<tv><programme start=\"20260101120000 +0000\" stop=\"20260101130000 +0000\" channel=\"x\"><title>Oops";

        var result = XmltvParser.Parse(ToStream(xml));

        // Must not throw; a parse-error warning is acceptable, zero programs is acceptable.
        Assert.NotNull(result);
    }

    private static Stream ToStream(string xml) => new MemoryStream(Encoding.UTF8.GetBytes(xml));
}
