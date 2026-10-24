using System.Windows.Threading;

namespace ToggleScreensaver;

/// <summary>
/// Réactive la veille pendant la plage horaire configurée, puis rétablit l'état par
/// défaut à la fin de la plage. Seuls les franchissements de bornes agissent : un
/// basculement manuel pendant la plage n'est donc pas écrasé avant la prochaine borne.
/// </summary>
public sealed class SleepScheduler
{
    private readonly AppSettings _settings;
    private readonly SleepController _sleep;
    private readonly DispatcherTimer _timer;
    private bool _initialized;
    private bool _wasInWindow;

    public SleepScheduler(AppSettings settings, SleepController sleep)
    {
        _settings = settings;
        _sleep = sleep;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
        _timer.Tick += (_, _) => Evaluate();
    }

    public void Start() => _timer.Start();

    /// <summary>
    /// Au premier appel (lancement), applique l'état initial : veille réactivée si on est
    /// dans la plage, sinon l'état par défaut choisi dans les paramètres. Ensuite, n'agit
    /// qu'aux franchissements de bornes ; plage désactivée, l'état courant n'est jamais touché.
    /// </summary>
    public void Evaluate()
    {
        var inWindow = _settings.IsInScheduleWindow(TimeOnly.FromDateTime(DateTime.Now));

        if (_initialized)
        {
            if (_wasInWindow == inWindow) return;

            if (!_settings.ScheduleEnabled)
            {
                _wasInWindow = false; // décochée : on ne touche pas à l'état courant
                return;
            }
        }

        _initialized = true;
        _wasInWindow = inWindow;
        _sleep.SetBlocked(inWindow ? false : _settings.BlockSleepOnStart);
    }
}
