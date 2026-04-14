using Avalonia.Controls;
using Avalonia.Platform;

namespace MailForwarder.Services;

public static class AppIconService
{
    private static readonly Uri IconUri = new("avares://MailForwarder/Assets/mail-icon.ico");

    public static WindowIcon LoadWindowIcon()
    {
        using var stream = AssetLoader.Open(IconUri);
        return new WindowIcon(stream);
    }
}
