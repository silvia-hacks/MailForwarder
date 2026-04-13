using Avalonia.Controls;
using Avalonia.Interactivity;
using MailForwarder.ViewModels;

namespace MailForwarder.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
        Closing += OnClosing;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.OpenSettingsRequested -= OnOpenSettingsRequested;
            viewModel.OpenSettingsRequested += OnOpenSettingsRequested;
        }
    }

    private async void OnOpenSettingsRequested(object? sender, EventArgs e)
    {
        if (App.Current is not App app)
        {
            return;
        }

        var viewModel = new SettingsViewModel(
            app.SettingsService,
            app.LogService,
            app.MailReceiveService,
            app.MailForwardService);

        var window = new SettingsWindow
        {
            DataContext = viewModel
        };

        viewModel.Saved += (_, _) => window.Close();
        await window.ShowDialog(this);
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (App.Current is App { IsExitRequested: true })
        {
            return;
        }

        e.Cancel = true;
        Hide();
    }
}
