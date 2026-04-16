using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System.Runtime.InteropServices;

namespace MailForwarder.Services;

public static class AppIconService
{
    private static readonly Uri IconUri = new("avares://MailForwarder/Assets/mail-icon.ico");
    private static readonly Uri IconPngUri = new("avares://MailForwarder/Assets/mail-icon.png");
    private static WindowIcon? _defaultIcon;
    private static WindowIcon? _monitoringIcon;

    public static WindowIcon LoadWindowIcon()
    {
        return _defaultIcon ??= LoadIcon(IconUri);
    }

    public static WindowIcon LoadMonitoringWindowIcon()
    {
        return _monitoringIcon ??= CreateMonitoringIcon();
    }

    private static WindowIcon LoadIcon(Uri uri)
    {
        using var stream = AssetLoader.Open(uri);
        return new WindowIcon(stream);
    }

    private static WindowIcon CreateMonitoringIcon()
    {
        using var stream = AssetLoader.Open(IconPngUri);
        using var bitmap = WriteableBitmap.DecodeToWidth(stream, 256, BitmapInterpolationMode.HighQuality);
        using var framebuffer = bitmap.Lock();

        var bytes = new byte[framebuffer.RowBytes * framebuffer.Size.Height];
        Marshal.Copy(framebuffer.Address, bytes, 0, bytes.Length);

        for (var y = 0; y < framebuffer.Size.Height; y++)
        {
            var rowOffset = y * framebuffer.RowBytes;

            for (var x = 0; x < framebuffer.Size.Width; x++)
            {
                var pixelOffset = rowOffset + (x * 4);
                var blue = bytes[pixelOffset];
                var green = bytes[pixelOffset + 1];
                var red = bytes[pixelOffset + 2];
                var alpha = bytes[pixelOffset + 3];

                if (!IsNavyBackground(red, green, blue, alpha))
                {
                    continue;
                }

                var brightness = Math.Clamp((red + green + blue) / 294d, 0.55d, 1.15d);
                bytes[pixelOffset] = (byte)Math.Clamp(69 * brightness, 0, 255);
                bytes[pixelOffset + 1] = (byte)Math.Clamp(58 * brightness, 0, 255);
                bytes[pixelOffset + 2] = (byte)Math.Clamp(143 * brightness, 0, 255);
            }
        }

        Marshal.Copy(bytes, 0, framebuffer.Address, bytes.Length);

        using var iconStream = new MemoryStream();
        bitmap.Save(iconStream);
        iconStream.Position = 0;
        return new WindowIcon(iconStream);
    }

    private static bool IsNavyBackground(byte red, byte green, byte blue, byte alpha)
    {
        return alpha > 0 &&
               red < 90 &&
               green < 135 &&
               blue < 170 &&
               blue > red + 30;
    }
}
