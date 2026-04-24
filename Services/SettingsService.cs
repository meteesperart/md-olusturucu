using System.IO;
using System.Text.Json;
using MDOlusturucu.Models;

namespace MDOlusturucu.Services;

public class SettingsService : ISettingsService
{
    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MDOlusturucu",
        "settings.json");

    private static readonly string DefaultDirectory = Path.Combine(
        AppContext.BaseDirectory,
        "MD Dosyaları");

    private static readonly JsonSerializerOptions JsonOpts =
        new() { WriteIndented = true };

    public AppSettings Current { get; private set; }

    public event EventHandler<AppSettings>? SettingsChanged;

    public SettingsService()
    {
        Current = Load();
        EnsureDirectoryExists(Current.MarkdownDirectory);
    }

    public void Save(AppSettings settings)
    {
        Current = settings;
        EnsureDirectoryExists(Current.MarkdownDirectory);

        var dir = Path.GetDirectoryName(ConfigPath)!;
        Directory.CreateDirectory(dir);
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(Current, JsonOpts));

        SettingsChanged?.Invoke(this, Current);
    }

    public void SaveSilent(AppSettings settings)
    {
        Current = settings;
        var dir = Path.GetDirectoryName(ConfigPath)!;
        Directory.CreateDirectory(dir);
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(Current, JsonOpts));
        // SettingsChanged kasıtlı olarak tetiklenmez
    }

    private static AppSettings Load()
    {
        if (!File.Exists(ConfigPath))
            return BuildDefault();

        try
        {
            var json = File.ReadAllText(ConfigPath);
            var s = JsonSerializer.Deserialize<AppSettings>(json);
            if (s is null)
                return BuildDefault();

            // MarkdownDirectory boşsa veya mevcut değilse varsayılanı kullan;
            // diğer ayarlar (Language vb.) korunur
            if (string.IsNullOrWhiteSpace(s.MarkdownDirectory) || !Directory.Exists(s.MarkdownDirectory))
                s.MarkdownDirectory = DefaultDirectory;

            if (string.IsNullOrWhiteSpace(s.Language))
                s.Language = "tr";

            return s;
        }
        catch
        {
            return BuildDefault();
        }
    }

    private static AppSettings BuildDefault() => new()
    {
        MarkdownDirectory = DefaultDirectory,
        EditorFontFamily  = "Cascadia Code",
        EditorFontColor   = "",
        Language          = "tr"
    };

    private static void EnsureDirectoryExists(string path)
    {
        if (!string.IsNullOrWhiteSpace(path))
            Directory.CreateDirectory(path);
    }
}
