using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.Input;
using PhosphorIconsAvalonia;
using Flowxel.UI;
using Flowxel.UI.Controls.Navigation;
using Flowxel.UI.Services;
using Flowxel.UITester.Services;
using Flowxel.UITester.Views;

namespace Flowxel.UITester.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private bool _isInitialized;
    private readonly IAppSettingsStore _settings;

    public MainWindowViewModel(
        INavigationService navigation,
        IContentDialogService dialogService,
        IOverlayService overlayService,
        IInfoBarService infoBarService,
        IAppSettingsStore settings)
    {
        Navigation = navigation;
        DialogService = dialogService;
        OverlayService = overlayService;
        InfoBarService = infoBarService;
        _settings = settings;
        ToggleThemeCommand = new RelayCommand(ToggleTheme);

        var items = new[]
        {
            new NavigationItemControl
            {
                Header = "Dialogs",
                IconData = IconService.CreateGeometry(Icon.chat_circle_text, IconType.regular),
                PageType = typeof(ContentDialogTestingPageView),
                PageViewModelType = typeof(ContentDialogTestingPageViewModel)
            },
            new NavigationItemControl
            {
                Header = "File Dialogs",
                IconData = IconService.CreateGeometry(Icon.folder_open, IconType.regular),
                PageType = typeof(DialogsTestingPageView),
                PageViewModelType = typeof(DialogsTestingPageViewModel)
            },
            new NavigationItemControl
            {
                Header = "Overlay",
                IconData = IconService.CreateGeometry(Icon.check_circle, IconType.regular),
                PageType = typeof(OverlayTestingPageView),
                PageViewModelType = typeof(OverlayTestingPageViewModel)
            },
            new NavigationItemControl
            {
                Header = "InfoBar",
                IconData = IconService.CreateGeometry(Icon.info, IconType.regular),
                PageType = typeof(InfoBarTestingPageView),
                PageViewModelType = typeof(InfoBarTestingPageViewModel)
            },
            new NavigationItemControl
            {
                Header = "Charts",
                IconData = IconService.CreateGeometry(Icon.chart_bar, IconType.regular),
                PageType = typeof(ChartsPageView),
                PageViewModelType = typeof(ChartsPageViewModel)
            },
            new NavigationItemControl
            {
                Header = "Expander",
                IconData = IconService.CreateGeometry(Icon.caret_circle_up_down, IconType.regular),
                PageType = typeof(ExpanderTestingPageView),
                PageViewModelType = typeof(ExpanderTestingPageViewModel)
            },
            new NavigationItemControl
            {
                Header = "Schedule",
                IconData = IconService.CreateGeometry(Icon.calendar, IconType.regular),
                PageType = typeof(SchedulePageView),
                PageViewModelType = typeof(SchedulePageViewModel)
            },
            new NavigationItemControl
            {
                Header = "Ribbon Canvas",
                IconData = IconService.CreateGeometry(Icon.app_window, IconType.regular),
                PageType = typeof(RibbonCanvasTestingPageView),
                PageViewModelType = typeof(RibbonCanvasTestingPageViewModel)
            },
            new NavigationItemControl
            {
                Header = "Imaging Canvas",
                IconData = IconService.CreateGeometry(Icon.app_window, IconType.regular),
                PageType = typeof(ImagingCanvasPageView),
                PageViewModelType = typeof(ImagingCanvasPageViewModel)
            },
            new NavigationItemControl
            {
                Header = "Docking",
                IconData = IconService.CreateGeometry(Icon.square_split_horizontal, IconType.regular),
                PageType = typeof(DockingTestingPageView),
                PageViewModelType = typeof(DockingTestingPageViewModel)
            },
            new NavigationItemControl
            {
                Header = "Docked Canvas",
                IconData = IconService.CreateGeometry(Icon.app_window, IconType.regular),
                PageType = typeof(DockingCanvasTestingPageView),
                PageViewModelType = typeof(DockingCanvasTestingPageViewModel)
            },
            new NavigationItemControl
            {
                Header = "Navigate",
                IconData = IconService.CreateGeometry(Icon.compass, IconType.regular),
                PageType = typeof(NavigationTestingPageView),
                PageViewModelType = typeof(NavigationTestingPageViewModel)
            },
            new NavigationItemControl
            {
                Header = "Nav Cancel",
                IconData = IconService.CreateGeometry(Icon.file_x, IconType.regular),
                PageType = typeof(NavigationCancellationDemoPageView),
                PageViewModelType = typeof(NavigationCancellationDemoPageViewModel)
            },
            new NavigationItemControl
            {
                Header = "Editors",
                IconData = Geometry.Parse("M3 17.25V21h3.75L17.81 9.94l-3.75-3.75L3 17.25zM20.71 7.04a1 1 0 0 0 0-1.41l-2.34-2.34a1 1 0 0 0-1.41 0l-1.83 1.83 3.75 3.75 1.83-1.83z"),
                PageType = typeof(EditorsTestingPageView),
                PageViewModelType = typeof(EditorsTestingPageViewModel)
            }
        };

        var footerItems = new[]
        {
            new NavigationItemControl
            {
                Header = "Settings",
                IconData = IconService.CreateGeometry(Icon.gear, IconType.regular),
                PageType = typeof(SettingsPageView),
                PageViewModelType = typeof(SettingsPageViewModel)
            }
        };

        Logo = new Avalonia.Controls.PathIcon
        {
            Data = Geometry.Parse("M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5"),
            Width = 28,
            Height = 28,
            Foreground = new SolidColorBrush(Color.FromRgb(99, 102, 241))
        };

        Navigation.Initialize(items, footerItems);
    }

    public INavigationService Navigation { get; }
    public IContentDialogService DialogService { get; }
    public IOverlayService OverlayService { get; }
    public IInfoBarService InfoBarService { get; }
    public object Logo { get; }

    public IRelayCommand ToggleThemeCommand { get; }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        _isInitialized = true;
        await Navigation.NavigateToAsync<ContentDialogTestingPageViewModel>();
    }

    private void ToggleTheme()
    {
        var app = Application.Current;
        if (app != null)
        {
            app.RequestedThemeVariant = app.ActualThemeVariant == ThemeVariant.Dark
                ? ThemeVariant.Light
                : ThemeVariant.Dark;
            _settings.SetTheme(app.RequestedThemeVariant);
        }
    }
}
