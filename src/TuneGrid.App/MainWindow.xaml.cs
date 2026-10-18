using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TuneGrid.App.Views;

namespace TuneGrid.App;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ContentFrame.Navigate(typeof(GuidePage));
    }

    private void RootNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        var tag = (args.SelectedItem as NavigationViewItem)?.Tag as string;
        switch (tag)
        {
            case "Guide":
                ContentFrame.Navigate(typeof(GuidePage));
                break;
            case "PlaylistSetup":
                ContentFrame.Navigate(typeof(PlaylistSetupPage));
                break;
        }
    }
}
