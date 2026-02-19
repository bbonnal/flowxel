using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace Flowxel.UI.Translation;

public static class JsonTranslationCatalogLoader
{
    public static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> LoadEmbeddedResourcesByPrefix(
        Assembly assembly,
        string resourcePrefix)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourcePrefix);

        var catalog = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        var names = assembly.GetManifestResourceNames();
        foreach (var name in names)
        {
            if (!name.StartsWith(resourcePrefix, StringComparison.Ordinal))
            {
                continue;
            }

            if (!name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var suffix = name.Substring(resourcePrefix.Length);
            if (suffix.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                suffix = suffix[..^5];
            }

            if (string.IsNullOrWhiteSpace(suffix))
            {
                continue;
            }

            CultureInfo culture;
            try
            {
                culture = CultureInfo.GetCultureInfo(suffix);
            }
            catch (CultureNotFoundException)
            {
                continue;
            }

            using var stream = assembly.GetManifestResourceStream(name);
            if (stream is null)
            {
                continue;
            }

            var entries = ParseEntries(stream);
            catalog[culture.Name] = entries;
        }

        return catalog;
    }

    private static IReadOnlyDictionary<string, string> ParseEntries(Stream stream)
    {
        using var doc = JsonDocument.Parse(stream);
        if (doc.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("Translation JSON root must be an object.");
        }

        var entries = new Dictionary<string, string>(StringComparer.Ordinal);
        Flatten(doc.RootElement, string.Empty, entries);
        return entries;
    }

    private static void Flatten(JsonElement element, string prefix, IDictionary<string, string> target)
    {
        foreach (var property in element.EnumerateObject())
        {
            var key = string.IsNullOrWhiteSpace(prefix) ? property.Name : $"{prefix}.{property.Name}";

            if (property.Value.ValueKind == JsonValueKind.Object)
            {
                Flatten(property.Value, key, target);
                continue;
            }

            if (property.Value.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var value = property.Value.GetString();
            if (value is not null)
            {
                target[key] = value;
            }
        }
    }
}
