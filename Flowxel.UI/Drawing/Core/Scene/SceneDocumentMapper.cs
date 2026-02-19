using Flowxel.Core.Geometry.Primitives;
using Flowxel.Core.Geometry.Shapes;
using Flowxel.UI.Drawing.Shapes;
using Shape = Flowxel.Core.Geometry.Shapes.Shape;
using FlowPoint = Flowxel.Core.Geometry.Shapes.Point;
using FlowLine = Flowxel.Core.Geometry.Shapes.Line;
using FlowRectangle = Flowxel.Core.Geometry.Shapes.Rectangle;

namespace Flowxel.UI.Drawing.Scene;

public static class SceneDocumentMapper
{
    public static SceneDocument ToDocument(IEnumerable<Shape> shapes, ISet<string> computedShapeIds)
    {
        var dtos = new List<SceneShapeDto>();
        foreach (var shape in shapes)
        {
            var dto = ToDto(shape, computedShapeIds.Contains(shape.Id));
            if (dto is not null)
                dtos.Add(dto);
        }

        return new SceneDocument
        {
            Version = SceneDocument.CurrentVersion,
            Shapes = dtos
        };
    }

    public static (IReadOnlyList<Shape> Shapes, IReadOnlyList<string> ComputedShapeIds) FromDocument(SceneDocument document)
    {
        if (document.Version != SceneDocument.CurrentVersion)
            throw new InvalidOperationException($"Unsupported scene version '{document.Version}'.");

        var shapes = new List<Shape>();
        var computed = new List<string>();

        foreach (var dto in document.Shapes)
        {
            var shape = FromDto(dto);
            if (shape is null)
                continue;

            shapes.Add(shape);
            if (dto.IsComputed)
                computed.Add(shape.Id);
        }

        return (shapes, computed);
    }

    private static SceneShapeDto? ToDto(Shape shape, bool isComputed)
    {
        return shape switch
        {
            FlowPoint point => CreateBase(point, "Point", isComputed),
            FlowLine line => CreateBase(line, "Line", isComputed) with { Length = line.Length },
            FlowRectangle rectangle => CreateBase(rectangle, "Rectangle", isComputed) with { Width = rectangle.Width, Height = rectangle.Height },
            Circle circle => CreateBase(circle, "Circle", isComputed) with { Radius = circle.Radius },
            ImageShape image => CreateBase(image, "Image", isComputed) with { Width = image.Width, Height = image.Height, SourcePath = image.SourcePath },
            TextBoxShape textBox => CreateBase(textBox, "TextBox", isComputed) with
            {
                Width = textBox.Width,
                Height = textBox.Height,
                FontSize = textBox.FontSize,
                Text = textBox.Text
            },
            ArrowShape arrow => CreateBase(arrow, "Arrow", isComputed) with
            {
                Length = arrow.Length,
                HeadLength = arrow.HeadLength,
                HeadAngleRad = arrow.HeadAngleRad
            },
            CenterlineRectangleShape centerline => CreateBase(centerline, "CenterlineRectangle", isComputed) with
            {
                Length = centerline.Length,
                Width = centerline.Width
            },
            ReferentialShape referential => CreateBase(referential, "Referential", isComputed) with
            {
                XAxisLength = referential.XAxisLength,
                YAxisLength = referential.YAxisLength
            },
            DimensionShape dimension => CreateBase(dimension, "Dimension", isComputed) with
            {
                Length = dimension.Length,
                Offset = dimension.Offset,
                Text = dimension.Text
            },
            AngleDimensionShape angle => CreateBase(angle, "AngleDimension", isComputed) with
            {
                Radius = angle.Radius,
                StartAngleRad = angle.StartAngleRad,
                SweepAngleRad = angle.SweepAngleRad,
                Text = angle.Text
            },
            TextShape text => CreateBase(text, "Text", isComputed) with
            {
                Text = text.Text,
                FontSize = text.FontSize
            },
            MultilineTextShape multilineText => CreateBase(multilineText, "MultilineText", isComputed) with
            {
                Text = multilineText.Text,
                FontSize = multilineText.FontSize,
                Width = multilineText.Width
            },
            IconShape icon => CreateBase(icon, "Icon", isComputed) with
            {
                IconKey = icon.IconKey,
                Size = icon.Size
            },
            ArcShape arc => CreateBase(arc, "Arc", isComputed) with
            {
                Radius = arc.Radius,
                StartAngleRad = arc.StartAngleRad,
                SweepAngleRad = arc.SweepAngleRad
            },
            _ => null
        };
    }

