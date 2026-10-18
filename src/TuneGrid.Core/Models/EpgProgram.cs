namespace TuneGrid.Core.Models;

/// <summary>A single scheduled program from XMLTV, for one EPG channel id.</summary>
public sealed class EpgProgram
{
    public required string EpgChannelId { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    public required DateTimeOffset Start { get; init; }

    public required DateTimeOffset Stop { get; init; }
}
