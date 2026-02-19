using Flowxel.ImagingTester.ViewModels;
using Flowxel.ImagingTester.Views;
using Flowxel.UI;
using Flowxel.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Flowxel.ImagingTester;

public static class ServiceCollectionExtensions
{
    public static void AddImagingTesterServices(this IServiceCollection services)
    {
        _ = services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        _ = services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        _ = services.AddSingleton<IContentDialogService, ContentDialogService>();
        _ = services.AddSingleton<IFileDialogService, FileDialogService>();
        _ = services.AddSingleton<IInfoBarService, InfoBarService>();

        _ = services.AddSingleton<MainWindow>();
        _ = services.AddSingleton<MainWindowViewModel>();

        _ = services.AddSingleton<ImagingCanvasPageViewModel>();
        _ = services.AddSingleton<ImagingCanvasPageView>();
    }
}
