using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Flowxel.UI.Controls.Docking;

/// <summary>
/// Base class for docking layout model nodes
/// </summary>
public abstract class DockLayoutNode
{
}

/// <summary>
/// Model representing a single dock pane
/// </summary>
public class DockPaneModel : DockLayoutNode
{
    public string Header { get; set; } = string.Empty;
    public object? Content { get; set; }
    public bool CanClose { get; set; } = true;
    public bool CanMove { get; set; } = true;
    public DockPaneSizeMode HorizontalSizeMode { get; set; } = DockPaneSizeMode.Stretch;
    public DockPaneSizeMode VerticalSizeMode { get; set; } = DockPaneSizeMode.Stretch;
    public double PreferredWidth { get; set; } = double.NaN;
    public double PreferredHeight { get; set; } = double.NaN;
}

/// <summary>
/// Model representing a group of tabbed panes
/// </summary>
public class DockTabGroupModel : DockLayoutNode
{
    public AvaloniaList<DockPaneModel> Panes { get; } = new();
    public DockPaneModel? SelectedPane { get; set; }
}

/// <summary>
/// Model representing a split container with two children
/// </summary>
public class DockSplitModel : DockLayoutNode
{
    public Orientation Orientation { get; set; } = Orientation.Horizontal;
    public DockLayoutNode? First { get; set; }
    public DockLayoutNode? Second { get; set; }
    public GridLength FirstSize { get; set; } = new GridLength(1, GridUnitType.Star);
    public GridLength SecondSize { get; set; } = new GridLength(1, GridUnitType.Star);
    public bool FirstResizable { get; set; } = true;
    public bool SecondResizable { get; set; } = true;
}
