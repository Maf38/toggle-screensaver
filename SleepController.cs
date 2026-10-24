using System.Runtime.InteropServices;

namespace ToggleScreensaver;

/// <summary>
/// Empêche (ou non) la mise en veille de l'écran et du système.
/// SetThreadExecutionState est lié au thread appelant : tout passe par le thread UI.
/// </summary>
public sealed class SleepController
{
    [DllImport("kernel32.dll")]
    private static extern uint SetThreadExecutionState(uint esFlags);

    private const uint ES_CONTINUOUS       = 0x80000000;
    private const uint ES_SYSTEM_REQUIRED  = 0x00000001;
    private const uint ES_DISPLAY_REQUIRED = 0x00000002;

    /// <summary>true = veille désactivée (le PC reste éveillé).</summary>
    public bool IsBlocked { get; private set; }

    /// <summary>Déclenché à chaque demande de changement d'état, même sans changement effectif.</summary>
    public event Action? Changed;

    public void SetBlocked(bool blocked)
    {
        IsBlocked = blocked;
        SetThreadExecutionState(blocked
            ? ES_CONTINUOUS | ES_DISPLAY_REQUIRED | ES_SYSTEM_REQUIRED
            : ES_CONTINUOUS);
        Changed?.Invoke();
    }

    public void Toggle() => SetBlocked(!IsBlocked);

    /// <summary>Rétablit le comportement normal de Windows.</summary>
    public void Release() => SetThreadExecutionState(ES_CONTINUOUS);
}
