using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using TuneGrid.App.ViewModels;

namespace TuneGrid.App.Views;

public sealed partial class GuidePage : Page
{
    private readonly GuideViewModel _viewModel = new();

    public GuidePage()
    {
        InitializeComponent();

        // TODO: load the active playlist's channels + merged EPG from AppDatabase once
        // playlist selection exists; wired as an empty guide for now so the page renders.
        _viewModel.Load(channels: [], allPrograms: []);
        ChannelList.ItemsSource = _viewModel.Rows;
    }

    private void ChannelList_ItemDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (ChannelList.SelectedItem is not GuideRow row)
        {
            return;
        }

        Frame.Navigate(typeof(PlayerPage), row.Channel);
    }
}
