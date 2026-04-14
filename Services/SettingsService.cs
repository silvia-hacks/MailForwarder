using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MailForwarder.Models;

namespace MailForwarder.Services;

public sealed class SettingsService
{
    private const string EncryptedPrefix = "dpapi:";
    private static readonly byte[] OptionalEntropy = Encoding.UTF8.GetBytes("MailForwarder.Settings.v1");
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _baseDirectory;
    private readonly string _settingsPath;

    public SettingsService()
    {
        _baseDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MailForwarder");
        _settingsPath = Path.Combine(_baseDirectory, "settings.json");
    }

    public AppSettings Load()
    {
        Directory.CreateDirectory(_baseDirectory);

        if (!File.Exists(_settingsPath))
        {
            var defaults = new AppSettings();
            Save(defaults);
            return defaults;
        }

        var json = File.ReadAllText(_settingsPath);
        var fileModel = JsonSerializer.Deserialize<SettingsFileModel>(json) ?? new SettingsFileModel();
        return ToAppSettings(fileModel);
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(_baseDirectory);
        var fileModel = ToFileModel(settings);
        var json = JsonSerializer.Serialize(fileModel, JsonOptions);
        File.WriteAllText(_settingsPath, json);
    }

    private static AppSettings ToAppSettings(SettingsFileModel fileModel)
    {
        return new AppSettings
        {
            General = new GeneralSettings
            {
                LaunchOnWindowsStartup = fileModel.General.LaunchOnWindowsStartup
            },
            Pop3 = new Pop3Settings
            {
                Host = fileModel.Pop3.Host,
                Port = fileModel.Pop3.Port,
                UseSsl = fileModel.Pop3.UseSsl,
                Username = fileModel.Pop3.Username,
                Password = Unprotect(fileModel.Pop3.Password)
            },
            Smtp = new SmtpSettings
            {
                Host = fileModel.Smtp.Host,
                Port = fileModel.Smtp.Port,
                UseSsl = fileModel.Smtp.UseSsl,
                Username = fileModel.Smtp.Username,
                Password = Unprotect(fileModel.Smtp.Password)
            },
            Forward = new ForwardSettings
            {
                ToAddress = fileModel.Forward.ToAddress,
                PollIntervalMinutes = fileModel.Forward.PollIntervalMinutes,
                DeleteAfterForward = fileModel.Forward.DeleteAfterForward
            }
        };
    }

    private static SettingsFileModel ToFileModel(AppSettings settings)
    {
        return new SettingsFileModel
        {
            General = new GeneralSettingsFileModel
            {
                LaunchOnWindowsStartup = settings.General.LaunchOnWindowsStartup
            },
            Pop3 = new Pop3SettingsFileModel
            {
                Host = settings.Pop3.Host,
                Port = settings.Pop3.Port,
                UseSsl = settings.Pop3.UseSsl,
                Username = settings.Pop3.Username,
                Password = Protect(settings.Pop3.Password)
            },
            Smtp = new SmtpSettingsFileModel
            {
                Host = settings.Smtp.Host,
                Port = settings.Smtp.Port,
                UseSsl = settings.Smtp.UseSsl,
                Username = settings.Smtp.Username,
                Password = Protect(settings.Smtp.Password)
            },
            Forward = new ForwardSettingsFileModel
            {
                ToAddress = settings.Forward.ToAddress,
                PollIntervalMinutes = settings.Forward.PollIntervalMinutes,
                DeleteAfterForward = settings.Forward.DeleteAfterForward
            }
        };
    }

    private static string Protect(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (value.StartsWith(EncryptedPrefix, StringComparison.Ordinal))
        {
            return value;
        }

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return value;
        }

        var plainBytes = Encoding.UTF8.GetBytes(value);
        var protectedBytes = ProtectedData.Protect(plainBytes, OptionalEntropy, DataProtectionScope.CurrentUser);
        return EncryptedPrefix + Convert.ToBase64String(protectedBytes);
    }

    private static string Unprotect(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (!value.StartsWith(EncryptedPrefix, StringComparison.Ordinal))
        {
            return value;
        }

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return string.Empty;
        }

        var payload = value[EncryptedPrefix.Length..];
        var protectedBytes = Convert.FromBase64String(payload);
        var plainBytes = ProtectedData.Unprotect(protectedBytes, OptionalEntropy, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(plainBytes);
    }

    private sealed class SettingsFileModel
    {
        public GeneralSettingsFileModel General { get; set; } = new();
        public Pop3SettingsFileModel Pop3 { get; set; } = new();
        public SmtpSettingsFileModel Smtp { get; set; } = new();
        public ForwardSettingsFileModel Forward { get; set; } = new();
    }

    private sealed class GeneralSettingsFileModel
    {
        public bool LaunchOnWindowsStartup { get; set; }
    }

    private sealed class Pop3SettingsFileModel
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 995;
        public bool UseSsl { get; set; } = true;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    private sealed class SmtpSettingsFileModel
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 465;
        public bool UseSsl { get; set; } = true;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    private sealed class ForwardSettingsFileModel
    {
        public string ToAddress { get; set; } = string.Empty;
        public int PollIntervalMinutes { get; set; } = 5;
        public bool DeleteAfterForward { get; set; }
    }
}
