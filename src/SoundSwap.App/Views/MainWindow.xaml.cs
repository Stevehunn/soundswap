using System.ComponentModel;
using System.Windows;
using SoundSwap.Desktop.Services;
using SoundSwap.Desktop.ViewModels;

namespace SoundSwap.Desktop.Views;

/// <summary>The main window. Its X button asks what to do instead of closing; cards can be dragged to reorder (see <c>MainWindow.DragDrop.cs</c>).</summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.EditorDialog = editor => new ConfigEditorWindow(editor) { Owner = this }.ShowDialog() == true;
        viewModel.ThemeEditorDialog = editor => new ThemeEditorWindow(editor) { Owner = this }.ShowDialog();
        SourceInitialized += (_, _) => ThemeService.ApplyTitleBar(this);
    }

    // The X button never closes the process directly: the app asks what to do.
    protected override void OnClosing(CancelEventArgs e)
    {
        if (!((App)Application.Current).IsExiting)
        {
            e.Cancel = true;
            ((App)Application.Current).RequestClose();
        }
        base.OnClosing(e);
    }
}
