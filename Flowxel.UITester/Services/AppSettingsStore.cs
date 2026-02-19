using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using Avalonia.Styling;

namespace Flowxel.UITester.Services;

public sealed class AppSettings
{
    public string Theme { get; set; } = "Dark";
    public string Culture { get; set; } = CultureInfo.CurrentUICulture.Name;
}

public interface IAppSettingsStore
{
    AppSettings Current { get; }
    void Load();
    void SetTheme(ThemeVariant theme);
    void SetCulture(CultureInfo culture);
}

public sealed class JsonAppSettingsStore : IAppSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _settingsPath;

    public JsonAppSettingsStore()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var appDir = Path.Combine(home, ".flowxel-uitester");
        _settingsPath = Path.Combine(appDir, "settings.json");
    }

    public AppSettings Current { get; private set; } = new();

    public void Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (loaded is not null)
                    Current = loaded;
            }
        }
        catch
        {
            Current = new AppSettings();
        }

        Save();
    }

    public void SetTheme(ThemeVariant theme)
    {
        Current.Theme = theme == ThemeVariant.Light ? "Light" : "Dark";
        Save();
    }

    public void SetCulture(CultureInfo culture)
    {
        Current.Culture = culture.Name;
        Save();
    }

    private void Save()
    {
        var directory = Path.GetDirectoryName(_settingsPath);
        if (string.IsNullOrWhiteSpace(directory))
            return;

        Directory.CreateDirectory(directory);

        var tmpPath = _settingsPath + ".tmp";
        File.WriteAllText(tmpPath, JsonSerializer.Serialize(Current, JsonOptions));
        File.Move(tmpPath, _settingsPath, overwrite: true);
    }
}
