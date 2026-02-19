using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Flowxel.UI.Translation;
using Flowxel.UITester.Services;

namespace Flowxel.UITester.ViewModels;

public class SettingsPageViewModel : ViewModelBase
{
    private readonly ITranslationService _translations;
    private readonly ILogger<SettingsPageViewModel> _logger;
    private readonly IAppSettingsStore _settings;
    private int _pendingChanges;

    public SettingsPageViewModel(
        ITranslationService translations,
        ILogger<SettingsPageViewModel> logger,
        IAppSettingsStore settings)
    {
        _translations = translations;
        _logger = logger;
        _settings = settings;

        CardClickedCommand = new RelayCommand<string?>(CardClicked);
        SetEnglishCommand = new RelayCommand(() => SetCulture("en"));
        SetFrenchCommand = new RelayCommand(() => SetCulture("fr"));

        RefreshTranslations();
    }

    public string SaveStatus
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    public string CurrentLanguageDisplay
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    public string? LastAction
    {
        get;
        set => SetProperty(ref field, value);
    }

    public IRelayCommand CardClickedCommand { get; }
    public IRelayCommand SetEnglishCommand { get; }
    public IRelayCommand SetFrenchCommand { get; }

    private void CardClicked(string? parameter)
    {
        _pendingChanges++;
        LastAction = _translations.Translate("settings.lastAction", new Dictionary<string, object?>
        {
            ["Section"] = parameter ?? "Unknown"
        });
        SaveStatus = _translations.TranslateCount("settings.saveStatus", _pendingChanges);

        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["Feature"] = "Settings",
            ["Action"] = "CardClicked"
        });

        _logger.LogInformation("Settings card clicked. Section={Section} PendingChanges={PendingChanges}", parameter, _pendingChanges);
    }

    private void SetCulture(string cultureName)
    {
        var culture = System.Globalization.CultureInfo.GetCultureInfo(cultureName);
        _translations.SetCulture(culture);
        _settings.SetCulture(culture);
        RefreshTranslations();
        SaveStatus = _translations.TranslateCount("settings.saveStatus", _pendingChanges);

        _logger.LogInformation("UI culture changed to {Culture}", culture.Name);
        CardClicked("Language");
    }

    private void RefreshTranslations()
    {
        CurrentLanguageDisplay = _translations.Translate("settings.language.current", new Dictionary<string, object?>
        {
            ["Language"] = _translations.CurrentCulture.NativeName
        });
    }
}
