using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SkyV.Launcher;

public sealed class LauncherSettings
{
    [JsonPropertyName("website_base_url")]
    public string WebsiteBaseUrl { get; set; } = "https://www.vokunrp.com";

    [JsonPropertyName("queue_base_url")]
    public string QueueBaseUrl { get; set; } = "https://queue.vokunrp.com";

    [JsonPropertyName("pack_url")]
    public string PackUrl { get; set; } = "https://github.com/HALFIN-Games/SkyV/releases/latest/download/VokunPack.zip";

    [JsonPropertyName("skyrim_install_path")]
    public string? SkyrimInstallPath { get; set; }

    public static LauncherSettings Load()
    {
        try
        {
            var path = GetSettingsPath();
            if (!File.Exists(path))
            {
                var created = new LauncherSettings();
                created.Save();
                return created;
            }

            var json = File.ReadAllText(path);
            var parsed = JsonSerializer.Deserialize<LauncherSettings>(json, JsonOptions());
            return parsed ?? new LauncherSettings();
        }
        catch
        {
            return new LauncherSettings();
        }
    }

    public void Save()
    {
        var path = GetSettingsPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        var json = JsonSerializer.Serialize(this, JsonOptions());
        File.WriteAllText(path, json);
    }

    public static string GetSettingsPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "SkyV", "settings.json");
    }

    private static JsonSerializerOptions JsonOptions()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = true,
        };
    }
}
