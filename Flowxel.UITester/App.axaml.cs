using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Flowxel.UI.Translation;
using Flowxel.UITester.ViewModels;
using Flowxel.UITester.Views;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Microsoft.Extensions.DependencyInjection;
using Flowxel.UI.Services;
using Flowxel.UI.Services.Shortcuts;
using Flowxel.UITester.Services;

namespace Flowxel.UITester;

public partial class App : Application
{
    public static IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        LiveCharts.Configure(settings => settings
            .AddSkiaSharp()
            .AddDefaultMappers()
            .AddDarkTheme());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var servicesCollection = new ServiceCollection();
        servicesCollection.AddCommonServices();
        var services = servicesCollection.BuildServiceProvider();
        Services = services;

        var translationService = services.GetRequiredService<ITranslationService>();
        TranslationBindingSource.Instance.Initialize(translationService);
        var settings = services.GetRequiredService<IAppSettingsStore>();
        settings.Load();

        if (!string.IsNullOrWhiteSpace(settings.Current.Culture))
        {
            try
            {
                translationService.SetCulture(System.Globalization.CultureInfo.GetCultureInfo(settings.Current.Culture));
            }
            catch
            {
                // Keep default culture when persisted value is invalid.
            }
        }

        RequestedThemeVariant = string.Equals(settings.Current.Theme, "Light", StringComparison.OrdinalIgnoreCase)
            ? ThemeVariant.Light
            : ThemeVariant.Dark;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();

            var mainWindow = services.GetRequiredService<MainWindow>();
            var vm = services.GetRequiredService<MainWindowViewModel>();
            mainWindow.DataContext = vm;

            // Register hosts directly via services
            // This is mandatory for theses services to work above the main window
            services.GetRequiredService<IContentDialogService>().RegisterHost(mainWindow.HostDialog);
            services.GetRequiredService<IOverlayService>().RegisterHost(mainWindow.HostOverlay);
            services.GetRequiredService<IInfoBarService>().RegisterHost(mainWindow.HostInfoBar);

            desktop.MainWindow = mainWindow;

            // Handle ViewModel initialization
            mainWindow.Opened += async (_, _) => await vm.InitializeAsync();
        }

        // Keep the global handler for dynamic views (like UserControls in TabItems) 
        // that need shortcuts bound when they appear
        Control.DataContextProperty.Changed.AddClassHandler<Control>((control, e) =>
        {
            if (e.NewValue is IShortcutBindingProvider provider)
            {
                services.GetService<IShortcutService>()?.Bind(control, provider.GetShortcutDefinitions());
            }
        });

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
