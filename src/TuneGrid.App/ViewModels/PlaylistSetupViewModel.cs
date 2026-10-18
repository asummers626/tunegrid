using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TuneGrid.Core.Models;
using TuneGrid.Core.Parsing;
using TuneGrid.Core.Storage;
using TuneGrid.Core.Xtream;

namespace TuneGrid.App.ViewModels;

public enum PlaylistSetupMode
{
    M3uUrl,
    Xtream,
}

/// <summary>Drives the "add playlist" screen: M3U URL or Xtream credentials in, parsed channels persisted out.</summary>
public partial class PlaylistSetupViewModel : ObservableObject
{
    private readonly AppDatabase _database;
    private readonly HttpClient _httpClient;

    [ObservableProperty]
    private PlaylistSetupMode _mode = PlaylistSetupMode.M3uUrl;

    [ObservableProperty]
    private string _playlistName = string.Empty;

    [ObservableProperty]
    private string _m3uUrl = string.Empty;

    [ObservableProperty]
    private string _xtreamServerUrl = string.Empty;

    [ObservableProperty]
    private string _xtreamUsername = string.Empty;

    [ObservableProperty]
    private string _xtreamPassword = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _statusMessage;

    public PlaylistSetupViewModel(AppDatabase database, HttpClient httpClient)
    {
        _database = database;
        _httpClient = httpClient;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(PlaylistName))
        {
            StatusMessage = "Give this playlist a name first.";
            return;
        }

        IsLoading = true;
        StatusMessage = null;

        try
        {
            var playlistId = Guid.NewGuid().ToString("N");

            if (Mode == PlaylistSetupMode.M3uUrl)
            {
                await SaveM3uPlaylistAsync(playlistId);
            }
            else
            {
                await SaveXtreamPlaylistAsync(playlistId);
            }
        }
        catch (HttpRequestException ex)
        {
            StatusMessage = $"Couldn't reach the server: {ex.Message}";
        }
        catch (UriFormatException)
        {
            StatusMessage = "That server URL doesn't look valid.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SaveM3uPlaylistAsync(string playlistId)
    {
        if (!Uri.TryCreate(M3uUrl, UriKind.Absolute, out var uri))
        {
            StatusMessage = "That playlist URL doesn't look valid.";
            return;
        }

        var content = await _httpClient.GetStringAsync(uri);
        var parsed = M3uPlaylistParser.Parse(content, playlistId);

        _database.UpsertPlaylist(new Playlist
        {
            Id = playlistId,
            Name = PlaylistName,
            SourceType = PlaylistSourceType.M3uUrl,
            M3uLocation = M3uUrl,
            LastRefreshedAt = DateTimeOffset.UtcNow,
        });
        _database.ReplaceChannels(playlistId, parsed.Channels);

        StatusMessage = parsed.Warnings.Count == 0
            ? $"Added {parsed.Channels.Count} channels."
            : $"Added {parsed.Channels.Count} channels ({parsed.Warnings.Count} entries skipped — see log).";
    }

    private async Task SaveXtreamPlaylistAsync(string playlistId)
    {
        var credentials = new XtreamCredentials
        {
            ServerUrl = XtreamServerUrl,
            Username = XtreamUsername,
            Password = XtreamPassword,
        };

        var client = new XtreamClient(_httpClient);
        var channels = await client.GetLiveChannelsAsync(credentials, playlistId);

        _database.UpsertPlaylist(new Playlist
        {
            Id = playlistId,
            Name = PlaylistName,
            SourceType = PlaylistSourceType.Xtream,
            Xtream = credentials,
            LastRefreshedAt = DateTimeOffset.UtcNow,
        });
        _database.ReplaceChannels(playlistId, channels);

        StatusMessage = $"Added {channels.Count} channels.";
    }
}
