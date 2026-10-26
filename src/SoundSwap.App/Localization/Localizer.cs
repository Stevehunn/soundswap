using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Data;

namespace SoundSwap.Desktop.Localization;

/// <summary>A language the interface can be shown in.</summary>
/// /// <param name="Code">Two-letter code, also the name of the JSON file.</param>
/// /// <param name="NativeName">Name in its own language, shown in the dropdown.</param>
public sealed record LanguageInfo(string Code, string NativeName)
{
    public override string ToString() => NativeName;
}

/// <summary>
/// Runtime translations. Each language is an embedded JSON file (Localization/Lang/xx.json);
/// to add a language: add the file and one line in <see cref="Languages"/>.
/// </summary>
public sealed class Localizer : INotifyPropertyChanged
{
    /// <summary>The single shared instance (XAML bindings use it).</summary>
    public static Localizer Instance { get; } = new();

    /// <summary>Supported languages. To add one: add its JSON file and a line here.</summary>
    public static IReadOnlyList<LanguageInfo> Languages { get; } = new[]
    {
        new LanguageInfo("fr", "Français"), new LanguageInfo("en", "English"),
        new LanguageInfo("es", "Español"), new LanguageInfo("de", "Deutsch"),
        new LanguageInfo("pt", "Português"), new LanguageInfo("it", "Italiano"),
        new LanguageInfo("ru", "Русский"), new LanguageInfo("zh", "中文（简体）"),
        new LanguageInfo("ja", "日本語"), new LanguageInfo("ar", "العربية"),
    };

    private Dictionary<string, string> _current = new();
    private Dictionary<string, string> _fallback = new();

    /// <summary>Raised when the language changes so every <c>{loc:Loc ...}</c> binding refreshes.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Code of the current language.</summary>
    public string Code { get; private set; } = "en";
    /// <summary>Right-to-left for Arabic, left-to-right otherwise.</summary>
    public FlowDirection FlowDirection => Code == "ar" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    /// <summary>Translated text for a key; falls back to English, then to the key itself.</summary>
    /// /// <param name="key">Resource key.</param>
    public string this[string key] =>
        _current.TryGetValue(key, out var value) ? value :
        _fallback.TryGetValue(key, out var fallback) ? fallback : key;

    /// <summary>Translated text with <see cref="string.Format(string, object[])"/> arguments.</summary>
    /// /// <param name="key">Resource key.</param>
    /// /// <param name="args">Values for <c>{0}</c>, <c>{1}</c>...</param>
    public string Format(string key, params object[] args) => string.Format(this[key], args);

    /// <summary>Picks the Windows display language if supported, otherwise English.</summary>
    public static string Detect()
    {
        var code = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return Languages.Any(l => l.Code == code) ? code : "en";
    }

    /// <summary>Switches the language at runtime.</summary>
    /// /// <param name="code">Two-letter language code; unknown codes give English.</param>
    public void Load(string code)
    {
        if (Languages.All(l => l.Code != code)) code = "en";
        if (_fallback.Count == 0) _fallback = Read("en");
        _current = Read(code);
        Code = code;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Binding.IndexerName));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FlowDirection)));
    }

    private static Dictionary<string, string> Read(string code)
    {
        var name = $"SoundSwap.Desktop.Localization.Lang.{code}.json";
        using var stream = typeof(Localizer).Assembly.GetManifestResourceStream(name)
                           ?? throw new InvalidOperationException($"Missing language file: {name}");
        return JsonSerializer.Deserialize<Dictionary<string, string>>(stream) ?? new();
    }
}
