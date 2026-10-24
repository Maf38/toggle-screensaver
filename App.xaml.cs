using System.Threading;
using System.Windows;
using Velopack;

namespace ToggleScreensaver;

public partial class App : Application
{
    [STAThread]
    private static void Main(string[] args)
    {
        // Doit s'exécuter avant tout le reste : gère les hooks d'installation, de mise à jour
        // et de désinstallation (l'entrée de démarrage Windows est retirée à la désinstallation).
        VelopackApp.Build()
            .OnBeforeUninstallFastCallback(_ => StartupRegistration.Apply(false))
            .Run();

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }

    private const string MutexName = @"Local\ToggleScreensaver.SingleInstance";
    private const string ShowEventName = @"Local\ToggleScreensaver.Show";

    private Mutex? _mutex;
    private EventWaitHandle? _showEvent;
    private TrayService? _tray;
    private HotkeyManager? _hotkey;
    private SleepScheduler? _scheduler;
    private MainWindow? _window;

    public AppSettings Settings { get; private set; } = new();
    public SleepController Sleep { get; } = new();
    public UpdateService Updates { get; } = new();

    /// <summary>Le raccourci demandé est déjà utilisé par une autre application.</summary>
    public bool HotkeyFailed { get; private set; }

    public bool IsQuitting { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Instance unique : le raccourci global et l'icône de notification ne se partagent pas.
        _mutex = new Mutex(true, MutexName, out var createdNew);
        if (!createdNew)
        {
            try { EventWaitHandle.OpenExisting(ShowEventName).Set(); } catch { }
            Shutdown();
            return;
        }
        _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
        new Thread(() =>
        {
            while (_showEvent.WaitOne())
                Dispatcher.BeginInvoke(ShowMainWindow);
        }) { IsBackground = true }.Start();

        Settings = AppSettings.Load();
        _scheduler = new SleepScheduler(Settings, Sleep);
        _tray = new TrayService(this);
        _hotkey = new HotkeyManager();
        _hotkey.Pressed += Sleep.Toggle;
        _window = new MainWindow();
        MainWindow = _window;

        ApplySettings(); // évalue aussi la plage horaire → état initial de la veille
        _scheduler.Start();
        Updates.Start();

        var launchedAtLogon = e.Args.Contains(StartupRegistration.StartupArg);
        if (launchedAtLogon && Settings.ShowInTray) return; // démarre caché dans la zone de notification

        if (launchedAtLogon) _window.WindowState = WindowState.Minimized;
        _window.Show();
    }

    /// <summary>Répercute les réglages courants sur le système (à appeler après chaque modification).</summary>
    public void ApplySettings()
    {
        if (_tray is null || _hotkey is null || _scheduler is null || _window is null) return;

        _tray.Visible = Settings.ShowInTray;
        if (_window.ShowInTaskbar == Settings.ShowInTray)
            _window.ShowInTaskbar = !Settings.ShowInTray;

        StartupRegistration.Apply(Settings.StartWithWindows);

        if (Settings.HotkeyEnabled)
            HotkeyFailed = !_hotkey.Register(Settings.HotkeyModifiers, Settings.HotkeyKey);
        else
        {
            _hotkey.Unregister();
            HotkeyFailed = false;
        }

        _scheduler.Evaluate();
    }

    public void ShowMainWindow()
    {
        if (_window is null) return;
        if (!_window.IsVisible) _window.Show();
        if (_window.WindowState == WindowState.Minimized) _window.WindowState = WindowState.Normal;
        _window.Activate();
    }

    public void Quit()
    {
        if (IsQuitting) return;
        IsQuitting = true;
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Sleep.Release(); // toujours rétablir l'état normal à la fermeture
        _hotkey?.Dispose();
        _tray?.Dispose();
        _mutex?.Dispose();
        Updates.ApplyOnExit();
        base.OnExit(e);
    }
}
