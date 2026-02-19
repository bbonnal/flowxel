using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Flowxel.UI;
using Flowxel.UI.Services;
using Flowxel.UI.Services.Shortcuts;
using Flowxel.UI.Translation;
using Flowxel.UITester.Services;
using Flowxel.UITester.ViewModels;
using Flowxel.UITester.Views;

namespace Flowxel.UITester;

public static class ServiceCollectionExtensions
{
    public static void AddCommonServices(this IServiceCollection services)
    {
        ConfigureLogging(services);
        ConfigureTranslation(services);

        // Core app services are shared and host-backed.
        _ = services.AddSingleton<NavigationService>();
        _ = services.AddSingleton<INavigationService>(sp => sp.GetRequiredService<NavigationService>());
        _ = services.AddSingleton<IContentDialogService, ContentDialogService>();
        _ = services.AddSingleton<IInfoBarService, InfoBarService>();
        _ = services.AddSingleton<IOverlayService, OverlayService>();
        _ = services.AddSingleton<IFileDialogService, FileDialogService>();
        _ = services.AddSingleton<IFolderDialogService, FolderDialogService>();
        _ = services.AddSingleton<IShortcutService, ShortcutService>();
        _ = services.AddSingleton<IAppSettingsStore, JsonAppSettingsStore>();

        _ = services.AddSingleton<MainWindow>();
        _ = services.AddSingleton<MainWindowViewModel>();

        _ = services.AddTransient<ContentDialogTestingPageView>();
        _ = services.AddTransient<DialogsTestingPageView>();
        _ = services.AddTransient<OverlayTestingPageView>();
        _ = services.AddTransient<InfoBarTestingPageView>();
        _ = services.AddTransient<ChartsPageView>();
        _ = services.AddTransient<ExpanderTestingPageView>();
        _ = services.AddTransient<SchedulePageView>();
        _ = services.AddTransient<RibbonCanvasTestingPageView>();
        _ = services.AddTransient<DockingTestingPageView>();
        _ = services.AddTransient<DockingCanvasTestingPageView>();
        _ = services.AddTransient<NavigationTestingPageView>();
        _ = services.AddTransient<NavigationCancellationDemoPageView>();
        _ = services.AddTransient<EditorsTestingPageView>();
        _ = services.AddTransient<DummyPageView>();
        _ = services.AddTransient<SettingsPageView>();

        _ = services.AddSingleton<ContentDialogTestingPageViewModel>();
        _ = services.AddSingleton<DialogsTestingPageViewModel>();
        _ = services.AddSingleton<OverlayTestingPageViewModel>();
        _ = services.AddSingleton<InfoBarTestingPageViewModel>();
        _ = services.AddSingleton<ChartsPageViewModel>();
        _ = services.AddSingleton<ExpanderTestingPageViewModel>();
        _ = services.AddSingleton<SchedulePageViewModel>();
        _ = services.AddSingleton<RibbonCanvasTestingPageViewModel>();
        _ = services.AddSingleton<DockingTestingPageViewModel>();
        _ = services.AddSingleton<DockingCanvasTestingPageViewModel>();
        _ = services.AddSingleton<NavigationTestingPageViewModel>();
        _ = services.AddSingleton<NavigationCancellationDemoPageViewModel>();
        _ = services.AddSingleton<EditorsTestingPageViewModel>();
        _ = services.AddSingleton<DummyPageViewModel>();
        _ = services.AddSingleton<SettingsPageViewModel>();
    }

    private static void ConfigureLogging(IServiceCollection services)
    {
        _ = services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        _ = services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
    }

    private static void ConfigureTranslation(IServiceCollection services)
    {
        var catalog = BuildCatalogFromResources();
        _ = services.AddSingleton<ITranslationService>(sp => new TranslationService(
            catalog,
            CultureInfo.CurrentUICulture,
            CultureInfo.GetCultureInfo("en")));
    }

    private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> BuildCatalogFromResources()
    {
        return JsonTranslationCatalogLoader.LoadEmbeddedResourcesByPrefix(
            Assembly.GetExecutingAssembly(),
            "Flowxel.UITester.Resources.Translation.");
    }
}
