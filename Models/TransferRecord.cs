namespace MailForwarder.Models;

public sealed class TransferRecord
{
    public string MessageKey { get; set; } = string.Empty;
    public DateTimeOffset ForwardedAt { get; set; } = DateTimeOffset.Now;
}
