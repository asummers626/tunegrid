using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TuneGrid.App.Services;
using TuneGrid.Core.Models;

namespace TuneGrid.App.ViewModels;

/// <summary>Drives the player screen: current channel, playback state, play/stop.</summary>
public partial class PlayerViewModel : ObservableObject
{
    private readonly IPlaybackEngine _engine;

    [ObservableProperty]
    private Channel? _currentChannel;

    [ObservableProperty]
    private PlaybackState _playbackState = PlaybackState.Stopped;

    [ObservableProperty]
    private string? _errorMessage;

    public PlayerViewModel(IPlaybackEngine engine)
    {
        _engine = engine;
        _engine.StateChanged += OnEngineStateChanged;
    }

    [RelayCommand]
    private void PlayChannel(Channel channel)
    {
        CurrentChannel = channel;
        ErrorMessage = null;
        _engine.Play(channel.StreamUrl);
    }

    [RelayCommand]
    private void Stop()
    {
        _engine.Stop();
    }

    private void OnEngineStateChanged(object? sender, PlaybackStateChangedEventArgs e)
    {
        PlaybackState = e.State;
        ErrorMessage = e.ErrorMessage;
    }
}
