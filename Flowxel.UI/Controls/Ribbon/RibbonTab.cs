using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls.Primitives;
using Avalonia.Metadata;

namespace Flowxel.UI.Controls.Ribbon;

public class RibbonTab : TemplatedControl
{
    public static readonly StyledProperty<string?> HeaderProperty =
        AvaloniaProperty.Register<RibbonTab, string?>(nameof(Header));

    [Content]
    public AvaloniaList<RibbonGroup> Groups { get; } = new();

    public string? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }
}
