using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TuneGrid.App.Services;
using TuneGrid.App.ViewModels;
using TuneGrid.Core.Storage;

namespace TuneGrid.App.Views;

public sealed partial class PlaylistSetupPage : Page
{
    private readonly PlaylistSetupViewModel _viewModel;

    public PlaylistSetupPage()
    {
        InitializeComponent();

        // TODO: resolve these from a DI container once one is wired up; constructed
        // directly here for now so the page is self-contained during early scaffolding.
        var database = new AppDatabase(AppPaths.DatabaseFile);
        _viewModel = new PlaylistSetupViewModel(database, new HttpClient());
    }

    private void ModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var isXtream = ModeCombo.SelectedIndex == 1;
        M3uPanel.Visibility = isXtream ? Visibility.Collapsed : Visibility.Visible;
        XtreamPanel.Visibility = isXtream ? Visibility.Visible : Visibility.Collapsed;
        _viewModel.Mode = isXtream ? PlaylistSetupMode.Xtream : PlaylistSetupMode.M3uUrl;
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.PlaylistName = PlaylistNameBox.Text;
        _viewModel.M3uUrl = M3uUrlBox.Text;
        _viewModel.XtreamServerUrl = XtreamServerBox.Text;
        _viewModel.XtreamUsername = XtreamUsernameBox.Text;
        _viewModel.XtreamPassword = XtreamPasswordBox.Password;

        LoadingRing.IsActive = true;
        SaveButton.IsEnabled = false;

        await _viewModel.SaveCommand.ExecuteAsync(null);

        LoadingRing.IsActive = false;
        SaveButton.IsEnabled = true;
        StatusText.Text = _viewModel.StatusMessage;
    }
}
