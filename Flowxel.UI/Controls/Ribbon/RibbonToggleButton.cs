using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using global::Avalonia.Media;

namespace Flowxel.UI.Controls.Ribbon;

public class RibbonToggleButton : TemplatedControl
{
    public RibbonToggleButton()
    {
        Focusable = true;
        IsTabStop = true;
    }

    public static readonly StyledProperty<string?> HeaderProperty =
        AvaloniaProperty.Register<RibbonToggleButton, string?>(nameof(Header));

    public static readonly StyledProperty<Geometry?> IconDataProperty =
        AvaloniaProperty.Register<RibbonToggleButton, Geometry?>(nameof(IconData));

    public static readonly StyledProperty<bool> IsCheckedProperty =
        AvaloniaProperty.Register<RibbonToggleButton, bool>(
            nameof(IsChecked),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<RibbonToggleButton, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<RibbonToggleButton, object?>(nameof(CommandParameter));

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

    public bool IsChecked
    {
        get => GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
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

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsCheckedProperty)
        {
            if (change.GetNewValue<bool>())
                PseudoClasses.Add(":checked");
            else
                PseudoClasses.Remove(":checked");
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        PseudoClasses.Add(":pressed");

        IsChecked = !IsChecked;

        if (Command is { } command && command.CanExecute(CommandParameter))
        {
            command.Execute(CommandParameter);
        }

        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        PseudoClasses.Remove(":pressed");
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        PseudoClasses.Remove(":pressed");
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key is not (Key.Enter or Key.Space))
            return;

        IsChecked = !IsChecked;

        if (Command is { } command && command.CanExecute(CommandParameter))
            command.Execute(CommandParameter);

        e.Handled = true;
    }
}
