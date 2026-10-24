using System.IO;
using Microsoft.Win32;

namespace ToggleScreensaver;

/// <summary>Lancement automatique à l'ouverture de session (clé Run de l'utilisateur).</summary>
public static class StartupRegistration
{
    public const string StartupArg = "--startup";

    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "ToggleScreensaver";

    public static void Apply(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            if (key is null) return;

            var exe = Environment.ProcessPath;
            // Pas d'enregistrement si lancé via `dotnet ToggleScreensaver.dll`.
            if (enabled && exe is not null
                && !Path.GetFileName(exe).Equals("dotnet.exe", StringComparison.OrdinalIgnoreCase))
                key.SetValue(ValueName, $"\"{exe}\" {StartupArg}");
            else
                key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
        catch
        {
            // Registre inaccessible : on ignore, la case reste simplement sans effet.
        }
    }
}
