using Flowxel.Core.Geometry.Primitives;
using FlowPoint = Flowxel.Core.Geometry.Shapes.Point;
using Shape = Flowxel.Core.Geometry.Shapes.Shape;

namespace Flowxel.UI.Controls.Drawing;

internal sealed class PointToolShapeBuilder : IToolShapeBuilder
{
    public DrawingTool Tool => DrawingTool.Point;

    public Shape? Build(Vector start, Vector end, double minShapeSize)
        => new FlowPoint { Pose = ShapeMath.CreatePose(start.X, start.Y) };
}
