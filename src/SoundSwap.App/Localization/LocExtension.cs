using System;
using System.Windows.Data;
using System.Windows.Markup;

namespace SoundSwap.Desktop.Localization;

/// <summary>XAML usage: Text="{loc:Loc validate}". Updates live when the language changes.</summary>
public sealed class LocExtension : MarkupExtension
{
    /// <summary>Creates the extension.</summary>
    /// /// <param name="key">Resource key.</param>
    public LocExtension(string key) => Key = key;

    /// <summary>Resource key to translate.</summary>
    public string Key { get; set; }

    /// <inheritdoc/>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new Binding($"[{Key}]") { Source = Localizer.Instance, Mode = BindingMode.OneWay };
        return binding.ProvideValue(serviceProvider);
    }
}
