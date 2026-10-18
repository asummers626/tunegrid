using LibVLCSharp.Shared;

namespace TuneGrid.App.Services;

/// <summary>
/// Wraps LibVLCSharp's <see cref="MediaPlayer"/>. Handles HLS, MPEG-TS, and progressive
/// HTTP streams, which is what IPTV providers serve. Errors (bad/unreachable stream URL,
/// unsupported codec, provider-side failure) surface as <see cref="PlaybackState.Error"/>
/// via <see cref="StateChanged"/> rather than throwing into caller code.
/// </summary>
public sealed class LibVlcPlaybackEngine : IPlaybackEngine
{
    private readonly LibVLC _libVlc;
    private readonly MediaPlayer _mediaPlayer;
    private Media? _currentMedia;

    public PlaybackState State { get; private set; } = PlaybackState.Stopped;

    public event EventHandler<PlaybackStateChangedEventArgs>? StateChanged;

    public LibVlcPlaybackEngine(LibVLC libVlc)
    {
        _libVlc = libVlc;
        _mediaPlayer = new MediaPlayer(_libVlc);

        _mediaPlayer.Buffering += (_, _) => SetState(PlaybackState.Buffering);
        _mediaPlayer.Playing += (_, _) => SetState(PlaybackState.Playing);
        _mediaPlayer.Paused += (_, _) => SetState(PlaybackState.Paused);
        _mediaPlayer.Stopped += (_, _) => SetState(PlaybackState.Stopped);
        _mediaPlayer.EncounteredError += (_, _) => SetState(PlaybackState.Error, "Playback failed — the stream may be offline or unreachable.");
    }

    /// <summary>The native video surface for this engine; attach to a VideoView's MediaPlayer property.</summary>
    public MediaPlayer NativeMediaPlayer => _mediaPlayer;

    public void Play(Uri streamUrl)
    {
        if (streamUrl.Scheme != Uri.UriSchemeHttp && streamUrl.Scheme != Uri.UriSchemeHttps)
        {
            SetState(PlaybackState.Error, "Refusing to play a non-HTTP(S) stream URL.");
            return;
        }

        _currentMedia?.Dispose();
        _currentMedia = new Media(_libVlc, streamUrl);
        _mediaPlayer.Play(_currentMedia);
    }

    public void Stop()
    {
        _mediaPlayer.Stop();
    }

    private void SetState(PlaybackState state, string? errorMessage = null)
    {
        State = state;
        StateChanged?.Invoke(this, new PlaybackStateChangedEventArgs(state, errorMessage));
    }

    public void Dispose()
    {
        _mediaPlayer.Dispose();
        _currentMedia?.Dispose();
    }
}
