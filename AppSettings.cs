using System.IO;
using System.Text.Json;

namespace ToggleScreensaver;

/// <summary>
/// Paramètres de l'application, persistés en JSON dans %APPDATA%\ToggleScreensaver\settings.json.
/// </summary>
public sealed class AppSettings
{
    // Modificateurs Win32 (RegisterHotKey)
    public const uint ModAlt = 0x1, ModControl = 0x2, ModShift = 0x4, ModWin = 0x8;

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ToggleScreensaver", "settings.json");

    /// <summary>État au lancement : true = veille désactivée (écran maintenu allumé).</summary>
    public bool BlockSleepOnStart { get; set; } = true;

    /// <summary>Plage horaire pendant laquelle la veille est réactivée automatiquement.</summary>
    public bool ScheduleEnabled { get; set; } = true;
    public TimeOnly ScheduleStart { get; set; } = new(21, 30);
    public TimeOnly ScheduleEnd { get; set; } = new(5, 30);

    public bool StartWithWindows { get; set; }
    public bool ShowInTray { get; set; }

    public bool HotkeyEnabled { get; set; } = true;
    public uint HotkeyModifiers { get; set; } = ModControl | ModAlt;
    public uint HotkeyKey { get; set; } = 0x53; // touche S

    /// <summary>La plage peut passer minuit (ex. 21:30 → 05:30). Début = fin : plage vide.</summary>
    public bool IsInScheduleWindow(TimeOnly now)
    {
        if (!ScheduleEnabled || ScheduleStart == ScheduleEnd) return false;
        return ScheduleStart < ScheduleEnd
            ? now >= ScheduleStart && now < ScheduleEnd
            : now >= ScheduleStart || now < ScheduleEnd;
    }

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
        }
        catch
        {
            // Fichier illisible ou corrompu : on repart des valeurs par défaut.
        }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            var tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(this, JsonOptions));
            File.Move(tmp, FilePath, overwrite: true);
        }
        catch
        {
            // Écriture impossible (droits, disque) : les réglages restent valables pour la session.
        }
    }
}
