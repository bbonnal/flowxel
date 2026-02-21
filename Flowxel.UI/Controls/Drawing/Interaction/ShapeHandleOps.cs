using Flowxel.Core.Geometry.Primitives;
using Flowxel.Core.Geometry.Shapes;
using Flowxel.UI.Controls.Drawing.Shapes;
using FlowRectangle = Flowxel.Core.Geometry.Shapes.Rectangle;
using Line = Flowxel.Core.Geometry.Shapes.Line;

namespace Flowxel.UI.Controls.Drawing;

internal static class ShapeHandleOps
{
    public static void SetLineFromEndpoints(Line line, Vector start, Vector end, double minShapeSize)
    {
        var delta = end - start;
        if (delta.M <= minShapeSize)
            return;

        line.Pose = ShapeMath.CreatePose(start.X, start.Y, delta.Normalize());
        line.Length = delta.M;
    }

    public static void SetCenterlineRectangleFromEndpoints(CenterlineRectangleShape shape, Vector start, Vector end, double minShapeSize)
    {
        var delta = end - start;
        if (delta.M <= minShapeSize)
            return;

        shape.Pose = ShapeMath.CreatePose(start.X, start.Y, delta.Normalize());
        shape.Length = delta.M;
    }

    public static void ResizeRectangle(FlowRectangle rectangle, Vector movingCorner, Vector fixedCorner, double minShapeSize)
    {
        var width = Math.Abs(movingCorner.X - fixedCorner.X);
        var height = Math.Abs(movingCorner.Y - fixedCorner.Y);
        if (width <= minShapeSize || height <= minShapeSize)
            return;

        rectangle.Pose = ShapeMath.CreatePose(
            (movingCorner.X + fixedCorner.X) * 0.5,
            (movingCorner.Y + fixedCorner.Y) * 0.5);
        rectangle.Width = width;
        rectangle.Height = height;
    }

    public static void ApplyCircleHandleDrag(Circle circle, ShapeHandleKind handle, Vector world, Vector? lastWorld, double minShapeSize)
    {
        switch (handle)
        {
            case ShapeHandleKind.Move when lastWorld is not null:
                circle.Translate(world - lastWorld.Value);
                return;
            case ShapeHandleKind.CircleRadius:
                circle.Radius = Math.Max(ShapeMath.Distance(circle.Pose.Position, world), minShapeSize);
                return;
        }
    }

    public static void ApplyImageHandleDrag(ImageShape image, ShapeHandleKind handle, Vector world, Vector? lastWorld, double minShapeSize)
    {
        if (handle == ShapeHandleKind.Move && lastWorld is not null)
        {
            image.Translate(world - lastWorld.Value);
            return;
        }

        switch (handle)
        {
            case ShapeHandleKind.RectTopLeft:
                ResizeAxisAlignedBox(world, image.BottomRight, minShapeSize, (center, width, height) =>
                {
                    image.Pose = ShapeMath.CreatePose(center.X, center.Y);
                    image.Width = width;
                    image.Height = height;
                });
                return;
            case ShapeHandleKind.RectTopRight:
                ResizeAxisAlignedBox(world, image.BottomLeft, minShapeSize, (center, width, height) =>
                {
                    image.Pose = ShapeMath.CreatePose(center.X, center.Y);
                    image.Width = width;
                    image.Height = height;
                });
                return;
            case ShapeHandleKind.RectBottomRight:
                ResizeAxisAlignedBox(world, image.TopLeft, minShapeSize, (center, width, height) =>
                {
                    image.Pose = ShapeMath.CreatePose(center.X, center.Y);
                    image.Width = width;
                    image.Height = height;
                });
                return;
            case ShapeHandleKind.RectBottomLeft:
                ResizeAxisAlignedBox(world, image.TopRight, minShapeSize, (center, width, height) =>
                {
                    image.Pose = ShapeMath.CreatePose(center.X, center.Y);
                    image.Width = width;
                    image.Height = height;
                });
                return;
        }
    }

