using System;
using System.Drawing;
using Forms = System.Windows.Forms;
using SoundSwap.Core.Services;
using SoundSwap.Desktop.Localization;

namespace SoundSwap.Desktop.Services;

/// <summary>Notification-area icon with a menu to switch configurations without opening the window.</summary>
public sealed class TrayService : IDisposable
{
    private readonly Forms.NotifyIcon _icon;
    private readonly ConfigurationService _config;

    /// <summary>Creates the notification-area icon and its menu.</summary>
    /// /// <param name="config">Configurations listed in the menu.</param>
    public TrayService(ConfigurationService config)
    {
        _config = config;
        using var stream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Assets/SoundSwap.ico")).Stream;
        _icon = new Forms.NotifyIcon { Icon = new Icon(stream), Text = "SoundSwap", Visible = true };
        _icon.MouseClick += (_, e) => { if (e.Button == Forms.MouseButtons.Left) OpenRequested?.Invoke(); };

        _config.Changed += (_, _) => Rebuild();
        _config.ActiveChanged += (_, _) => Rebuild();
        Localizer.Instance.PropertyChanged += (_, _) => Rebuild();
        Rebuild();
    }

    /// <summary>The user asked to open the window (left click or menu).</summary>
    public event Action? OpenRequested;
    /// <summary>The user chose Quit in the menu.</summary>
    public event Action? QuitRequested;

    /// <summary>Shows a short notification balloon.</summary>
    /// /// <param name="message">Text to display.</param>
    public void ShowToast(string message) => _icon.ShowBalloonTip(2000, "SoundSwap", message, Forms.ToolTipIcon.None);

    private void Rebuild()
    {
        var loc = Localizer.Instance;
        var menu = new Forms.ContextMenuStrip();

        var open = new Forms.ToolStripMenuItem(loc["tray_open"]) { Font = new Font(Forms.Control.DefaultFont, FontStyle.Bold) };
        open.Click += (_, _) => OpenRequested?.Invoke();
        menu.Items.Add(open);
        menu.Items.Add(new Forms.ToolStripSeparator());

        foreach (var configuration in _config.Configurations)
        {
            var id = configuration.Id;
            var item = new Forms.ToolStripMenuItem(configuration.Name) { Checked = id == _config.ActiveId };
            item.Click += (_, _) => _config.Activate(id);
            menu.Items.Add(item);
        }
        if (_config.Configurations.Count > 0) menu.Items.Add(new Forms.ToolStripSeparator());

        var quit = new Forms.ToolStripMenuItem(loc["tray_quit"]);
        quit.Click += (_, _) => QuitRequested?.Invoke();
        menu.Items.Add(quit);

        var old = _icon.ContextMenuStrip;
        _icon.ContextMenuStrip = menu;
        old?.Dispose();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }
}
