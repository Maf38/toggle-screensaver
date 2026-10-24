using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace ToggleScreensaver;

/// <summary>Raccourci clavier global, reçu via une fenêtre invisible (message-only).</summary>
public sealed class HotkeyManager : IDisposable
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const int WM_HOTKEY = 0x0312;
    private const int HotkeyId = 0x5353;
    private const uint ModNoRepeat = 0x4000;
    private static readonly IntPtr HwndMessage = new(-3);

    private readonly HwndSource _source;
    private bool _registered;

    public event Action? Pressed;

    public HotkeyManager()
    {
        _source = new HwndSource(new HwndSourceParameters("ToggleScreensaverHotkey") { ParentWindow = HwndMessage });
        _source.AddHook(WndProc);
    }

    /// <summary>Enregistre le raccourci ; false s'il est déjà pris par une autre application.</summary>
    public bool Register(uint modifiers, uint virtualKey)
    {
        Unregister();
        _registered = RegisterHotKey(_source.Handle, HotkeyId, modifiers | ModNoRepeat, virtualKey);
        return _registered;
    }

    public void Unregister()
    {
        if (!_registered) return;
        UnregisterHotKey(_source.Handle, HotkeyId);
        _registered = false;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HotkeyId)
        {
            Pressed?.Invoke();
            handled = true;
        }
        return IntPtr.Zero;
    }

    public static string Format(uint modifiers, uint virtualKey)
    {
        var parts = new List<string>();
        if ((modifiers & AppSettings.ModControl) != 0) parts.Add("Ctrl");
        if ((modifiers & AppSettings.ModAlt) != 0) parts.Add("Alt");
        if ((modifiers & AppSettings.ModShift) != 0) parts.Add("Maj");
        if ((modifiers & AppSettings.ModWin) != 0) parts.Add("Win");

        var key = KeyInterop.KeyFromVirtualKey((int)virtualKey).ToString();
        if (key.Length == 2 && key[0] == 'D' && char.IsDigit(key[1])) key = key[1..]; // D1 → 1
        parts.Add(key);
        return string.Join(" + ", parts);
    }

    public void Dispose()
    {
        Unregister();
        _source.Dispose();
    }
}
