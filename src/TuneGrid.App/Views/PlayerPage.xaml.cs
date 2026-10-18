using LibVLCSharp.Shared;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using TuneGrid.App.Services;
using TuneGrid.App.ViewModels;
using TuneGrid.Core.Models;

namespace TuneGrid.App.Views;

public sealed partial class PlayerPage : Page
{
    private LibVLC? _libVlc;
    private LibVlcPlaybackEngine? _engine;
    private PlayerViewModel? _viewModel;

    public PlayerPage()
    {
        InitializeComponent();
        Unloaded += (_, _) => _engine?.Dispose();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is not Channel channel)
        {
            return;
        }

        Core.Initialize(); // LibVLCSharp native lib bootstrap
        _libVlc = new LibVLC();
        _engine = new LibVlcPlaybackEngine(_libVlc);
        _viewModel = new PlayerViewModel(_engine);
        _viewModel.PropertyChanged += (_, _) =>
        {
            StateText.Text = _viewModel.PlaybackState.ToString();
        };

        PlayerVideoView.MediaPlayer = _engine.NativeMediaPlayer;

        ChannelNameText.Text = channel.Name;
        _viewModel.PlayChannelCommand.Execute(channel);
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel?.StopCommand.Execute(null);
    }
}
