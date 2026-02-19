using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia;

namespace Flowxel.UI.Controls.Navigation;

/// <summary>
/// Represents an item in a navigation control.
/// </summary>
public class NavigationItemControl : TemplatedControl
{
    /// <summary>
    /// Defines the <see cref="Header"/> property.
    /// </summary>
    public static readonly StyledProperty<string?> HeaderProperty =
        AvaloniaProperty.Register<NavigationItemControl, string?>(nameof(Header));

    /// <summary>
    /// Defines the <see cref="IconData"/> property.
    /// </summary>
    public static readonly StyledProperty<Geometry?> IconDataProperty =
        AvaloniaProperty.Register<NavigationItemControl, Geometry?>(nameof(IconData));

    /// <summary>
    /// Defines the <see cref="PageType"/> property.
    /// </summary>
    public static readonly StyledProperty<Type?> PageTypeProperty =
        AvaloniaProperty.Register<NavigationItemControl, Type?>(nameof(PageType));

    /// <summary>
    /// Defines the <see cref="PageViewModelType"/> property.
    /// </summary>
    public static readonly StyledProperty<Type?> PageViewModelTypeProperty =
        AvaloniaProperty.Register<NavigationItemControl, Type?>(nameof(PageViewModelType));

    /// <summary>
    /// Gets or sets the header text for the navigation item.
    /// </summary>
    public string? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>
    /// Gets or sets the icon geometry data for the navigation item.
    /// </summary>
    public Geometry? IconData
    {
        get => GetValue(IconDataProperty);
        set => SetValue(IconDataProperty, value);
    }

    /// <summary>
    /// Gets or sets the View type to resolve for this navigation item's page.
    /// </summary>
    public Type? PageType
    {
        get => GetValue(PageTypeProperty);
        set => SetValue(PageTypeProperty, value);
    }

    /// <summary>
    /// Gets or sets the ViewModel type to resolve for this navigation item's page.
    /// </summary>
    public Type? PageViewModelType
    {
        get => GetValue(PageViewModelTypeProperty);
        set => SetValue(PageViewModelTypeProperty, value);
    }
}
