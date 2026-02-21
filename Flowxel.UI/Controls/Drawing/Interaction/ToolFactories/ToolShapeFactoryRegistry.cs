using System.Collections.Generic;

namespace Flowxel.UI.Controls.Drawing;

internal static class ToolShapeFactoryRegistry
{
    private static readonly Dictionary<DrawingTool, IToolShapeFactory> Factories = new()
    {
        [DrawingTool.Point] = new PointToolShapeFactory(),
        [DrawingTool.Line] = new LineToolShapeFactory(),
        [DrawingTool.Rectangle] = new RectangleToolShapeFactory(),
        [DrawingTool.Circle] = new CircleToolShapeFactory(),
        [DrawingTool.Image] = new ImageToolShapeFactory(),
        [DrawingTool.TextBox] = new TextBoxToolShapeFactory(),
        [DrawingTool.Arrow] = new ArrowToolShapeFactory(),
        [DrawingTool.CenterlineRectangle] = new CenterlineRectangleToolShapeFactory(),
        [DrawingTool.Referential] = new ReferentialToolShapeFactory(),
        [DrawingTool.Dimension] = new DimensionToolShapeFactory(),
        [DrawingTool.AngleDimension] = new AngleDimensionToolShapeFactory(),
        [DrawingTool.Text] = new TextToolShapeFactory(),
        [DrawingTool.MultilineText] = new MultilineTextToolShapeFactory(),
        [DrawingTool.Icon] = new IconToolShapeFactory(),
        [DrawingTool.Arc] = new ArcToolShapeFactory(),
    };

    public static bool TryGet(DrawingTool tool, out IToolShapeFactory factory)
        => Factories.TryGetValue(tool, out factory!);
}
