using Forms = System.Windows.Forms;

namespace ToggleScreensaver;

/// <summary>Icône de zone de notification (clic droit : menu, double-clic : ouvrir).</summary>
public sealed class TrayService : IDisposable
{
    private readonly SleepController _sleep;
    private readonly Forms.NotifyIcon _icon;
    private readonly Forms.ToolStripMenuItem _toggleItem;

    public TrayService(App app)
    {
        _sleep = app.Sleep;

        _toggleItem = new Forms.ToolStripMenuItem("", null, (_, _) => _sleep.Toggle());
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add(_toggleItem);
        menu.Items.Add(new Forms.ToolStripMenuItem("Ouvrir", null, (_, _) => app.ShowMainWindow()));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(new Forms.ToolStripMenuItem("Quitter", null, (_, _) => app.Quit()));

        _icon = new Forms.NotifyIcon { ContextMenuStrip = menu, Visible = false };
        _icon.DoubleClick += (_, _) => app.ShowMainWindow();

        _sleep.Changed += Update;
        Update();
    }

    public bool Visible
    {
        get => _icon.Visible;
        set => _icon.Visible = value;
    }

    private void Update()
    {
        var blocked = _sleep.IsBlocked;
        _icon.Icon = IconFactory.GetTrayIcon(blocked);
        _icon.Text = blocked ? "Toggle Screensaver — veille désactivée" : "Toggle Screensaver — veille activée";
        _toggleItem.Text = blocked ? "Réactiver la veille" : "Désactiver la veille";
    }

    public void Dispose()
    {
        _sleep.Changed -= Update;
        _icon.Visible = false; // évite l'icône fantôme jusqu'au prochain survol
        _icon.ContextMenuStrip?.Dispose();
        _icon.Dispose();
    }
}
