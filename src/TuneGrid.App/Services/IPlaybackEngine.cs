namespace TuneGrid.App.Services;

public enum PlaybackState
{
    Stopped,
    Buffering,
    Playing,
    Paused,
    Error,
}

public sealed class PlaybackStateChangedEventArgs(PlaybackState state, string? errorMessage) : EventArgs
{
    public PlaybackState State { get; } = state;
    public string? ErrorMessage { get; } = errorMessage;
}

/// <summary>
/// Playback is isolated behind this interface so the UI/view-model layer never depends on
/// LibVLC directly — it can be unit-tested with a fake, and the engine could be swapped
/// (e.g. for mpv) without touching views.
/// </summary>
public interface IPlaybackEngine : IDisposable
{
    PlaybackState State { get; }

    event EventHandler<PlaybackStateChangedEventArgs>? StateChanged;

    void Play(Uri streamUrl);

    void Stop();
}
