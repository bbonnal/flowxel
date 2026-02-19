using System;
using System.ComponentModel;

namespace Flowxel.UI.Translation;

public sealed class TranslationBindingSource : INotifyPropertyChanged
{
    private static readonly Lazy<TranslationBindingSource> InstanceFactory = new(() => new TranslationBindingSource());

    private ITranslationService? _translationService;

    private TranslationBindingSource()
    {
    }

    public static TranslationBindingSource Instance => InstanceFactory.Value;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string this[string key]
    {
        get
        {
            if (_translationService is null)
            {
                return $"[[{key}]]";
            }

            return _translationService.Translate(key);
        }
    }

    public void Initialize(ITranslationService translationService)
    {
        ArgumentNullException.ThrowIfNull(translationService);

        if (ReferenceEquals(_translationService, translationService))
        {
            return;
        }

        if (_translationService is not null)
        {
            _translationService.CultureChanged -= OnCultureChanged;
        }

        _translationService = translationService;
        _translationService.CultureChanged += OnCultureChanged;
        RaiseAllTranslationsChanged();
    }

    private void OnCultureChanged(object? sender, System.Globalization.CultureInfo e)
    {
        RaiseAllTranslationsChanged();
    }

    private void RaiseAllTranslationsChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }
}
