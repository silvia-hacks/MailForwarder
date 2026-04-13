using MailForwarder.Models;
using MailKit;
using MailKit.Net.Pop3;
using MailKit.Security;
using MimeKit;

namespace MailForwarder.Services;

public sealed class MailReceiveService
{
    public ConnectionTestResult TestConnection(AppSettings settings)
    {
        var protocolLogPath = CreateProtocolLogPath("pop3-test");
        try
        {
            using var client = new Pop3Client(new ProtocolLogger(protocolLogPath));
            ConnectAndAuthenticate(client, settings);
            client.Disconnect(true);
        }
        catch (Exception ex)
        {
            ex.Data["ProtocolLogPath"] = protocolLogPath;
            throw;
        }

        return new ConnectionTestResult
        {
            ProtocolLogPath = protocolLogPath
        };
    }

    public IList<ReceivedMessage> ReceiveMessages(AppSettings settings)
    {
        using var client = Connect(settings);

        var messages = new List<ReceivedMessage>(client.Count);

        for (var index = 0; index < client.Count; index++)
        {
            var message = client.GetMessage(index);
            messages.Add(new ReceivedMessage
            {
                Index = index,
                Message = message,
                MessageKey = BuildMessageKey(message)
            });
        }

        client.Disconnect(true);
        return messages;
    }

    public void DeleteMessages(AppSettings settings, IEnumerable<int> indexes)
    {
        var indexList = indexes
            .Distinct()
            .OrderBy(index => index)
            .ToList();

        if (indexList.Count == 0)
        {
            return;
        }

        using var client = Connect(settings);

        foreach (var index in indexList)
        {
            if (index >= 0 && index < client.Count)
            {
                client.DeleteMessage(index);
            }
        }

        client.Disconnect(true);
    }

    private static Pop3Client Connect(AppSettings settings)
    {
        var client = new Pop3Client();
        ConnectAndAuthenticate(client, settings);
        return client;
    }

    private static void ConnectAndAuthenticate(Pop3Client client, AppSettings settings)
    {
        client.Connect(
            settings.Pop3.Host,
            settings.Pop3.Port,
            settings.Pop3.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable);

        // Some servers advertise APOP but reject it. Force USER/PASS fallback.
        if (client.Capabilities.HasFlag(Pop3Capabilities.Apop))
        {
            client.Capabilities &= ~Pop3Capabilities.Apop;
        }

        client.Authenticate(settings.Pop3.Username, settings.Pop3.Password);
    }

    private static string CreateProtocolLogPath(string prefix)
    {
        var baseDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MailForwarder",
            "logs");
        Directory.CreateDirectory(baseDirectory);
        return Path.Combine(baseDirectory, $"{prefix}-{DateTime.Now:yyyyMMdd-HHmmss}.log");
    }

    private static string BuildMessageKey(MimeMessage message)
    {
        if (!string.IsNullOrWhiteSpace(message.MessageId))
        {
            return message.MessageId.Trim();
        }

        var from = message.From.Mailboxes.FirstOrDefault()?.Address ?? string.Empty;
        var subject = message.Subject ?? string.Empty;
        var date = message.Date.ToUniversalTime().ToString("O");

        return $"{from}|{subject}|{date}";
    }
}
