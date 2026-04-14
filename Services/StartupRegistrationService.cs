using Microsoft.Win32;

namespace MailForwarder.Services;

public sealed class StartupRegistrationService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppValueName = "MailForwarder";

    public bool IsEnabled()
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        using var runKey = Registry.CurrentUser.OpenSubKey(RunKeyPath);
        var value = runKey?.GetValue(AppValueName) as string;
        return !string.IsNullOrWhiteSpace(value);
    }

    public void Apply(bool enabled)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using var runKey = Registry.CurrentUser.CreateSubKey(RunKeyPath);

        if (!enabled)
        {
            runKey.DeleteValue(AppValueName, throwOnMissingValue: false);
            return;
        }

        var executablePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            throw new InvalidOperationException("実行ファイルのパスを取得できませんでした。");
        }

        runKey.SetValue(AppValueName, $"\"{executablePath}\"");
    }
}
