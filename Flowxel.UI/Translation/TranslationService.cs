using System.Globalization;
using System.Text.RegularExpressions;

namespace Flowxel.UI.Translation;

public sealed class TranslationService : ITranslationService
{
    private static readonly Regex PlaceholderRegex = new("\\{([A-Za-z0-9_.-]+)\\}", RegexOptions.Compiled);
    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _catalogByCulture;
    private readonly object _lock = new();
    private CultureInfo _currentCulture;

    public TranslationService(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> catalogByCulture,
        CultureInfo? initialCulture = null,
        CultureInfo? fallbackCulture = null)
    {
        _catalogByCulture = catalogByCulture;
        _currentCulture = initialCulture ?? CultureInfo.CurrentUICulture;
        FallbackCulture = fallbackCulture ?? CultureInfo.GetCultureInfo("en");
    }

    public CultureInfo CurrentCulture
    {
        get
        {
            lock (_lock)
            {
                return _currentCulture;
            }
        }
    }

    public CultureInfo FallbackCulture { get; }

    public event EventHandler<CultureInfo>? CultureChanged;

    public void SetCulture(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);

        lock (_lock)
        {
            if (Equals(_currentCulture, culture))
            {
                return;
            }

            _currentCulture = culture;
        }

        CultureChanged?.Invoke(this, culture);
    }

    public string Translate(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var template = ResolveTemplate(key, CurrentCulture);
        return template ?? MissingKeyMarker(key);
    }

    public string Translate(string key, IReadOnlyDictionary<string, object?> arguments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(arguments);

        var culture = CurrentCulture;
        var template = ResolveTemplate(key, culture);
        if (template is null)
        {
            return MissingKeyMarker(key);
        }

        return FormatTemplate(template, arguments, culture);
    }

    public string TranslateCount(string keyBase, decimal count, IReadOnlyDictionary<string, object?>? arguments = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyBase);

        var culture = CurrentCulture;
        var category = SelectPluralCategory(count);
        var key = $"{keyBase}.{category}";
        var template = ResolveTemplate(key, culture)
            ?? ResolveTemplate($"{keyBase}.other", culture)
            ?? ResolveTemplate(keyBase, culture);

        if (template is null)
        {
            return MissingKeyMarker(key);
        }

        var formattingArguments = arguments is null
            ? new Dictionary<string, object?>(StringComparer.Ordinal)
            : new Dictionary<string, object?>(arguments, StringComparer.Ordinal);

        formattingArguments.TryAdd("Count", count);
        return FormatTemplate(template, formattingArguments, culture);
    }

    private string? ResolveTemplate(string key, CultureInfo culture)
    {
        foreach (var candidate in EnumerateFallbackCultures(culture))
        {
            if (_catalogByCulture.TryGetValue(candidate.Name, out var entries) &&
                entries.TryGetValue(key, out var value))
            {
                return value;
            }
        }

        return null;
    }

    private IEnumerable<CultureInfo> EnumerateFallbackCultures(CultureInfo culture)
    {
        for (var current = culture; !string.IsNullOrWhiteSpace(current.Name); current = current.Parent)
        {
            yield return current;
        }

        yield return CultureInfo.InvariantCulture;

        if (culture.Name.Equals(FallbackCulture.Name, StringComparison.OrdinalIgnoreCase))
        {
            yield break;
        }

        for (var current = FallbackCulture; !string.IsNullOrWhiteSpace(current.Name); current = current.Parent)
        {
            yield return current;
        }
    }

    private static string SelectPluralCategory(decimal count)
    {
        if (count == 0)
        {
            return "zero";
        }

        return count == 1 ? "one" : "other";
    }

    private static string FormatTemplate(string template, IReadOnlyDictionary<string, object?> arguments, IFormatProvider formatProvider)
    {
        if (arguments.Count == 0)
        {
            return template;
        }

        return PlaceholderRegex.Replace(template, match =>
        {
            var key = match.Groups[1].Value;
            if (!arguments.TryGetValue(key, out var value) || value is null)
            {
                return match.Value;
            }

            return value switch
            {
                IFormattable formattable => formattable.ToString(null, formatProvider),
                _ => value.ToString() ?? string.Empty
            };
        });
    }

    private static string MissingKeyMarker(string key)
    {
        return $"[[{key}]]";
    }
}