    private static Shape? FromDto(SceneShapeDto dto)
    {
        var pose = new Pose(new Vector(dto.PositionX, dto.PositionY), new Vector(dto.OrientationX, dto.OrientationY));

        Shape? shape = dto.Kind switch
        {
            "Point" => new FlowPoint { Pose = pose },
            "Line" => new FlowLine { Pose = pose, Length = dto.Length ?? 0 },
            "Rectangle" => new FlowRectangle { Pose = pose, Width = dto.Width ?? 0, Height = dto.Height ?? 0 },
            "Circle" => new Circle { Pose = pose, Radius = dto.Radius ?? 0 },
            "Image" => new ImageShape
            {
                Pose = pose,
                Width = dto.Width ?? 0,
                Height = dto.Height ?? 0,
                SourcePath = dto.SourcePath ?? string.Empty
            },
            "TextBox" => new TextBoxShape
            {
                Pose = pose,
                Width = dto.Width ?? 0,
                Height = dto.Height ?? 0,
                FontSize = dto.FontSize ?? 14,
                Text = dto.Text ?? "Text"
            },
            "Arrow" => new ArrowShape
            {
                Pose = pose,
                Length = dto.Length ?? 0,
                HeadLength = dto.HeadLength ?? 18,
                HeadAngleRad = dto.HeadAngleRad ?? (Math.PI / 7)
            },
            "CenterlineRectangle" => new CenterlineRectangleShape
            {
                Pose = pose,
                Length = dto.Length ?? 0,
                Width = dto.Width ?? 0
            },
            "Referential" => new ReferentialShape
            {
                Pose = pose,
                XAxisLength = dto.XAxisLength ?? 80,
                YAxisLength = dto.YAxisLength ?? 80
            },
            "Dimension" => new DimensionShape
            {
                Pose = pose,
                Length = dto.Length ?? 0,
                Offset = dto.Offset ?? 24,
                Text = dto.Text ?? string.Empty
            },
            "AngleDimension" => new AngleDimensionShape
            {
                Pose = pose,
                Radius = dto.Radius ?? 40,
                StartAngleRad = dto.StartAngleRad ?? 0,
                SweepAngleRad = dto.SweepAngleRad ?? (Math.PI / 2),
                Text = dto.Text ?? string.Empty
            },
            "Text" => new TextShape
            {
                Pose = pose,
                Text = dto.Text ?? "Text",
                FontSize = dto.FontSize ?? 20
            },
            "MultilineText" => new MultilineTextShape
            {
                Pose = pose,
                Text = dto.Text ?? "Line 1\nLine 2",
                FontSize = dto.FontSize ?? 16,
                Width = dto.Width ?? 240
            },
            "Icon" => new IconShape
            {
                Pose = pose,
                IconKey = dto.IconKey ?? "★",
                Size = dto.Size ?? 32
            },
            "Arc" => new ArcShape
            {
                Pose = pose,
                Radius = dto.Radius ?? 40,
                StartAngleRad = dto.StartAngleRad ?? 0,
                SweepAngleRad = dto.SweepAngleRad ?? (Math.PI / 2)
            },
            _ => null
        };

        if (shape is not null && !string.IsNullOrWhiteSpace(dto.Id))
            shape.Id = dto.Id;

        if (shape is not null)
        {
            if (dto.LineWeight is not null)
                shape.LineWeight = Math.Max(dto.LineWeight.Value, 0);

            if (dto.Fill is not null)
                shape.Fill = dto.Fill.Value;
        }

        return shape;
    }

    private static SceneShapeDto CreateBase(Shape shape, string kind, bool isComputed)
        => new()
        {
            Kind = kind,
            Id = shape.Id,
            IsComputed = isComputed,
            PositionX = shape.Pose.Position.X,
            PositionY = shape.Pose.Position.Y,
            OrientationX = shape.Pose.Orientation.X,
            OrientationY = shape.Pose.Orientation.Y,
            LineWeight = shape.LineWeight,
            Fill = shape.Fill
        };
}
