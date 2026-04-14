using MailForwarder.Models;
using MailForwarder.Services;

namespace MailForwarder.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService;
    private readonly LogService _logService;
    private readonly MailReceiveService _mailReceiveService;
    private readonly MailForwardService _mailForwardService;
    private readonly StartupRegistrationService _startupRegistrationService;
    private AppSettings _settings;
    private string _testResultMessage = "まだ接続確認を実行していません。";
    private bool _isTestResultError;

    public SettingsViewModel(
        SettingsService settingsService,
        LogService logService,
        MailReceiveService mailReceiveService,
        MailForwardService mailForwardService,
        StartupRegistrationService startupRegistrationService)
    {
        _settingsService = settingsService;
        _logService = logService;
        _mailReceiveService = mailReceiveService;
        _mailForwardService = mailForwardService;
        _startupRegistrationService = startupRegistrationService;
        _settings = _settingsService.Load();
        if (OperatingSystem.IsWindows())
        {
            _settings.General.LaunchOnWindowsStartup = _startupRegistrationService.IsEnabled();
        }

        SaveCommand = new RelayCommand(_ => Save());
        TestPop3Command = new RelayCommand(_ => TestPop3());
        TestSmtpCommand = new RelayCommand(_ => TestSmtp());
    }

    public RelayCommand SaveCommand { get; }

    public RelayCommand TestPop3Command { get; }

    public RelayCommand TestSmtpCommand { get; }

    public AppSettings Settings
    {
        get => _settings;
        set => SetProperty(ref _settings, value);
    }

    public string TestResultMessage
    {
        get => _testResultMessage;
        private set => SetProperty(ref _testResultMessage, value);
    }

    public bool IsTestResultError
    {
        get => _isTestResultError;
        private set => SetProperty(ref _isTestResultError, value);
    }

    public event EventHandler? Saved;

    private void Save()
    {
        _settingsService.Save(Settings);
        _startupRegistrationService.Apply(Settings.General.LaunchOnWindowsStartup);
        _logService.AddInfo("設定を保存しました。");
        Saved?.Invoke(this, EventArgs.Empty);
    }

    private void TestPop3()
    {
        try
        {
            var result = _mailReceiveService.TestConnection(Settings);
            var message = "POP3接続確認に成功しました。";
            if (!string.IsNullOrWhiteSpace(result.ProtocolLogPath))
            {
                message += $"{Environment.NewLine}プロトコルログ: {result.ProtocolLogPath}";
            }

            _logService.AddInfo(message);
            IsTestResultError = false;
            TestResultMessage = message;
        }
        catch (Exception ex)
        {
            var details = BuildExceptionDetails(ex);
            var protocolLogPath = TryExtractProtocolLogPath(ex);
            var message = $"POP3接続確認に失敗しました: {details}";
            if (!string.IsNullOrWhiteSpace(protocolLogPath))
            {
                message += $"{Environment.NewLine}プロトコルログ: {protocolLogPath}";
            }

            _logService.AddError(message);
            IsTestResultError = true;
            TestResultMessage = message;
        }
    }

    private void TestSmtp()
    {
        try
        {
            _mailForwardService.TestConnection(Settings);
            _logService.AddInfo("SMTP接続確認に成功しました。");
            IsTestResultError = false;
            TestResultMessage = "SMTP接続確認に成功しました。";
        }
        catch (Exception ex)
        {
            var details = BuildExceptionDetails(ex);
            _logService.AddError($"SMTP接続確認に失敗しました: {details}");
            IsTestResultError = true;
            TestResultMessage = $"SMTP接続確認に失敗しました: {details}";
        }
    }

    private static string BuildExceptionDetails(Exception ex)
    {
        var parts = new List<string>
        {
            $"{ex.GetType().Name}: {ex.Message}"
        };

        if (ex.InnerException is not null)
        {
            parts.Add($"内部例外: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
        }

        return string.Join(" | ", parts);
    }

    private static string? TryExtractProtocolLogPath(Exception ex)
    {
        return ex.Data.Contains("ProtocolLogPath")
            ? ex.Data["ProtocolLogPath"] as string
            : null;
    }
}
