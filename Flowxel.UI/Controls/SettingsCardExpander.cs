using Avalonia.Controls.Primitives;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia;

namespace Flowxel.UI.Controls;

public class SettingsCardExpander : TemplatedControl
{
    private Border? _headerBorder;

    public SettingsCardExpander()
    {
        Focusable = true;
        IsTabStop = true;
    }

    public static readonly StyledProperty<string?> HeaderProperty =
        AvaloniaProperty.Register<SettingsCardExpander, string?>(nameof(Header));

    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<SettingsCardExpander, string?>(nameof(Description));

    public static readonly StyledProperty<Geometry?> IconDataProperty =
        AvaloniaProperty.Register<SettingsCardExpander, Geometry?>(nameof(IconData));

    public static readonly StyledProperty<object?> ContentProperty =
        AvaloniaProperty.Register<SettingsCardExpander, object?>(nameof(Content));

    public static readonly StyledProperty<bool> IsExpandedProperty =
        AvaloniaProperty.Register<SettingsCardExpander, bool>(nameof(IsExpanded));

    public string? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public Geometry? IconData
    {
        get => GetValue(IconDataProperty);
        set => SetValue(IconDataProperty, value);
    }

    [Content]
    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public bool IsExpanded
    {
        get => GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsExpandedProperty)
        {
            if (change.GetNewValue<bool>())
                PseudoClasses.Add(":expanded");
            else
                PseudoClasses.Remove(":expanded");
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_headerBorder is not null)
        {
            _headerBorder.PointerPressed -= OnHeaderPointerPressed;
            _headerBorder.PointerReleased -= OnHeaderPointerReleased;
            _headerBorder.PointerCaptureLost -= OnHeaderPointerCaptureLost;
        }

        _headerBorder = e.NameScope.Find<Border>("PART_Header");

        if (_headerBorder is not null)
        {
            _headerBorder.PointerPressed += OnHeaderPointerPressed;
            _headerBorder.PointerReleased += OnHeaderPointerReleased;
            _headerBorder.PointerCaptureLost += OnHeaderPointerCaptureLost;
        }
    }

    private void OnHeaderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        PseudoClasses.Add(":pressed");
        IsExpanded = !IsExpanded;
        e.Handled = true;
    }

    private void OnHeaderPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        PseudoClasses.Remove(":pressed");
    }

    private void OnHeaderPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        PseudoClasses.Remove(":pressed");
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key is not (Key.Enter or Key.Space))
            return;

        IsExpanded = !IsExpanded;
        e.Handled = true;
    }
}
