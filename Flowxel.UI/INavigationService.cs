using Flowxel.UI.Controls.Navigation;
using Avalonia.Controls;

namespace Flowxel.UI;

/// <summary>
/// Defines a service for managing navigation within the application.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Gets or sets the currently selected navigation item.
    /// </summary>
    NavigationItemControl? SelectedItem { get; set; }

    /// <summary>
    /// Gets the main navigation items.
    /// </summary>
    IReadOnlyList<NavigationItemControl> Items { get; }

    /// <summary>
    /// Gets the footer navigation items.
    /// </summary>
    IReadOnlyList<NavigationItemControl> FooterItems { get; }

    /// <summary>
    /// Initializes navigation items.
    /// </summary>
    void Initialize(IReadOnlyList<NavigationItemControl> items, IReadOnlyList<NavigationItemControl>? footerItems = null);

    /// <summary>
    /// Gets the currently displayed page.
    /// </summary>
    Control? CurrentPage { get; }

    /// <summary>
    /// Gets or sets the factory function used to create pages from navigation items.
    /// </summary>
    Func<NavigationItemControl, Control> PageFactory { get; set; }

    /// <summary>
    /// Navigates to a page by resolving the specified ViewModel type.
    /// </summary>
    /// <typeparam name="TViewModel">The ViewModel type to navigate to.</typeparam>
    Task NavigateToAsync<TViewModel>() where TViewModel : class;

    /// <summary>
    /// Navigates to a page by resolving the specified ViewModel type.
    /// </summary>
    /// <param name="viewModelType">The ViewModel type to navigate to.</param>
    Task NavigateToAsync(Type viewModelType);
}
