using MailForwarder.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace MailForwarder.Services;

public sealed class MailForwardService
{
    public void TestConnection(AppSettings settings)
    {
        using var client = Connect(settings);
        client.Disconnect(true);
    }

    public void Forward(AppSettings settings, MimeMessage originalMessage)
    {
        using var client = Connect(settings);
        var forwardMessage = BuildForwardMessage(settings, originalMessage);
        client.Send(forwardMessage);
        client.Disconnect(true);
    }

    private static MimeMessage BuildForwardMessage(AppSettings settings, MimeMessage originalMessage)
    {
        var forward = new MimeMessage();
        forward.From.Add(BuildSender(settings));
        forward.To.Add(MailboxAddress.Parse(settings.Forward.ToAddress));
        forward.Subject = BuildForwardSubject(originalMessage.Subject);

        if (originalMessage.ReplyTo.Count > 0)
        {
            forward.ReplyTo.AddRange(originalMessage.ReplyTo);
        }
        else if (originalMessage.From.Count > 0)
        {
            forward.ReplyTo.AddRange(originalMessage.From);
        }

        var bodyParts = new List<MimeEntity>
        {
            new TextPart("plain")
            {
                Text = BuildForwardHeader(originalMessage)
            }
        };

        if (originalMessage.Body is not null)
        {
            bodyParts.Add(originalMessage.Body);
        }

        if (bodyParts.Count == 1)
        {
            forward.Body = bodyParts[0];
        }
        else
        {
            var multipart = new Multipart("mixed");
            foreach (var part in bodyParts)
            {
                multipart.Add(part);
            }

            forward.Body = multipart;
        }

        return forward;
    }

    private static SmtpClient Connect(AppSettings settings)
    {
        var client = new SmtpClient();
        client.Connect(
            settings.Smtp.Host,
            settings.Smtp.Port,
            settings.Smtp.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable);
        client.Authenticate(settings.Smtp.Username, settings.Smtp.Password);
        return client;
    }

    private static MailboxAddress BuildSender(AppSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.Smtp.Username) && settings.Smtp.Username.Contains('@'))
        {
            return MailboxAddress.Parse(settings.Smtp.Username);
        }

        if (!string.IsNullOrWhiteSpace(settings.Pop3.Username) && settings.Pop3.Username.Contains('@'))
        {
            return MailboxAddress.Parse(settings.Pop3.Username);
        }

        return new MailboxAddress("メール自動転送", settings.Forward.ToAddress);
    }

    private static string BuildForwardSubject(string? subject)
    {
        if (string.IsNullOrWhiteSpace(subject))
        {
            return "Fwd: (件名なし)";
        }

        return subject.StartsWith("Fwd:", StringComparison.OrdinalIgnoreCase)
            ? subject
            : $"Fwd: {subject}";
    }

    private static string BuildForwardHeader(MimeMessage originalMessage)
    {
        var from = originalMessage.From.ToString();
        var to = originalMessage.To.ToString();
        var date = originalMessage.Date == DateTimeOffset.MinValue
            ? "-"
            : originalMessage.Date.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss");
        var subject = originalMessage.Subject ?? string.Empty;

        return string.Join(Environment.NewLine, new[]
        {
            "メール自動転送アプリによる自動転送です。",
            string.Empty,
            $"差出人: {from}",
            $"宛先: {to}",
            $"日時: {date}",
            $"件名: {subject}",
            string.Empty
        });
    }
}
