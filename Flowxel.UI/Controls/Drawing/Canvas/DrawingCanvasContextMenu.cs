using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Flowxel.UI.Controls.Drawing;

public sealed class DrawingCanvasContextMenu : ContextMenu
{
    private readonly MenuItem _canvasSettingsItem;
    private readonly MenuItem _deleteShapeItem;
    private readonly MenuItem _centerViewItem;
    private readonly MenuItem _propertiesItem;

    protected override Type StyleKeyOverride => typeof(ContextMenu);

    public DrawingCanvasContextMenu()
    {
        _canvasSettingsItem = new MenuItem { Header = "Canvas settings..." };
        _deleteShapeItem = new MenuItem { Header = "Delete shape" };
        _centerViewItem = new MenuItem { Header = "Center view" };
        _propertiesItem = new MenuItem { Header = "Properties..." };

        _canvasSettingsItem.Click += (_, _) => CanvasSettingsRequested?.Invoke(this, EventArgs.Empty);
        _deleteShapeItem.Click += (_, _) => DeleteShapeRequested?.Invoke(this, EventArgs.Empty);
        _centerViewItem.Click += (_, _) => CenterViewRequested?.Invoke(this, EventArgs.Empty);
        _propertiesItem.Click += (_, _) => PropertiesRequested?.Invoke(this, EventArgs.Empty);

        ItemsSource = new object[] { _canvasSettingsItem, _propertiesItem, _deleteShapeItem, _centerViewItem };
        Placement = PlacementMode.Pointer;
    }

    public event EventHandler? CanvasSettingsRequested;
    public event EventHandler? DeleteShapeRequested;
    public event EventHandler? CenterViewRequested;
    public event EventHandler? PropertiesRequested;

    public void ConfigureForShape()
    {
        _canvasSettingsItem.IsVisible = false;
        _propertiesItem.IsVisible = true;
        _deleteShapeItem.IsVisible = true;
        _centerViewItem.IsVisible = false;
    }

    public void ConfigureForComputedShape()
    {
        _canvasSettingsItem.IsVisible = false;
        _propertiesItem.IsVisible = false;
        _deleteShapeItem.IsVisible = false;
        _centerViewItem.IsVisible = false;
    }

    public void ConfigureForCanvas()
    {
        _canvasSettingsItem.IsVisible = true;
        _propertiesItem.IsVisible = false;
        _deleteShapeItem.IsVisible = false;
        _centerViewItem.IsVisible = true;
    }
}
