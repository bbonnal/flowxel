using System.Windows.Input;
using Avalonia;
using global::Avalonia.Media;

namespace Flowxel.UI.Controls.Ribbon;

public class RibbonMenuItem : AvaloniaObject
{
    public static readonly StyledProperty<string?> HeaderProperty =
        AvaloniaProperty.Register<RibbonMenuItem, string?>(nameof(Header));

    public static readonly StyledProperty<Geometry?> IconDataProperty =
        AvaloniaProperty.Register<RibbonMenuItem, Geometry?>(nameof(IconData));

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<RibbonMenuItem, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<RibbonMenuItem, object?>(nameof(CommandParameter));

    public string? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public Geometry? IconData
    {
        get => GetValue(IconDataProperty);
        set => SetValue(IconDataProperty, value);
    }

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }
}
