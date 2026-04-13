using System.IO;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using MailForwarder.ViewModels;
using MailForwarder.Views;

namespace MailForwarder.Services;

public sealed class TrayIconHost : IDisposable
{
    private readonly MainWindow _mainWindow;
    private readonly MainViewModel _mainViewModel;
    private readonly TrayIcon _trayIcon;

    public TrayIconHost(MainWindow mainWindow, MainViewModel mainViewModel)
    {
        _mainWindow = mainWindow;
        _mainViewModel = mainViewModel;

        var menu = new NativeMenu();
        var openItem = new NativeMenuItem("開く");
        var startItem = new NativeMenuItem("監視開始");
        var stopItem = new NativeMenuItem("監視停止");
        var separator = new NativeMenuItemSeparator();
        var exitItem = new NativeMenuItem("終了");

        menu.Items.Add(openItem);
        menu.Items.Add(startItem);
        menu.Items.Add(stopItem);
        menu.Items.Add(separator);
        menu.Items.Add(exitItem);

        openItem.Click += (_, _) => ShowMainWindow();
        startItem.Click += (_, _) => _mainViewModel.StartMonitoringCommand.Execute(null);
        stopItem.Click += (_, _) => _mainViewModel.StopMonitoringCommand.Execute(null);
        exitItem.Click += (_, _) => (App.Current as App)?.RequestShutdown();

        _trayIcon = new TrayIcon
        {
            ToolTipText = "メール自動転送",
            IsVisible = true,
            Menu = menu,
            Icon = BuildIcon()
        };

        _trayIcon.Clicked += (_, _) => ShowMainWindow();
    }

    public void ShowMainWindow()
    {
        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    public void Dispose()
    {
        _trayIcon.Dispose();
    }

    private static WindowIcon BuildIcon()
    {
        return new WindowIcon(new MemoryStream(BuildIcoBytes()));
    }

    private static byte[] BuildIcoBytes()
    {
        const int size = 16;
        const int pixelBytes = size * size * 4;
        const int maskBytes = size * 4;
        const int imageBytes = 40 + pixelBytes + maskBytes;

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        // ICONDIR
        writer.Write((ushort)0);
        writer.Write((ushort)1);
        writer.Write((ushort)1);

        // ICONDIRENTRY
        writer.Write((byte)size);
        writer.Write((byte)size);
        writer.Write((byte)0);
        writer.Write((byte)0);
        writer.Write((ushort)1);
        writer.Write((ushort)32);
        writer.Write(imageBytes);
        writer.Write(22);

        // BITMAPINFOHEADER
        writer.Write(40);
        writer.Write(size);
        writer.Write(size * 2);
        writer.Write((ushort)1);
        writer.Write((ushort)32);
        writer.Write(0);
        writer.Write(pixelBytes);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);

        for (var y = size - 1; y >= 0; y--)
        {
            for (var x = 0; x < size; x++)
            {
                var isBorder = x == 0 || y == 0 || x == size - 1 || y == size - 1;
                var isEnvelopeBody = y >= 4 && y <= 11;
                var isFlap = y >= 4 && Math.Abs(x - 7.5) <= y - 4;

                byte red = 0xF4;
                byte green = 0xF8;
                byte blue = 0xFB;
                byte alpha = 0xFF;

                if (isBorder)
                {
                    red = 0x4B;
                    green = 0x64;
                    blue = 0x78;
                }
                else if (isEnvelopeBody)
                {
                    red = 0xE7;
                    green = 0xF0;
                    blue = 0xF7;
                }

                if (isFlap && y <= 8)
                {
                    red = 0xD8;
                    green = 0xE3;
                    blue = 0xEC;
                }

                writer.Write(blue);
                writer.Write(green);
                writer.Write(red);
                writer.Write(alpha);
            }
        }

        for (var i = 0; i < maskBytes; i++)
        {
            writer.Write((byte)0);
        }

        writer.Flush();
        return stream.ToArray();
    }
}
