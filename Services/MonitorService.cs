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

    public DateTimeOffset? LastCheckAt { get; private set; }

    public DateTimeOffset? LastForwardAt { get; private set; }

    public event EventHandler? StateChanged;

    public void Start()
    {
        var settings = _settingsService.Load();
        _intervalMinutes = Math.Max(1, settings.Forward.PollIntervalMinutes);
        _timer.Change(TimeSpan.Zero, TimeSpan.FromMinutes(_intervalMinutes));
        IsRunning = true;

        _logService.AddInfo($"監視を開始しました。確認間隔: {_intervalMinutes} 分");
        RaiseStateChanged();
    }

    public void Stop()
    {
        _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        IsRunning = false;
        _logService.AddInfo("監視を停止しました。");
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
        if (!_cycleLock.Wait(0))
        {
            return;
        }

        try
        {
            ExecuteCycleCore(limitToSingleMessage: false);
        }
        finally
        {
            _cycleLock.Release();
        }
    }

    public void ExecuteSingleCycle()
    {
        if (!_cycleLock.Wait(0))
        {
            return;
        }

        try
        {
            ExecuteCycleCore(limitToSingleMessage: true);
        }
        finally
        {
            _cycleLock.Release();
        }
    }

    private void ExecuteCycleCore(bool limitToSingleMessage)
    {
        LastCheckAt = DateTimeOffset.Now;
        _logService.AddInfo(limitToSingleMessage ? "メール確認を開始しました。対象は1通です。" : "メール確認を開始しました。");

        var settings = _settingsService.Load();

        if (string.IsNullOrWhiteSpace(settings.Pop3.Host) ||
            string.IsNullOrWhiteSpace(settings.Smtp.Host) ||
            string.IsNullOrWhiteSpace(settings.Forward.ToAddress))
        {
            _logService.AddError("設定が不足しています。POP3、SMTP、転送先アドレスを確認してください。");
            RaiseStateChanged();
            return;
        }

        try
        {
            var messages = _mailReceiveService.ReceiveMessages(settings);
            _logService.AddInfo($"POP3から {messages.Count} 件のメールを取得しました。");

            var forwardedIndexes = new List<int>();

            var candidates = messages
                .Where(receivedMessage => !_historyService.HasTransferred(receivedMessage.MessageKey))
                .ToList();

            if (limitToSingleMessage)
            {
                candidates = candidates.Take(1).ToList();
            }

            foreach (var skippedMessage in messages.Except(candidates))
            {
                if (_historyService.HasTransferred(skippedMessage.MessageKey))
                {
                    _logService.AddInfo($"転送済みのためスキップしました: {skippedMessage.MessageKey}");
                }
            }

            foreach (var receivedMessage in candidates)
            {
                _mailForwardService.Forward(settings, receivedMessage.Message);
                _historyService.MarkTransferred(receivedMessage.MessageKey);
                forwardedIndexes.Add(receivedMessage.Index);
                LastForwardAt = DateTimeOffset.Now;
                _logService.AddInfo($"転送しました: {receivedMessage.Message.Subject ?? "(件名なし)"}");
            }

            if (settings.Forward.DeleteAfterForward && forwardedIndexes.Count > 0)
            {
                _mailReceiveService.DeleteMessages(settings, forwardedIndexes);
                _logService.AddInfo($"転送済みメールをサーバから {forwardedIndexes.Count} 件削除しました。");
            }
        }
        catch (Exception ex)
        {
            _logService.AddError($"メール確認に失敗しました: {ex.Message}");
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
