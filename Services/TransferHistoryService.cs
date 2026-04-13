using System.Text.Json;
using MailForwarder.Models;

namespace MailForwarder.Services;

public sealed class TransferHistoryService
{
    private readonly string _baseDirectory;
    private readonly string _historyPath;
    private readonly HashSet<string> _messageKeys = new(StringComparer.OrdinalIgnoreCase);

    public TransferHistoryService()
    {
        _baseDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MailForwarder");
        _historyPath = Path.Combine(_baseDirectory, "transfer-history.json");

        Load();
    }

    public bool HasTransferred(string messageKey) => _messageKeys.Contains(messageKey);

    public void MarkTransferred(string messageKey)
    {
        if (_messageKeys.Add(messageKey))
        {
            Save();
        }
    }

    private void Load()
    {
        Directory.CreateDirectory(_baseDirectory);

        if (!File.Exists(_historyPath))
        {
            return;
        }

        var json = File.ReadAllText(_historyPath);
        var records = JsonSerializer.Deserialize<List<TransferRecord>>(json) ?? new List<TransferRecord>();

        foreach (var record in records)
        {
            if (!string.IsNullOrWhiteSpace(record.MessageKey))
            {
                _messageKeys.Add(record.MessageKey);
            }
        }
    }

    private void Save()
    {
        Directory.CreateDirectory(_baseDirectory);

        var records = _messageKeys
            .Select(key => new TransferRecord
            {
                MessageKey = key,
                ForwardedAt = DateTimeOffset.Now
            })
            .ToList();

        var json = JsonSerializer.Serialize(records, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(_historyPath, json);
    }
}
