using Avalonia;
using System.Runtime.InteropServices;
using System.Threading;

namespace MailForwarder;

internal static class Program
{
    private const string SingleInstanceMutexName = @"Global\MailForwarder";
    private const string ActivateMainWindowEventName = @"Global\MailForwarder.ActivateMainWindow";
    private static Mutex? _singleInstanceMutex;

    [STAThread]
    public static void Main(string[] args)
    {
        if (!TryAcquireSingleInstance())
        {
            SignalExistingInstance();
            return;
        }

        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args, shutdownMode: Avalonia.Controls.ShutdownMode.OnExplicitShutdown);
        }
        finally
        {
            ReleaseSingleInstance();
        }
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
    }

    private static bool TryAcquireSingleInstance()
    {
        _singleInstanceMutex = new Mutex(initiallyOwned: false, SingleInstanceMutexName);

        try
        {
            return _singleInstanceMutex.WaitOne(0, exitContext: false);
        }
        catch (AbandonedMutexException)
        {
            return true;
        }
    }

    private static void ReleaseSingleInstance()
    {
        if (_singleInstanceMutex is null)
        {
            return;
        }

        try
        {
            _singleInstanceMutex.ReleaseMutex();
        }
        catch (ApplicationException)
        {
            // Ignore cases where the mutex was not acquired.
        }
        finally
        {
            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
        }
    }

    private static void NotifyAlreadyRunning()
    {
        const string message = "MailForwarder はすでに起動しています。";
        const string caption = "メール自動転送";

        if (OperatingSystem.IsWindows())
        {
            _ = MessageBoxW(IntPtr.Zero, message, caption, 0x00000040);
            return;
        }

        Console.Error.WriteLine(message);
    }

    public static EventWaitHandle CreateActivationEvent()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Single-instance activation is only supported on Windows.");
        }

        return new EventWaitHandle(
            initialState: false,
            EventResetMode.AutoReset,
            ActivateMainWindowEventName);
    }

    private static void SignalExistingInstance()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            using var activateEvent = EventWaitHandle.OpenExisting(ActivateMainWindowEventName);
            activateEvent.Set();
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            NotifyAlreadyRunning();
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);
}
