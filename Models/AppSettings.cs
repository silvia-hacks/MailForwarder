namespace MailForwarder.Models;

public sealed class AppSettings
{
    public Pop3Settings Pop3 { get; set; } = new();
    public SmtpSettings Smtp { get; set; } = new();
    public ForwardSettings Forward { get; set; } = new();
}

public sealed class Pop3Settings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 995;
    public bool UseSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 465;
    public bool UseSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class ForwardSettings
{
    public string ToAddress { get; set; } = string.Empty;
    public int PollIntervalMinutes { get; set; } = 5;
    public bool DeleteAfterForward { get; set; }
}
