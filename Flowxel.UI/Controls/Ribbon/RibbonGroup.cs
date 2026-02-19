using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Metadata;

namespace Flowxel.UI.Controls.Ribbon;

public class RibbonGroup : TemplatedControl
{
    public static readonly StyledProperty<string?> HeaderProperty =
        AvaloniaProperty.Register<RibbonGroup, string?>(nameof(Header));

    [Content]
    public AvaloniaList<Control> Items { get; } = new();

    public string? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }
}
