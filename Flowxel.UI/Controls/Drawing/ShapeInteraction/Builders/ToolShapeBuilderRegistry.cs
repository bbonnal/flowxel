using System.Collections.Generic;

namespace Flowxel.UI.Controls.Drawing;

internal static class ToolShapeBuilderRegistry
{
    private static readonly Dictionary<DrawingTool, IToolShapeBuilder> Factories = new()
    {
        [DrawingTool.Point] = new PointToolShapeBuilder(),
        [DrawingTool.Line] = new LineToolShapeBuilder(),
        [DrawingTool.Rectangle] = new RectangleToolShapeBuilder(),
        [DrawingTool.Circle] = new CircleToolShapeBuilder(),
        [DrawingTool.Image] = new ImageToolShapeBuilder(),
        [DrawingTool.TextBox] = new TextBoxToolShapeBuilder(),
        [DrawingTool.Arrow] = new ArrowToolShapeBuilder(),
        [DrawingTool.CenterlineRectangle] = new CenterlineRectangleToolShapeBuilder(),
        [DrawingTool.Referential] = new ReferentialToolShapeBuilder(),
        [DrawingTool.Dimension] = new DimensionToolShapeBuilder(),
        [DrawingTool.AngleDimension] = new AngleDimensionToolShapeBuilder(),
        [DrawingTool.Text] = new TextToolShapeBuilder(),
        [DrawingTool.MultilineText] = new MultilineTextToolShapeBuilder(),
        [DrawingTool.Icon] = new IconToolShapeBuilder(),
        [DrawingTool.Arc] = new ArcToolShapeBuilder(),
    };

    public static bool TryGet(DrawingTool tool, out IToolShapeBuilder factory)
        => Factories.TryGetValue(tool, out factory!);
}
