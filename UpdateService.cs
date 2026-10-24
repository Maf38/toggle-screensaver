using System.Windows.Threading;
using Velopack;
using Velopack.Sources;

namespace ToggleScreensaver;

/// <summary>
/// Mises à jour automatiques depuis les releases GitHub (Velopack). La mise à jour est
/// téléchargée en arrière-plan puis appliquée à la fermeture de l'application, ou tout de
/// suite sur demande. Sans effet hors installation (lancement depuis les sources).
/// </summary>
public sealed class UpdateService
{
    private const string RepoUrl = "https://github.com/Maf38/toggle-screensaver";

    private readonly UpdateManager _manager = new(new GithubSource(RepoUrl, null, false));
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromHours(4) };
    private UpdateInfo? _pending;
    private bool _checking;

    public bool IsInstalled => _manager.IsInstalled;

    public string CurrentVersion => _manager.CurrentVersion?.ToString() ?? "développement";

    /// <summary>Version téléchargée, prête à être installée.</summary>
    public string? PendingVersion => _pending?.TargetFullRelease.Version.ToString();

    public string Status { get; private set; } = "";

    public event Action? Changed;

    public void Start()
    {
        if (!IsInstalled)
        {
            Status = "Mises à jour indisponibles (version de développement).";
            return;
        }

        _timer.Tick += async (_, _) => await CheckAsync();
        _timer.Start();
        // Après le démarrage, sans gêner l'ouverture de la fenêtre
        Dispatcher.CurrentDispatcher.BeginInvoke(new Func<Task>(() => CheckAsync()), DispatcherPriority.ApplicationIdle);
    }

    public async Task CheckAsync(bool manual = false)
    {
        if (!IsInstalled || _checking || _pending is not null) return;

        _checking = true;
        SetStatus("Recherche de mises à jour…");
        try
        {
            var update = await _manager.CheckForUpdatesAsync();
            if (update is null)
            {
                SetStatus("L'application est à jour.");
                return;
            }

            SetStatus($"Téléchargement de la version {update.TargetFullRelease.Version}…");
            await _manager.DownloadUpdatesAsync(update);
            _pending = update;
            SetStatus($"Version {PendingVersion} prête : installée à la fermeture de l'application.");
        }
        catch
        {
            // Pas de connexion ou dépôt inaccessible : silencieux sauf demande explicite.
            SetStatus(manual ? "Impossible de vérifier les mises à jour (connexion ?)." : "");
        }
        finally
        {
            _checking = false;
        }
    }

    /// <summary>Installe la mise à jour téléchargée et relance l'application.</summary>
    public void ApplyAndRestart()
    {
        if (_pending is not null) _manager.ApplyUpdatesAndRestart(_pending.TargetFullRelease);
    }

    /// <summary>À la fermeture : installe en silence la mise à jour déjà téléchargée.</summary>
    public void ApplyOnExit()
    {
        if (_pending is not null) _manager.WaitExitThenApplyUpdates(_pending.TargetFullRelease, silent: true, restart: false);
    }

    private void SetStatus(string status)
    {
        Status = status;
        Changed?.Invoke();
    }
}
