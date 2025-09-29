using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Util.Store;
using BadmintonClub.Services;

namespace BadmintonClub.Services;

public sealed class GmailAuthService
{
    private readonly SettingsService _settings;

    public GmailAuthService(SettingsService settings) => _settings = settings;

    public async Task<UserCredential> AuthorizeAsync(CancellationToken ct)
    {
        var cfg = _settings.Load();
        using var stream = new FileStream(cfg.GmailClientSecretPath, FileMode.Open, FileAccess.Read);
        // Lưu token vào AppData bằng DataStore tuỳ biến
        var store = new FileDataStore(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BadmintonClub", "GoogleTokens"), true);
        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            GoogleClientSecrets.FromStream(stream).Secrets,
            new[] { GmailService.Scope.GmailSend },
            "club-owner",
            ct,
            store);
        return credential;
    }

    public GmailService CreateService(UserCredential cred)
        => new GmailService(new Google.Apis.Services.BaseClientService.Initializer
        {
            HttpClientInitializer = cred,
            ApplicationName = "BadmintonClub"
        });
}
