using System.Globalization;

namespace Flowxel.UI.Translation;

public interface ITranslationService
{
    CultureInfo CurrentCulture { get; }
    CultureInfo FallbackCulture { get; }

    event EventHandler<CultureInfo>? CultureChanged;

    void SetCulture(CultureInfo culture);
    string Translate(string key);
    string Translate(string key, IReadOnlyDictionary<string, object?> arguments);
    string TranslateCount(string keyBase, decimal count, IReadOnlyDictionary<string, object?>? arguments = null);
}
