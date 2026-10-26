using System.Windows;
using SoundSwap.Desktop.Services;

namespace SoundSwap.Desktop.Views;

/// <summary>What the user chose in the close dialog.</summary>
public enum CloseChoice { Cancel, Minimize, Quit }

/// <summary>Asks: minimize to the notification area, quit completely, or cancel.</summary>
public partial class CloseDialog : Window
{
    public CloseDialog()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => ThemeService.ApplyTitleBar(this);
    }

    /// <summary>The chosen action (<see cref="CloseChoice.Cancel"/> if the window was just closed).</summary>
    public CloseChoice Choice { get; private set; } = CloseChoice.Cancel;

    private void OnMinimize(object sender, RoutedEventArgs e) { Choice = CloseChoice.Minimize; Close(); }
    private void OnQuit(object sender, RoutedEventArgs e) { Choice = CloseChoice.Quit; Close(); }
}
