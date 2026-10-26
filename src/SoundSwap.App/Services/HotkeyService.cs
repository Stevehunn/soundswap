using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;
using SoundSwap.Core.Hotkeys;

namespace SoundSwap.Desktop.Services;

/// <summary>
/// System-wide hotkeys (RegisterHotKey) received by a hidden message-only window,
/// so they work even when the main window is minimized or hidden.
/// </summary>
public sealed class HotkeyService : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const uint ModNoRepeat = 0x4000;
    private static readonly IntPtr HwndMessage = new(-3);

    [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint vk);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private readonly HwndSource _source = new(new HwndSourceParameters("SoundSwap.Hotkeys") { ParentWindow = HwndMessage });
    private readonly Dictionary<int, string> _registered = new();
    private int _nextId = 1;

    /// <summary>Raised with the id given to <see cref="Register"/> when the shortcut is pressed anywhere in Windows.</summary>
    public event Action<string>? Pressed;

    /// <summary>Creates the hidden message-only window that receives the shortcuts.</summary>
    public HotkeyService() => _source.AddHook(WndProc);

    /// <summary>Registers a global shortcut.</summary>
    /// /// <param name="id">Identifier returned by <see cref="Pressed"/>.</param>
    /// /// <param name="gesture">The key combination.</param>
    /// /// <returns><c>false</c> if another program already owns this combination.</returns>
    public bool Register(string id, HotkeyGesture gesture)
    {
        if (!Enum.TryParse<Key>(gesture.Key, out var key)) return false;
        var virtualKey = (uint)KeyInterop.VirtualKeyFromKey(key);
        var native = _nextId++;
        // HotkeyModifiers values match MOD_ALT/CONTROL/SHIFT/WIN.
        if (!RegisterHotKey(_source.Handle, native, (uint)gesture.Modifiers | ModNoRepeat, virtualKey)) return false;
        _registered[native] = id;
        return true;
    }

    /// <summary>Releases every registered shortcut.</summary>
    public void UnregisterAll()
    {
        foreach (var native in _registered.Keys) UnregisterHotKey(_source.Handle, native);
        _registered.Clear();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && _registered.TryGetValue(wParam.ToInt32(), out var id))
        {
            handled = true;
            Pressed?.Invoke(id);
        }
        return IntPtr.Zero;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        UnregisterAll();
        _source.Dispose();
    }
}
