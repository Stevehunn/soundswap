using System.Text.RegularExpressions;

namespace SoundSwap.Core.Hotkeys;

/// <summary>Modifier keys of a shortcut. Values match the Win32 <c>MOD_*</c> constants used by <c>RegisterHotKey</c>.</summary>
[Flags]
public enum HotkeyModifiers
{
    /// <summary>No modifier (not valid for a global shortcut).</summary>
    None = 0,
    /// <summary>The Alt key.</summary>
    Alt = 1,
    /// <summary>The Ctrl key.</summary>
    Control = 2,
    /// <summary>The Shift key.</summary>
    Shift = 4,
    /// <summary>The Windows key.</summary>
    Windows = 8
}

/// <summary>UI-independent keyboard shortcut. <see cref="Key"/> is the WPF key name (e.g. "D1", "Right", "F5").</summary>
public readonly record struct HotkeyGesture(HotkeyModifiers Modifiers, string Key)
{
    /// <summary>Parses text such as <c>Ctrl+Alt+D1</c> (modifiers are case-insensitive; at least one is required).</summary>
    /// /// <param name="text">The text to parse.</param>
    /// /// <param name="gesture">The parsed shortcut when the method returns <c>true</c>.</param>
    /// /// <returns><c>true</c> if the text is a valid shortcut.</returns>
    public static bool TryParse(string? text, out HotkeyGesture gesture)
    {
        gesture = default;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var parts = text.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return false;

        var mods = HotkeyModifiers.None;
        foreach (var part in parts[..^1])
        {
            switch (part.ToLowerInvariant())
            {
                case "ctrl": case "control": mods |= HotkeyModifiers.Control; break;
                case "alt": mods |= HotkeyModifiers.Alt; break;
                case "shift": mods |= HotkeyModifiers.Shift; break;
                case "win": case "windows": mods |= HotkeyModifiers.Windows; break;
                default: return false;
            }
        }
        gesture = new HotkeyGesture(mods, parts[^1]);
        return true;
    }

    /// <summary>Canonical form used for storage and comparison, or null if invalid.</summary>
    public static string? Normalize(string? text) => TryParse(text, out var g) ? g.ToString() : null;

    /// <summary>Storage form: "Ctrl+Alt+Right".</summary>
    public override string ToString() => string.Join('+', ModifierNames().Append(Key));

    /// <summary>Human form: "Ctrl + Alt + →".</summary>
    public string Display => string.Join(" + ", ModifierNames().Append(KeyDisplay(Key)));

    private IEnumerable<string> ModifierNames()
    {
        if (Modifiers.HasFlag(HotkeyModifiers.Control)) yield return "Ctrl";
        if (Modifiers.HasFlag(HotkeyModifiers.Alt)) yield return "Alt";
        if (Modifiers.HasFlag(HotkeyModifiers.Shift)) yield return "Shift";
        if (Modifiers.HasFlag(HotkeyModifiers.Windows)) yield return "Win";
    }

    private static string KeyDisplay(string key)
    {
        switch (key)
        {
            case "Right": return "→";
            case "Left": return "←";
            case "Up": return "↑";
            case "Down": return "↓";
        }
        var digit = Regex.Match(key, @"^D(\d)$");
        if (digit.Success) return digit.Groups[1].Value;
        var pad = Regex.Match(key, @"^NumPad(\d)$");
        return pad.Success ? "Num " + pad.Groups[1].Value : key;
    }
}