    public static void ApplyTextBoxHandleDrag(TextBoxShape textBox, ShapeHandleKind handle, Vector world, Vector? lastWorld, double minShapeSize)
    {
        if (handle == ShapeHandleKind.Move && lastWorld is not null)
        {
            textBox.Translate(world - lastWorld.Value);
            return;
        }

        switch (handle)
        {
            case ShapeHandleKind.RectTopLeft:
                ResizeAxisAlignedBox(world, textBox.BottomRight, minShapeSize, (center, width, height) =>
                {
                    textBox.Pose = ShapeMath.CreatePose(center.X, center.Y);
                    textBox.Width = width;
                    textBox.Height = height;
                });
                return;
            case ShapeHandleKind.RectTopRight:
                ResizeAxisAlignedBox(world, textBox.BottomLeft, minShapeSize, (center, width, height) =>
                {
                    textBox.Pose = ShapeMath.CreatePose(center.X, center.Y);
                    textBox.Width = width;
                    textBox.Height = height;
                });
                return;
            case ShapeHandleKind.RectBottomRight:
                ResizeAxisAlignedBox(world, textBox.TopLeft, minShapeSize, (center, width, height) =>
                {
                    textBox.Pose = ShapeMath.CreatePose(center.X, center.Y);
                    textBox.Width = width;
                    textBox.Height = height;
                });
                return;
            case ShapeHandleKind.RectBottomLeft:
                ResizeAxisAlignedBox(world, textBox.TopRight, minShapeSize, (center, width, height) =>
                {
                    textBox.Pose = ShapeMath.CreatePose(center.X, center.Y);
                    textBox.Width = width;
                    textBox.Height = height;
                });
                return;
        }
    }

    public static void ApplyArrowHandleDrag(ArrowShape arrow, ShapeHandleKind handle, Vector world, Vector? lastWorld, double minShapeSize)
    {
        var start = arrow.StartPoint;
        var end = arrow.EndPoint;

        switch (handle)
        {
            case ShapeHandleKind.Move when lastWorld is not null:
                arrow.Translate(world - lastWorld.Value);
                return;
            case ShapeHandleKind.LineStart:
                SetArrowFromEndpoints(arrow, world, end, minShapeSize);
                return;
            case ShapeHandleKind.LineEnd:
                SetArrowFromEndpoints(arrow, start, world, minShapeSize);
                return;
        }
    }

    public static void ApplyReferentialDrag(ReferentialShape shape, ShapeHandleKind handle, Vector world, Vector? lastWorld, double minShapeSize)
    {
        switch (handle)
        {
            case ShapeHandleKind.Move when lastWorld is not null:
                shape.Translate(world - lastWorld.Value);
                return;
            case ShapeHandleKind.ReferentialXAxis:
            {
                var xDir = shape.Pose.Orientation.Normalize();
                var projection = Math.Abs(ShapeMath.Dot(world - shape.Origin, xDir));
                shape.XAxisLength = Math.Max(projection, minShapeSize);
                return;
            }
            case ShapeHandleKind.ReferentialYAxis:
            {
                var yDir = new Vector(-shape.Pose.Orientation.Y, shape.Pose.Orientation.X).Normalize();
                var projection = Math.Abs(ShapeMath.Dot(world - shape.Origin, yDir));
                shape.YAxisLength = Math.Max(projection, minShapeSize);
                return;
            }
        }
    }

    public static void ApplyDimensionDrag(DimensionShape shape, ShapeHandleKind handle, Vector world, Vector? lastWorld, double minShapeSize)
    {
        switch (handle)
        {
            case ShapeHandleKind.Move when lastWorld is not null:
                shape.Translate(world - lastWorld.Value);
                return;
            case ShapeHandleKind.LineStart:
                SetDimensionFromEndpoints(shape, world, shape.EndPoint, minShapeSize);
                shape.Text = shape.Length.ToString("0.##");
                return;
            case ShapeHandleKind.LineEnd:
                SetDimensionFromEndpoints(shape, shape.StartPoint, world, minShapeSize);
                shape.Text = shape.Length.ToString("0.##");
                return;
            case ShapeHandleKind.DimensionOffset:
            {
                var mid = ShapeMath.Midpoint(shape.StartPoint, shape.EndPoint);
                var signedOffset = ShapeMath.Dot(world - mid, shape.Normal);
                shape.Offset = signedOffset;
                return;
            }
        }
    }

