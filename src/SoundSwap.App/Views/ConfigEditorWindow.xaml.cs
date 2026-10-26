using System.Windows;
using SoundSwap.Desktop.Services;
using SoundSwap.Desktop.ViewModels;

namespace SoundSwap.Desktop.Views;

/// <summary>Dialog to create or edit a configuration.</summary>
public partial class ConfigEditorWindow : Window
{
    private readonly ConfigEditorViewModel _viewModel;

    public ConfigEditorWindow(ConfigEditorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = _viewModel = viewModel;
        SourceInitialized += (_, _) => ThemeService.ApplyTitleBar(this);
    }

    private void OnValidate(object sender, RoutedEventArgs e)
    {
        if (_viewModel.TryValidate()) DialogResult = true;
    }
}
