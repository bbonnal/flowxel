# Translation Workflow for AXML

Translation is grouped in `rUI.Avalonia.Desktop/Translation`.
The tester app only provides DI configuration and embedded JSON files.

## Architecture

Core translation functionality (package):
- `rUI.Avalonia.Desktop/Translation/ITranslationService.cs`
- `rUI.Avalonia.Desktop/Translation/TranslationService.cs`
- `rUI.Avalonia.Desktop/Translation/JsonTranslationCatalogLoader.cs`
- `rUI.Avalonia.Desktop/Translation/TranslationBindingSource.cs`
- `rUI.Avalonia.Desktop/Translation/TranslateExtension.cs`

App-side configuration:
- `Flowxel.UITester/ServiceCollectionExtensions.cs`
- `Flowxel.UITester/App.axaml.cs`

## Translation sources

All translations are embedded resources:
- `Flowxel.UITester/Resources/Translation/en.json`
- `Flowxel.UITester/Resources/Translation/fr.json`

Runtime loading:
- `JsonTranslationCatalogLoader.LoadEmbeddedResourcesByPrefix(...)`
- Prefix used: `Flowxel.UITester.Resources.Translation.`
- Each file name must be a valid culture name (`en.json`, `fr.json`, `fr-CA.json`, etc).

## AXML usage steps

1. Define keys in embedded JSON.
- Add/update key in `Resources/Translation/en.json`.
- Add same key in other embedded locale files as needed.

2. Register translation services in DI.
- Add catalog + formatter + plural rules + `ITranslationService` in `ServiceCollectionExtensions`.

3. Initialize binding source at startup.
- In `App.axaml.cs` after service provider creation:
- `TranslationBindingSource.Instance.Initialize(services.GetRequiredService<ITranslationService>());`

4. Use translation namespace in AXML.
- `xmlns:tr="using:rUI.Avalonia.Desktop.Translation"`

5. Bind strings directly by key.
- `Text="{tr:Translate settings.title}"`
- `Header="{tr:Translate settings.language.header}"`

6. For dynamic text, use ViewModel API.
- `ITranslationService.Translate(key, args)`
- `ITranslationService.TranslateCount(keyBase, count, args)`

## JSON format

Flat keys (recommended):

```json
{
  "settings.title": "Settings",
  "settings.language.header": "Language"
}
```

Nested objects are supported and flattened with dots.

## Deployment

- English-only deployment: embed only `en.json`.
- Multi-language deployment: embed additional files (`fr.json`, `de.json`, etc).
- No runtime translation files are required.

## Troubleshooting

If key renders as `[[some.key]]`:
1. Key missing in embedded translation files.
2. `TranslationBindingSource.Initialize(...)` was not called.
3. Wrong AXML namespace (`tr`).
4. File name is not a valid culture name.
