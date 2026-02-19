using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Flowxel.UI.Controls.Navigation;
using Microsoft.Extensions.DependencyInjection;

namespace Flowxel.UI.Services;

public class NavigationService(IServiceProvider serviceProvider) : ObservableObject, INavigationService
{
    private readonly SemaphoreSlim _navigationLock = new(1, 1);

    public Func<NavigationItemControl, Control> PageFactory { get; set; } = navItem =>
    {
        if (navItem.PageType is null || navItem.PageViewModelType is null)
            throw new InvalidOperationException("Navigation item must define PageType and PageViewModelType.");

        var page = serviceProvider.GetRequiredService(navItem.PageType) as Control
                   ?? throw new InvalidOperationException($"Page type '{navItem.PageType.FullName}' is not registered as a Control.");
        page.DataContext = serviceProvider.GetRequiredService(navItem.PageViewModelType);
        return page;
    };

    public object? CurrentPage
    {
        get;
        set => SetProperty(ref field, value);
    }

    public NavigationItemControl? SelectedItem
    {
        get;
        set
        {
            var previousItem = field;
            if (SetProperty(ref field, value))
                _ = TryNavigateToItemAsync(value, previousItem);
        }
    }

    public IReadOnlyList<NavigationItemControl> Items { get; private set; } = [];

    public IReadOnlyList<NavigationItemControl>? FooterItems { get; private set; }

    public void Initialize(IReadOnlyList<NavigationItemControl> items, IReadOnlyList<NavigationItemControl>? footerItems = null)
    {
        Items = items;
        FooterItems = footerItems;
    }

    public Task NavigateToAsync<TViewModel>() where TViewModel : class
        => NavigateToAsync(typeof(TViewModel));

    public async Task NavigateToAsync(Type viewModelType)
    {
        if (!await _navigationLock.WaitAsync(0))
            return;

        try
        {
            if (CurrentPage is not null)
            {
                var allowed = await InvokeDisappearingAsync(CurrentPage);
                if (!allowed)
                    return;
            }

            var target = FindItemForViewModel(viewModelType);
            CurrentPage = target is not null ? PageFactory(target) : null;
            SelectedItem = target;

            if (CurrentPage is not null)
                await InvokeAppearingAsync(CurrentPage);
        }
        finally
        {
            _navigationLock.Release();
        }
    }

    private async Task TryNavigateToItemAsync(NavigationItemControl? targetItem, NavigationItemControl? previousItem)
    {
        if (!await _navigationLock.WaitAsync(0))
            return;

        try
        {
            if (CurrentPage is not null)
            {
                var allowed = await InvokeDisappearingAsync(CurrentPage);
                if (!allowed)
                {
                    SelectedItem = previousItem;
                    return;
                }
            }

            CurrentPage = targetItem is not null ? PageFactory(targetItem) : null;

            if (CurrentPage is not null)
                await InvokeAppearingAsync(CurrentPage);
        }
        finally
        {
            _navigationLock.Release();
        }
    }

    private NavigationItemControl? FindItemForViewModel(Type viewModelType)
    {
        foreach (var item in Items)
        {
            if (item.PageViewModelType == viewModelType)
                return item;
        }

        if (FooterItems is null)
            return null;

        foreach (var item in FooterItems)
        {
            if (item.PageViewModelType == viewModelType)
                return item;
        }

        return null;
    }

    private static async Task<bool> InvokeDisappearingAsync(object page)
    {
        try
        {
            var vm = page is Control control ? control.DataContext : page;
            if (vm is INavigationViewModel nav)
                return await nav.OnDisappearingAsync();
            return true;
        }
        catch
        {
            return true;
        }
    }

    private static async Task InvokeAppearingAsync(object page)
    {
        try
        {
            var vm = page is Control control ? control.DataContext : page;
            if (vm is INavigationViewModel nav)
                await nav.OnAppearingAsync();
        }
        catch
        {
            // ignored
        }
    }
}
