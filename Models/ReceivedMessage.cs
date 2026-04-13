using MimeKit;

namespace MailForwarder.Models;

public sealed class ReceivedMessage
{
    public required int Index { get; init; }
    public required string MessageKey { get; init; }
    public required MimeMessage Message { get; init; }
}
