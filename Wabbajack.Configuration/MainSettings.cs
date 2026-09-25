namespace Wabbajack.Configuration;

public class MainSettings
{
    public const string SettingsFileName = "app_settings";
    private const int SettingsVersion = 1;

    public int CurrentSettingsVersion { get; set; }

    // Session-only: require explicit activation after every application start.
    [System.Text.Json.Serialization.JsonIgnore]
    public bool AutoNexusDownload { get; set; }

    public bool Upgrade()
    {
        if (CurrentSettingsVersion == SettingsVersion)
        {
            return false;
        }

        CurrentSettingsVersion = SettingsVersion;
        return true;
    }
}