using MailForwarder.Models;
using Avalonia.Threading;

namespace MailForwarder.Services;

public sealed class MonitorService : IDisposable
{
    private readonly SettingsService _settingsService;
    private readonly LogService _logService;
    private readonly TransferHistoryService _historyService;
    private readonly MailReceiveService _mailReceiveService;
    private readonly MailForwardService _mailForwardService;
    private readonly Timer _timer;
    private readonly SemaphoreSlim _cycleLock = new(1, 1);
    private int _intervalMinutes = 5;

    public MonitorService(
        SettingsService settingsService,
        LogService logService,
        TransferHistoryService historyService,
        MailReceiveService mailReceiveService,
        MailForwardService mailForwardService)
    {
        _settingsService = settingsService;
        _logService = logService;
        _historyService = historyService;
        _mailReceiveService = mailReceiveService;
        _mailForwardService = mailForwardService;

        _timer = new Timer(_ => ExecuteCycle(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    public bool IsRunning { get; private set; }

    public bool IsCycleRunning { get; private set; }

    public DateTimeOffset? LastCheckAt { get; private set; }

    public DateTimeOffset? LastForwardAt { get; private set; }

    public event EventHandler? StateChanged;

    public void Start()
    {
        var settings = _settingsService.Load();
        _intervalMinutes = Math.Max(1, settings.Forward.PollIntervalMinutes);
        _timer.Change(TimeSpan.Zero, TimeSpan.FromMinutes(_intervalMinutes));
        IsRunning = true;

        _logService.AddInfo($"監視開始 ({_intervalMinutes}分間隔)");
        RaiseStateChanged();
    }

    public void Stop()
    {
        _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        IsRunning = false;
        _logService.AddInfo("監視停止");
        RaiseStateChanged();
    }

    public void Reload()
    {
        if (IsRunning)
        {
            Stop();
            Start();
        }
    }

    public void ExecuteCycle()
    {
        _ = ExecuteCycleAsync(limitToSingleMessage: false);
    }

    public Task ExecuteSingleCycleAsync()
    {
        return ExecuteCycleAsync(limitToSingleMessage: true);
    }

    private Task ExecuteCycleAsync(bool limitToSingleMessage)
    {
        if (!_cycleLock.Wait(0))
        {
            return Task.CompletedTask;
        }

        IsCycleRunning = true;
        RaiseStateChanged();

        return Task.Run(() =>
        {
            try
            {
                ExecuteCycleCore(limitToSingleMessage);
            }
            finally
            {
                IsCycleRunning = false;
                RaiseStateChanged();
                _cycleLock.Release();
            }
        });
    }

    private void ExecuteCycleCore(bool limitToSingleMessage)
    {
        LastCheckAt = DateTimeOffset.Now;
        _logService.AddDebug(limitToSingleMessage ? "受信確認開始 (1通)" : "受信確認開始");

        var settings = _settingsService.Load();

        if (string.IsNullOrWhiteSpace(settings.Pop3.Host) ||
            string.IsNullOrWhiteSpace(settings.Smtp.Host) ||
            string.IsNullOrWhiteSpace(settings.Forward.ToAddress))
        {
            _logService.AddError("設定不足: POP3 / SMTP / 転送先を確認してください。");
            RaiseStateChanged();
            return;
        }

        try
        {
            var messages = _mailReceiveService.ReceiveMessages(settings);
            _logService.AddDebug($"受信 {messages.Count}件");

            var forwardedIndexes = new List<int>();
            var skippedTransferredCount = 0;
            var candidates = new List<ReceivedMessage>();

            foreach (var receivedMessage in messages)
            {
                if (_historyService.HasTransferred(receivedMessage.MessageKey))
                {
                    skippedTransferredCount++;
                    continue;
                }

                candidates.Add(receivedMessage);
            }

            if (limitToSingleMessage)
            {
                candidates = candidates.Take(1).ToList();
            }

            if (skippedTransferredCount > 0)
            {
                _logService.AddDebug($"転送済み {skippedTransferredCount}件をスキップ");
            }

            foreach (var receivedMessage in candidates)
            {
                _mailForwardService.Forward(settings, receivedMessage.Message);
                _historyService.MarkTransferred(receivedMessage.MessageKey);
                forwardedIndexes.Add(receivedMessage.Index);
                LastForwardAt = DateTimeOffset.Now;
                _logService.AddInfo($"転送: {receivedMessage.Message.Subject ?? "(件名なし)"}");
            }

            if (settings.Forward.DeleteAfterForward && forwardedIndexes.Count > 0)
            {
                _mailReceiveService.DeleteMessages(settings, forwardedIndexes);
                _logService.AddInfo($"サーバ削除 {forwardedIndexes.Count}件");
            }
        }
        catch (Exception ex)
        {
            _logService.AddError($"受信確認失敗: {ex.Message}");
        }

        RaiseStateChanged();
    }

    private void RaiseStateChanged()
    {
        Dispatcher.UIThread.Post(() => StateChanged?.Invoke(this, EventArgs.Empty));
    }

    public void Dispose()
    {
        _timer.Dispose();
        _cycleLock.Dispose();
    }
}
