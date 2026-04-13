using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MailForwarder.ViewModels;

namespace MailForwarder.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void Pop3ShowPasswordChanged(object? sender, RoutedEventArgs e)
    {
        if (this.FindControl<TextBox>("Pop3PasswordTextBox") is { } textBox &&
            sender is CheckBox checkBox)
        {
            textBox.PasswordChar = checkBox.IsChecked == true ? default : '*';
        }
    }

    private void SmtpShowPasswordChanged(object? sender, RoutedEventArgs e)
    {
        if (this.FindControl<TextBox>("SmtpPasswordTextBox") is { } textBox &&
            sender is CheckBox checkBox)
        {
            textBox.PasswordChar = checkBox.IsChecked == true ? default : '*';
        }
    }

    private void TestPop3Clicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel)
        {
            viewModel.TestPop3Command.Execute(null);
        }
    }

    private void TestSmtpClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel)
        {
            viewModel.TestSmtpCommand.Execute(null);
        }
    }

    private void SaveClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel)
        {
            viewModel.SaveCommand.Execute(null);
        }
    }
}
