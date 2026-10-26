using System;
using System.Windows;
using System.Windows.Interop;
using SoundSwap.Core.Theming;
using SoundSwap.Desktop.Services;
using SoundSwap.Desktop.ViewModels;
using Forms = System.Windows.Forms;

namespace SoundSwap.Desktop.Views;

/// <summary>Edits the custom theme with live preview on the whole application.</summary>
public partial class ThemeEditorWindow : Window
{
    private readonly ThemeEditorViewModel _viewModel;

    public ThemeEditorWindow(ThemeEditorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = _viewModel = viewModel;
        SourceInitialized += (_, _) => ThemeService.ApplyTitleBar(this);
        Loaded += (_, _) => PlaceBesideOwner();
    }

    // Sits at the right edge of the main window so the live preview stays visible.
    private void PlaceBesideOwner()
    {
        if (Owner is null) return;
        var work = SystemParameters.WorkArea;
        Left = Math.Clamp(Owner.Left + Owner.Width - Width - 24, work.Left, Math.Max(work.Left, work.Right - Width));
        Top = Math.Clamp(Owner.Top + 90, work.Top, Math.Max(work.Top, work.Bottom - ActualHeight));
    }

    private void OnPickColor(object sender, RoutedEventArgs e)
    {
        var slot = Enum.Parse<ColorSlot>((string)((FrameworkElement)sender).Tag);
        var start = Rgb.TryParse(_viewModel.GetHex(slot), out var current)
            ? System.Drawing.Color.FromArgb(current.R, current.G, current.B)
            : System.Drawing.Color.White;

        using var dialog = new Forms.ColorDialog { AnyColor = true, FullOpen = true, Color = start };
        if (dialog.ShowDialog(new OwnerWindow(this)) != Forms.DialogResult.OK) return;
        _viewModel.SetColor(slot, $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}");
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();

    private sealed class OwnerWindow : Forms.IWin32Window
    {
        public OwnerWindow(Window window) => Handle = new WindowInteropHelper(window).Handle;
        public IntPtr Handle { get; }
    }
}
