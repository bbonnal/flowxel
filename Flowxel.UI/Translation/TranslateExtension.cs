using System;
using Avalonia.Data;
using Avalonia.Markup.Xaml;

namespace Flowxel.UI.Translation;

public sealed class TranslateExtension : MarkupExtension
{
    public TranslateExtension()
    {
    }

    public TranslateExtension(string key)
    {
        Key = key;
    }

    public string Key { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (string.IsNullOrWhiteSpace(Key))
        {
            return string.Empty;
        }

        return new Binding
        {
            Path = $"[{Key}]",
            Source = TranslationBindingSource.Instance,
            Mode = BindingMode.OneWay
        };
    }
}
