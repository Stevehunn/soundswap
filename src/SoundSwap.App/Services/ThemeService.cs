using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;
using SoundSwap.Core.Models;
using SoundSwap.Core.Theming;

namespace SoundSwap.Desktop.Services;

/// <summary>
/// Swaps the color dictionary at runtime: Dark / Light (XAML files), System (follows Windows),
/// or Custom (built in code from the four colors the user picked).
/// </summary>
public static class ThemeService
{
    private static ThemeMode _mode = ThemeMode.System;
    private static bool _isDark = true;
    private static ResourceDictionary? _current;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    /// <summary>Starts following Windows theme changes (used by <see cref="ThemeMode.System"/>).</summary>
    public static void Initialize() =>
        SystemEvents.UserPreferenceChanged += (_, _) =>
        {
            if (_mode == ThemeMode.System) Application.Current?.Dispatcher.Invoke(() => Apply(ThemeMode.System));
        };

    /// <summary>Applies a theme to the whole application immediately.</summary>
    /// /// <param name="mode">Which theme.</param>
    /// /// <param name="custom">Colors used when <paramref name="mode"/> is <see cref="ThemeMode.Custom"/>.</param>
    public static void Apply(ThemeMode mode, CustomThemeColors? custom = null)
    {
        _mode = mode;

        ResourceDictionary next;
        if (mode == ThemeMode.Custom)
        {
            var palette = ThemePalette.From(custom ?? new CustomThemeColors());
            _isDark = palette.IsDark;
            next = Build(palette);
        }
        else
        {
            _isDark = mode switch
            {
                ThemeMode.Dark => true,
                ThemeMode.Light => false,
                _ => !SystemUsesLightTheme()
            };
            next = new ResourceDictionary { Source = new Uri($"Themes/{(_isDark ? "Dark" : "Light")}.xaml", UriKind.Relative) };
        }

        var dictionaries = Application.Current.Resources.MergedDictionaries;
        var existing = _current ?? dictionaries.FirstOrDefault(IsThemeFile);
        if (existing is null) dictionaries.Insert(0, next);
        else dictionaries[dictionaries.IndexOf(existing)] = next;
        _current = next;

        foreach (Window window in Application.Current.Windows) ApplyTitleBar(window);
    }

    /// <summary>Makes the native title bar follow the theme (Windows 10 2004+ / 11).</summary>
    public static void ApplyTitleBar(Window window)
    {
        var handle = new WindowInteropHelper(window).EnsureHandle();
        var value = _isDark ? 1 : 0;
        DwmSetWindowAttribute(handle, 20, ref value, sizeof(int));
    }

    private static bool IsThemeFile(ResourceDictionary d) =>
        d.Source?.OriginalString is { } s && (s.StartsWith("Themes/Dark") || s.StartsWith("Themes/Light"));

    // Same brush keys as Themes/Dark.xaml and Themes/Light.xaml.
    private static ResourceDictionary Build(ThemePalette p)
    {
        var dictionary = new ResourceDictionary();
        void Add(string key, string hex)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)!);
            brush.Freeze();
            dictionary[key] = brush;
        }
        Add("WindowBrush", p.Window);
        Add("PanelBrush", p.Panel);
        Add("ActivePanelBrush", p.ActivePanel);
        Add("InputBrush", p.Input);
        Add("HoverBrush", p.Hover);
        Add("LineBrush", p.Line);
        Add("TextBrush", p.Text);
        Add("MutedBrush", p.Muted);
        Add("AccentBrush", p.Accent);
        Add("OnAccentBrush", p.OnAccent);
        Add("OverlayBrush", p.Overlay);
        Add("DangerBrush", p.Danger);
        Add("DangerSoftBrush", p.DangerSoft);
        return dictionary;
    }

    private static bool SystemUsesLightTheme()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return key?.GetValue("AppsUseLightTheme") is not int v || v == 1;
    }
}
