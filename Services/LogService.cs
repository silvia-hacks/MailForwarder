using Avalonia.Threading;
using System.Collections.ObjectModel;
using MailForwarder.Models;

namespace MailForwarder.Services;

public sealed class LogService
{
    private readonly string _logDirectory;
    private readonly string _logPath;

    public LogService()
    {
        _logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MailForwarder",
            "logs");
        _logPath = Path.Combine(_logDirectory, "app.log");
        Directory.CreateDirectory(_logDirectory);
    }

    public ObservableCollection<LogEntry> Entries { get; } = new();

    public event EventHandler<LogEntry>? EntryAdded;

    public string LogPath => _logPath;

    public void AddInfo(string message) => Add("情報", message);

    public void AddDebug(string message) => Add("デバッグ", message);

    public void AddError(string message) => Add("エラー", message);

    private void Add(string level, string message)
    {
        var entry = new LogEntry
        {
            Timestamp = DateTimeOffset.Now,
            Level = level,
            Message = message
        };

        AppendToFile(entry);
        Dispatcher.UIThread.Post(() =>
        {
            if (!string.Equals(entry.Level, "デバッグ", StringComparison.OrdinalIgnoreCase))
            {
                Entries.Insert(0, entry);
            }

            EntryAdded?.Invoke(this, entry);
        });
    }

    private void AppendToFile(LogEntry entry)
    {
        Directory.CreateDirectory(_logDirectory);
        var line = $"{entry.Timestamp:yyyy-MM-dd HH:mm:ss} [{entry.Level}] {entry.Message}{Environment.NewLine}";
        File.AppendAllText(_logPath, line);
    }
}
