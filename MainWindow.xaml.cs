using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ToggleScreensaver;

public partial class MainWindow : Window
{
    private readonly App _app = (App)Application.Current;
    private bool _loading;

    public MainWindow()
    {
        InitializeComponent();

        foreach (var box in new[] { StartHourBox, EndHourBox })
            for (var h = 0; h < 24; h++) box.Items.Add(h.ToString("00"));
        foreach (var box in new[] { StartMinuteBox, EndMinuteBox })
            for (var m = 0; m < 60; m += 5) box.Items.Add(m.ToString("00"));

        LoadSettings();
        _app.Sleep.Changed += UpdateUI;
        UpdateUI();
    }

    private void ToggleBtn_Click(object sender, RoutedEventArgs e) => _app.Sleep.Toggle();

    private void UpdateUI()
    {
        var blocked = _app.Sleep.IsBlocked;
        var settings = _app.Settings;

        if (blocked)
        {
            ToggleBtn.Content    = "Réactiver la veille";
            ToggleBtn.Background = new SolidColorBrush(Color.FromRgb(0xE5, 0x39, 0x35)); // rouge
            StatusLabel.Text     = "😵 Veille désactivée";
            SleepImage.Opacity   = 0.3; // marmotte "réveillée" / grisée
        }
        else
        {
            ToggleBtn.Content    = "Désactiver la veille";
            ToggleBtn.Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x88, 0xE5)); // bleu
            StatusLabel.Text     = "💤 Veille activée (normal)";
            SleepImage.Opacity   = 1.0; // marmotte qui dort tranquille
        }

        // Lune quand la veille est activée, soleil quand elle est désactivée
        Icon = IconFactory.GetWindowIcon(blocked);

        if (settings.ScheduleEnabled && settings.ScheduleStart != settings.ScheduleEnd)
        {
            ScheduleLabel.Text = settings.IsInScheduleWindow(TimeOnly.FromDateTime(DateTime.Now))
                ? $"🌙 Plage de veille en cours, jusqu'à {settings.ScheduleEnd:HH\\:mm}"
                : $"🌙 Veille automatique à partir de {settings.ScheduleStart:HH\\:mm}";
            ScheduleLabel.Visibility = Visibility.Visible;
        }
        else
        {
            ScheduleLabel.Visibility = Visibility.Collapsed;
        }

        HotkeyLabel.Text = settings.HotkeyEnabled && !_app.HotkeyFailed
            ? HotkeyManager.Format(settings.HotkeyModifiers, settings.HotkeyKey)
            : "";
        HotkeyWarning.Visibility = _app.HotkeyFailed ? Visibility.Visible : Visibility.Collapsed;
    }

    // ───────── Paramètres ─────────

    private void LoadSettings()
    {
        _loading = true;
        var s = _app.Settings;
        StartWithWindowsCheck.IsChecked = s.StartWithWindows;
        BlockOnStartCheck.IsChecked = s.BlockSleepOnStart;
        ShowInTrayCheck.IsChecked = s.ShowInTray;
        ScheduleCheck.IsChecked = s.ScheduleEnabled;
        SelectTime(StartHourBox, StartMinuteBox, s.ScheduleStart);
        SelectTime(EndHourBox, EndMinuteBox, s.ScheduleEnd);
        HotkeyCheck.IsChecked = s.HotkeyEnabled;
        HotkeyBox.Text = HotkeyManager.Format(s.HotkeyModifiers, s.HotkeyKey);
        _loading = false;
    }

    private static void SelectTime(System.Windows.Controls.ComboBox hour, System.Windows.Controls.ComboBox minute, TimeOnly t)
    {
        hour.SelectedIndex = t.Hour;
        minute.SelectedIndex = t.Minute / 5; // les minutes se règlent par pas de 5
    }

    private static TimeOnly ReadTime(System.Windows.Controls.ComboBox hour, System.Windows.Controls.ComboBox minute, TimeOnly fallback) =>
        hour.SelectedIndex < 0 || minute.SelectedIndex < 0
            ? fallback
            : new TimeOnly(hour.SelectedIndex, minute.SelectedIndex * 5);

    /// <summary>Enregistre et applique immédiatement : pas de bouton « Enregistrer ».</summary>
    private void Setting_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;

        var s = _app.Settings;
        s.StartWithWindows = StartWithWindowsCheck.IsChecked == true;
        s.BlockSleepOnStart = BlockOnStartCheck.IsChecked == true;
        s.ShowInTray = ShowInTrayCheck.IsChecked == true;
        s.ScheduleEnabled = ScheduleCheck.IsChecked == true;
        s.ScheduleStart = ReadTime(StartHourBox, StartMinuteBox, s.ScheduleStart);
        s.ScheduleEnd = ReadTime(EndHourBox, EndMinuteBox, s.ScheduleEnd);
        s.HotkeyEnabled = HotkeyCheck.IsChecked == true;

        SaveAndApply();
    }

    private void SaveAndApply()
    {
        _app.Settings.Save();
        _app.ApplySettings();
        UpdateUI();
    }

    private void HotkeyBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        // Laisse Tab naviguer ; ignore les touches de modification seules
        if (key == Key.Tab && Keyboard.Modifiers == ModifierKeys.None) return;
        e.Handled = true;
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin
            or Key.ImeProcessed or Key.None)
            return;

        var mods = 0u;
        var pressed = Keyboard.Modifiers;
        if (pressed.HasFlag(ModifierKeys.Control)) mods |= AppSettings.ModControl;
        if (pressed.HasFlag(ModifierKeys.Alt)) mods |= AppSettings.ModAlt;
        if (pressed.HasFlag(ModifierKeys.Shift)) mods |= AppSettings.ModShift;
        if (pressed.HasFlag(ModifierKeys.Windows)) mods |= AppSettings.ModWin;
        if (mods == 0) return; // un raccourci global sans modificateur gênerait la frappe normale

        _app.Settings.HotkeyModifiers = mods;
        _app.Settings.HotkeyKey = (uint)KeyInterop.VirtualKeyFromKey(key);
        HotkeyBox.Text = HotkeyManager.Format(_app.Settings.HotkeyModifiers, _app.Settings.HotkeyKey);
        SaveAndApply();
    }

    // ───────── Fenêtre / zone de notification ─────────

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        // En mode zone de notification, réduire = masquer (pas de bouton dans la barre des tâches)
        if (WindowState == WindowState.Minimized && _app.Settings.ShowInTray)
        {
            Hide();
            WindowState = WindowState.Normal;
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_app.IsQuitting && _app.Settings.ShowInTray)
        {
            e.Cancel = true;
            Hide(); // l'application continue en arrière-plan
            return;
        }
        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _app.Quit();
    }
}
