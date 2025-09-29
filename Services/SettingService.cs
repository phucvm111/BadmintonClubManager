using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BadmintonClub.Services;

public sealed class SettingsService
{
    private readonly string _root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BadmintonClub");
    private readonly string _cfgPath;
    private readonly string _tokenPath;

    public SettingsService()
    {
        Directory.CreateDirectory(_root);
        _cfgPath = Path.Combine(_root, "settings.json");
        _tokenPath = Path.Combine(_root, "gmail_token.bin");
        if (!File.Exists(_cfgPath))
        {
            var def = new AppSettings
            {
                GmailClientSecretPath = "Utils/google-oauth-desktop.json",
                EnableEmailReminder = true,
                DefaultReminderMinutes = 120
            };
            File.WriteAllText(_cfgPath, JsonSerializer.Serialize(def, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    public AppSettings Load() => JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_cfgPath))!;
    public void Save(AppSettings s) => File.WriteAllText(_cfgPath, JsonSerializer.Serialize(s, new JsonSerializerOptions { WriteIndented = true }));

    public void SaveToken(byte[] token)
    {
        var enc = ProtectedData.Protect(token, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(_tokenPath, enc);
    }

    public byte[]? LoadToken()
    {
        if (!File.Exists(_tokenPath)) return null;
        var enc = File.ReadAllBytes(_tokenPath);
        return ProtectedData.Unprotect(enc, null, DataProtectionScope.CurrentUser);
    }
}

public sealed class AppSettings
{
    public string GmailClientSecretPath { get; set; } = "";
    public bool EnableEmailReminder { get; set; }
    public int DefaultReminderMinutes { get; set; }
}
