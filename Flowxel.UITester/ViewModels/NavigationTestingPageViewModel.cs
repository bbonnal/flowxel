using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Flowxel.UI;

namespace Flowxel.UITester.ViewModels;

public class NavigationTestingPageViewModel : ViewModelBase
{
    public NavigationTestingPageViewModel(INavigationService navigation)
    {
        _navigation = navigation;
        NavigateToSettingsCommand = new AsyncRelayCommand(NavigateToSettings);
        NavigateToDummyCommand = new AsyncRelayCommand(NavigateToDummy);
    }

    private readonly INavigationService _navigation;

    public IAsyncRelayCommand NavigateToSettingsCommand { get; }
    public IAsyncRelayCommand NavigateToDummyCommand { get; }

    private async Task NavigateToSettings()
    {
        await _navigation.NavigateToAsync<SettingsPageViewModel>();
    }

    private async Task NavigateToDummy()
    {
        await _navigation.NavigateToAsync<DummyPageViewModel>();
    }
}
