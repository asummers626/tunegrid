using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using TuneGrid.Core.Models;

namespace TuneGrid.App.ViewModels;

/// <summary>One row of the EPG grid: a channel plus its programs for the visible time window.</summary>
public sealed class GuideRow
{
    public required Channel Channel { get; init; }
    public required IReadOnlyList<EpgProgram> Programs { get; init; }
}

/// <summary>
/// Drives the grid EPG: channels down the side, programs across time. <see cref="Programs"/>
/// is matched to channels via <see cref="Channel.EpgChannelId"/>, which may be absent — rows
/// with no EPG match still show up with an empty program list rather than being dropped.
/// </summary>
public partial class GuideViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<GuideRow> _rows = [];

    [ObservableProperty]
    private DateTimeOffset _windowStart = DateTimeOffset.UtcNow;

    [ObservableProperty]
    private TimeSpan _windowLength = TimeSpan.FromHours(4);

    public void Load(IReadOnlyList<Channel> channels, IReadOnlyList<EpgProgram> allPrograms)
    {
        var byEpgChannel = allPrograms
            .GroupBy(p => p.EpgChannelId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<EpgProgram>)g.OrderBy(p => p.Start).ToList());

        Rows = new ObservableCollection<GuideRow>(
            channels.Select(channel => new GuideRow
            {
                Channel = channel,
                Programs = channel.EpgChannelId is not null && byEpgChannel.TryGetValue(channel.EpgChannelId, out var programs)
                    ? programs
                    : [],
            }));
    }

    /// <summary>The program airing "now" for a row, if any is in <see cref="WindowStart"/>'s instant.</summary>
    public EpgProgram? CurrentProgramFor(GuideRow row) =>
        row.Programs.FirstOrDefault(p => p.Start <= WindowStart && WindowStart < p.Stop);
}
