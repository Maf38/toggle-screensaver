using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;

namespace ToggleScreensaver;

public partial class MainWindow : Window
{
    // Empêche la mise en veille de l'écran et du système
    [DllImport("kernel32.dll")]
    private static extern uint SetThreadExecutionState(uint esFlags);

    private const uint ES_CONTINUOUS       = 0x80000000;
    private const uint ES_SYSTEM_REQUIRED  = 0x00000001;
    private const uint ES_DISPLAY_REQUIRED = 0x00000002;

    private bool _screensaverBlocked = false;

    public MainWindow()
    {
        InitializeComponent();
        UpdateUI();
    }

    private void ToggleBtn_Click(object sender, RoutedEventArgs e)
    {
        _screensaverBlocked = !_screensaverBlocked;

        if (_screensaverBlocked)
        {
            // Désactive la veille écran et système, de façon persistante
            SetThreadExecutionState(ES_CONTINUOUS | ES_DISPLAY_REQUIRED | ES_SYSTEM_REQUIRED);
        }
        else
        {
            // Rétablit le comportement normal de Windows
            SetThreadExecutionState(ES_CONTINUOUS);
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (_screensaverBlocked)
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
    }

    protected override void OnClosed(EventArgs e)
    {
        // Toujours rétablir l'état normal à la fermeture
        SetThreadExecutionState(ES_CONTINUOUS);
        base.OnClosed(e);
    }
}
