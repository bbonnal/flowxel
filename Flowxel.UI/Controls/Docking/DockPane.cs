using Avalonia.Controls.Primitives;
using Avalonia.Metadata;
using Avalonia;

namespace Flowxel.UI.Controls.Docking;

public class DockPane : TemplatedControl
{
    public static readonly StyledProperty<string?> HeaderProperty =
        AvaloniaProperty.Register<DockPane, string?>(nameof(Header));

    public static readonly StyledProperty<object?> PaneContentProperty =
        AvaloniaProperty.Register<DockPane, object?>(nameof(PaneContent));

    public static readonly StyledProperty<bool> CanCloseProperty =
        AvaloniaProperty.Register<DockPane, bool>(nameof(CanClose), true);

    public static readonly StyledProperty<bool> CanMoveProperty =
        AvaloniaProperty.Register<DockPane, bool>(nameof(CanMove), true);

    public static readonly StyledProperty<DockPaneSizeMode> HorizontalSizeModeProperty =
        AvaloniaProperty.Register<DockPane, DockPaneSizeMode>(nameof(HorizontalSizeMode), DockPaneSizeMode.Stretch);

    public static readonly StyledProperty<DockPaneSizeMode> VerticalSizeModeProperty =
        AvaloniaProperty.Register<DockPane, DockPaneSizeMode>(nameof(VerticalSizeMode), DockPaneSizeMode.Stretch);

    public static readonly StyledProperty<double> PreferredWidthProperty =
        AvaloniaProperty.Register<DockPane, double>(nameof(PreferredWidth), double.NaN);

    public static readonly StyledProperty<double> PreferredHeightProperty =
        AvaloniaProperty.Register<DockPane, double>(nameof(PreferredHeight), double.NaN);

    public string? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    [Content]
    public object? PaneContent
    {
        get => GetValue(PaneContentProperty);
        set => SetValue(PaneContentProperty, value);
    }

    public bool CanClose
    {
        get => GetValue(CanCloseProperty);
        set => SetValue(CanCloseProperty, value);
    }

    public bool CanMove
    {
        get => GetValue(CanMoveProperty);
        set => SetValue(CanMoveProperty, value);
    }

    public DockPaneSizeMode HorizontalSizeMode
    {
        get => GetValue(HorizontalSizeModeProperty);
        set => SetValue(HorizontalSizeModeProperty, value);
    }

    public DockPaneSizeMode VerticalSizeMode
    {
        get => GetValue(VerticalSizeModeProperty);
        set => SetValue(VerticalSizeModeProperty, value);
    }

    public double PreferredWidth
    {
        get => GetValue(PreferredWidthProperty);
        set => SetValue(PreferredWidthProperty, value);
    }

    public double PreferredHeight
    {
        get => GetValue(PreferredHeightProperty);
        set => SetValue(PreferredHeightProperty, value);
    }
}
