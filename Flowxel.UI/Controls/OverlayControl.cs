using Avalonia.Controls;
using Avalonia.Media;
using Avalonia;

namespace Flowxel.UI.Controls;

public class OverlayControl : ContentControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<OverlayControl, string?>(nameof(Title));

    public static readonly StyledProperty<string?> MessageProperty =
        AvaloniaProperty.Register<OverlayControl, string?>(nameof(Message));

    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<OverlayControl, bool>(nameof(IsOpen), false);

    public static readonly StyledProperty<bool> IsIndeterminateProperty =
        AvaloniaProperty.Register<OverlayControl, bool>(nameof(IsIndeterminate), true);

    public static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<OverlayControl, double>(nameof(Progress), 0);

    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<OverlayControl, double>(nameof(Minimum), 0);

    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<OverlayControl, double>(nameof(Maximum), 100);

    public static readonly StyledProperty<IBrush?> OverlayBrushProperty =
        AvaloniaProperty.Register<OverlayControl, IBrush?>(
            nameof(OverlayBrush),
            new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)));

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public bool IsIndeterminate
    {
        get => GetValue(IsIndeterminateProperty);
        set => SetValue(IsIndeterminateProperty, value);
    }

    public double Progress
    {
        get => GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public double Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public IBrush? OverlayBrush
    {
        get => GetValue(OverlayBrushProperty);
        set => SetValue(OverlayBrushProperty, value);
    }
}
