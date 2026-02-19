using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Flowxel.ImagingTester.ViewModels;
using Flowxel.ImagingTester.Views;
using Flowxel.UI.Services;

namespace Flowxel.ImagingTester;

public partial class App : Application
{
    public static IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var servicesCollection = new ServiceCollection();
        servicesCollection.AddImagingTesterServices();

        var services = servicesCollection.BuildServiceProvider();
        Services = services;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = services.GetRequiredService<MainWindow>();
            var vm = services.GetRequiredService<MainWindowViewModel>();
            mainWindow.DataContext = vm;

            services.GetRequiredService<IContentDialogService>().RegisterHost(mainWindow.HostDialog);
            services.GetRequiredService<IInfoBarService>().RegisterHost(mainWindow.HostInfoBar);

            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