    public static void ApplyAngleDimensionDrag(AngleDimensionShape shape, ShapeHandleKind handle, Vector world, Vector? lastWorld, double minShapeSize)
    {
        switch (handle)
        {
            case ShapeHandleKind.Move when lastWorld is not null:
                shape.Translate(world - lastWorld.Value);
                return;
            case ShapeHandleKind.AngleDimensionStart:
            {
                var end = shape.EndAngleRad;
                var newStart = ShapeMath.GetLocalAngle(shape.Pose, world);
                shape.StartAngleRad = newStart;
                shape.SweepAngleRad = ShapeMath.ClampSweep(end - newStart);
                shape.Text = $"{Math.Abs(shape.SweepAngleRad * 180 / Math.PI):0.#}°";
                return;
            }
            case ShapeHandleKind.AngleDimensionEnd:
            {
                var end = ShapeMath.GetLocalAngle(shape.Pose, world);
                shape.SweepAngleRad = ShapeMath.ClampSweep(end - shape.StartAngleRad);
                shape.Text = $"{Math.Abs(shape.SweepAngleRad * 180 / Math.PI):0.#}°";
                return;
            }
            case ShapeHandleKind.AngleDimensionRadius:
                shape.Radius = Math.Max(ShapeMath.Distance(shape.Center, world), minShapeSize);
                return;
        }
    }

    public static void ApplyIconDrag(IconShape shape, ShapeHandleKind handle, Vector world, Vector? lastWorld, double minShapeSize)
    {
        switch (handle)
        {
            case ShapeHandleKind.Move when lastWorld is not null:
                shape.Translate(world - lastWorld.Value);
                return;
            case ShapeHandleKind.CircleRadius:
                shape.Size = Math.Max(ShapeMath.Distance(shape.Pose.Position, world) * 2, minShapeSize);
                return;
        }
    }

    public static void ApplyArcDrag(ArcShape shape, ShapeHandleKind handle, Vector world, Vector? lastWorld, double minShapeSize)
    {
        switch (handle)
        {
            case ShapeHandleKind.Move when lastWorld is not null:
                shape.Translate(world - lastWorld.Value);
                return;
            case ShapeHandleKind.AngleDimensionStart:
            {
                var end = shape.EndAngleRad;
                var newStart = ShapeMath.GetLocalAngle(shape.Pose, world);
                shape.StartAngleRad = newStart;
                shape.SweepAngleRad = ShapeMath.ClampSweep(end - newStart);
                return;
            }
            case ShapeHandleKind.AngleDimensionEnd:
            {
                var end = ShapeMath.GetLocalAngle(shape.Pose, world);
                shape.SweepAngleRad = ShapeMath.ClampSweep(end - shape.StartAngleRad);
                return;
            }
            case ShapeHandleKind.AngleDimensionRadius:
                shape.Radius = Math.Max(ShapeMath.Distance(shape.Center, world), minShapeSize);
                return;
        }
    }

    private static void SetArrowFromEndpoints(ArrowShape arrow, Vector start, Vector end, double minShapeSize)
    {
        var delta = end - start;
        if (delta.M <= minShapeSize)
            return;

        arrow.Pose = ShapeMath.CreatePose(start.X, start.Y, delta.Normalize());
        arrow.Length = delta.M;
        arrow.HeadLength = Math.Max(12, arrow.Length * 0.15);
    }

    private static void SetDimensionFromEndpoints(DimensionShape shape, Vector start, Vector end, double minShapeSize)
    {
        var delta = end - start;
        if (delta.M <= minShapeSize)
            return;

        shape.Pose = ShapeMath.CreatePose(start.X, start.Y, delta.Normalize());
        shape.Length = delta.M;
    }

    private static void ResizeAxisAlignedBox(Vector movingCorner, Vector fixedCorner, double minShapeSize, Action<Vector, double, double> apply)
    {
        if (!ShapeMath.TryBuildAxisAlignedBox(movingCorner, fixedCorner, minShapeSize, out var center, out var width, out var height))
            return;

        apply(center, width, height);
    }
}
