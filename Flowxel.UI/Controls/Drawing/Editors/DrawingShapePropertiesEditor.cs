using Avalonia.Controls;
using Avalonia.Layout;
using Flowxel.Core.Geometry.Primitives;
using Flowxel.Core.Geometry.Shapes;
using Flowxel.UI.Controls.Drawing.Shapes;
using Flowxel.UI.Controls.Editors;
using FlowRectangle = Flowxel.Core.Geometry.Shapes.Rectangle;
using Shape = Flowxel.Core.Geometry.Shapes.Shape;

namespace Flowxel.UI.Controls.Drawing;

public sealed class DrawingShapePropertiesEditor : ScrollViewer
{
    private const double MinShapeSize = 0.0001;
    private readonly Action _apply;

    public DrawingShapePropertiesEditor(Shape shape)
    {
        var container = new StackPanel();
        container.Spacing = 8;
        container.MinWidth = 420;

        var xEditor = AddNumberEditor(container, "Position X", shape.Pose.Position.X);
        var yEditor = AddNumberEditor(container, "Position Y", shape.Pose.Position.Y);
        var oxEditor = AddNumberEditor(container, "Orientation X", shape.Pose.Orientation.X);
        var oyEditor = AddNumberEditor(container, "Orientation Y", shape.Pose.Orientation.Y);
        var lineWeightEditor = AddNumberEditor(container, "Line weight", shape.LineWeight, 0);
        var fillEditor = AddCheckBox(container, "Fill", shape.Fill);

        var specificApply = BuildSpecificEditor(container, shape);
        _apply = () =>
        {
            var x = GetEditorValueOr(xEditor, shape.Pose.Position.X);
            var y = GetEditorValueOr(yEditor, shape.Pose.Position.Y);
            var ox = GetEditorValueOr(oxEditor, shape.Pose.Orientation.X);
            var oy = GetEditorValueOr(oyEditor, shape.Pose.Orientation.Y);

            var orientation = new Vector(ox, oy);
            if (orientation.M <= 0.000001)
                orientation = shape.Pose.Orientation;

            shape.Pose = CreatePose(x, y, orientation);
            shape.LineWeight = Math.Max(GetEditorValueOr(lineWeightEditor, shape.LineWeight), 0);
            shape.Fill = fillEditor.IsChecked == true;
            specificApply();
        };

        Content = container;
    }

    public void ApplyChanges() => _apply();

    private static Action BuildSpecificEditor(Panel container, Shape shape)
    {
        return shape switch
        {
            Line line => BuildLineEditor(container, line),
            FlowRectangle rectangle => BuildRectangleEditor(container, rectangle),
            Circle circle => BuildCircleEditor(container, circle),
            ImageShape image => BuildImageEditor(container, image),
            TextBoxShape textBox => BuildTextBoxEditor(container, textBox),
            ArrowShape arrow => BuildArrowEditor(container, arrow),
            CenterlineRectangleShape centerlineRectangle => BuildCenterlineRectangleEditor(container, centerlineRectangle),
            ReferentialShape referential => BuildReferentialEditor(container, referential),
            DimensionShape dimension => BuildDimensionEditor(container, dimension),
            AngleDimensionShape angleDimension => BuildAngleDimensionEditor(container, angleDimension),
            TextShape text => BuildTextEditor(container, text),
            MultilineTextShape multilineText => BuildMultilineTextEditor(container, multilineText),
            IconShape icon => BuildIconEditor(container, icon),
            ArcShape arc => BuildArcEditor(container, arc),
            _ => static () => { }
        };
    }

    private static Action BuildLineEditor(Panel parent, Line line)
    {
        var lengthEditor = AddNumberEditor(parent, "Length", line.Length, MinShapeSize);
        return () => line.Length = Math.Max(GetEditorValueOr(lengthEditor, line.Length), MinShapeSize);
    }

    private static Action BuildRectangleEditor(Panel parent, FlowRectangle rectangle)
    {
        var widthEditor = AddNumberEditor(parent, "Width", rectangle.Width, MinShapeSize);
        var heightEditor = AddNumberEditor(parent, "Height", rectangle.Height, MinShapeSize);
        return () =>
        {
            rectangle.Width = Math.Max(GetEditorValueOr(widthEditor, rectangle.Width), MinShapeSize);
            rectangle.Height = Math.Max(GetEditorValueOr(heightEditor, rectangle.Height), MinShapeSize);
        };
    }

    private static Action BuildCircleEditor(Panel parent, Circle circle)
    {
        var radiusEditor = AddNumberEditor(parent, "Radius", circle.Radius, MinShapeSize);
        return () => circle.Radius = Math.Max(GetEditorValueOr(radiusEditor, circle.Radius), MinShapeSize);
    }

    private static Action BuildImageEditor(Panel parent, ImageShape image)
    {
        var widthEditor = AddNumberEditor(parent, "Width", image.Width, MinShapeSize);
        var heightEditor = AddNumberEditor(parent, "Height", image.Height, MinShapeSize);
        var pathEditor = AddTextEditor(parent, "Image path", image.SourcePath);
        return () =>
        {
            image.Width = Math.Max(GetEditorValueOr(widthEditor, image.Width), MinShapeSize);
            image.Height = Math.Max(GetEditorValueOr(heightEditor, image.Height), MinShapeSize);
            image.SourcePath = pathEditor.Text?.Trim() ?? string.Empty;
        };
    }

    private static Action BuildTextBoxEditor(Panel parent, TextBoxShape textBox)
    {
        var widthEditor = AddNumberEditor(parent, "Width", textBox.Width, MinShapeSize);
        var heightEditor = AddNumberEditor(parent, "Height", textBox.Height, MinShapeSize);
        var textValueEditor = AddTextEditor(parent, "Text", textBox.Text);
        var fontSizeEditor = AddNumberEditor(parent, "Font size", textBox.FontSize, 1);
        return () =>
        {
            textBox.Width = Math.Max(GetEditorValueOr(widthEditor, textBox.Width), MinShapeSize);
            textBox.Height = Math.Max(GetEditorValueOr(heightEditor, textBox.Height), MinShapeSize);
            textBox.Text = textValueEditor.Text ?? string.Empty;
            textBox.FontSize = Math.Max(GetEditorValueOr(fontSizeEditor, textBox.FontSize), 1);
        };
    }

    private static Action BuildArrowEditor(Panel parent, ArrowShape arrow)
    {
        var lengthEditor = AddNumberEditor(parent, "Length", arrow.Length, MinShapeSize);
        var headLengthEditor = AddNumberEditor(parent, "Head length", arrow.HeadLength, MinShapeSize);
        return () =>
        {
            arrow.Length = Math.Max(GetEditorValueOr(lengthEditor, arrow.Length), MinShapeSize);
            arrow.HeadLength = Math.Max(GetEditorValueOr(headLengthEditor, arrow.HeadLength), MinShapeSize);
        };
    }

    private static Action BuildCenterlineRectangleEditor(Panel parent, CenterlineRectangleShape centerlineRectangle)
    {
        var lengthEditor = AddNumberEditor(parent, "Length", centerlineRectangle.Length, MinShapeSize);
        var widthEditor = AddNumberEditor(parent, "Width", centerlineRectangle.Width, MinShapeSize);
        return () =>
        {
            centerlineRectangle.Length = Math.Max(GetEditorValueOr(lengthEditor, centerlineRectangle.Length), MinShapeSize);
            centerlineRectangle.Width = Math.Max(GetEditorValueOr(widthEditor, centerlineRectangle.Width), MinShapeSize);
        };
    }

    private static Action BuildReferentialEditor(Panel parent, ReferentialShape referential)
    {
        var xLengthEditor = AddNumberEditor(parent, "X axis length", referential.XAxisLength, MinShapeSize);
        var yLengthEditor = AddNumberEditor(parent, "Y axis length", referential.YAxisLength, MinShapeSize);
        return () =>
        {
            referential.XAxisLength = Math.Max(GetEditorValueOr(xLengthEditor, referential.XAxisLength), MinShapeSize);
            referential.YAxisLength = Math.Max(GetEditorValueOr(yLengthEditor, referential.YAxisLength), MinShapeSize);
        };
    }

    private static Action BuildDimensionEditor(Panel parent, DimensionShape dimension)
    {
        var lengthEditor = AddNumberEditor(parent, "Length", dimension.Length, MinShapeSize);
        var offsetEditor = AddNumberEditor(parent, "Offset", dimension.Offset);
        var textEditor = AddTextEditor(parent, "Text", dimension.Text);
        return () =>
        {
            dimension.Length = Math.Max(GetEditorValueOr(lengthEditor, dimension.Length), MinShapeSize);
            dimension.Offset = GetEditorValueOr(offsetEditor, dimension.Offset);
            dimension.Text = textEditor.Text ?? string.Empty;
        };
    }

    private static Action BuildAngleDimensionEditor(Panel parent, AngleDimensionShape angleDimension)
    {
        var radiusEditor = AddNumberEditor(parent, "Radius", angleDimension.Radius, MinShapeSize);
        var startDegEditor = AddNumberEditor(parent, "Start angle (deg)", angleDimension.StartAngleRad * 180 / Math.PI);
        var sweepDegEditor = AddNumberEditor(parent, "Sweep angle (deg)", angleDimension.SweepAngleRad * 180 / Math.PI);
        var textEditor = AddTextEditor(parent, "Text", angleDimension.Text);
        return () =>
        {
            angleDimension.Radius = Math.Max(GetEditorValueOr(radiusEditor, angleDimension.Radius), MinShapeSize);
            angleDimension.StartAngleRad = GetEditorValueOr(startDegEditor, angleDimension.StartAngleRad * 180 / Math.PI) * Math.PI / 180;
            angleDimension.SweepAngleRad = GetEditorValueOr(sweepDegEditor, angleDimension.SweepAngleRad * 180 / Math.PI) * Math.PI / 180;
            angleDimension.Text = textEditor.Text ?? string.Empty;
        };
    }

    private static Action BuildTextEditor(Panel parent, TextShape text)
    {
        var textValueEditor = AddTextEditor(parent, "Text", text.Text);
        var fontSizeEditor = AddNumberEditor(parent, "Font size", text.FontSize, 1);
        return () =>
        {
            text.Text = textValueEditor.Text ?? string.Empty;
            text.FontSize = Math.Max(GetEditorValueOr(fontSizeEditor, text.FontSize), 1);
        };
    }

    private static Action BuildMultilineTextEditor(Panel parent, MultilineTextShape multilineText)
    {
        var textValueEditor = AddMultiLineTextEditor(parent, "Text", multilineText.Text);
        var fontSizeEditor = AddNumberEditor(parent, "Font size", multilineText.FontSize, 1);
        var widthEditor = AddNumberEditor(parent, "Width", multilineText.Width, MinShapeSize);
        return () =>
        {
            multilineText.Text = textValueEditor.Text ?? string.Empty;
            multilineText.FontSize = Math.Max(GetEditorValueOr(fontSizeEditor, multilineText.FontSize), 1);
            multilineText.Width = Math.Max(GetEditorValueOr(widthEditor, multilineText.Width), MinShapeSize);
        };
    }

    private static Action BuildIconEditor(Panel parent, IconShape icon)
    {
        var iconEditor = AddTextEditor(parent, "Icon glyph", icon.IconKey);
        var sizeEditor = AddNumberEditor(parent, "Size", icon.Size, MinShapeSize);
        return () =>
        {
            icon.IconKey = string.IsNullOrWhiteSpace(iconEditor.Text) ? icon.IconKey : iconEditor.Text;
            icon.Size = Math.Max(GetEditorValueOr(sizeEditor, icon.Size), MinShapeSize);
        };
    }

    private static Action BuildArcEditor(Panel parent, ArcShape arc)
    {
        var radiusEditor = AddNumberEditor(parent, "Radius", arc.Radius, MinShapeSize);
        var startDegEditor = AddNumberEditor(parent, "Start angle (deg)", arc.StartAngleRad * 180 / Math.PI);
        var sweepDegEditor = AddNumberEditor(parent, "Sweep angle (deg)", arc.SweepAngleRad * 180 / Math.PI);
        return () =>
        {
            arc.Radius = Math.Max(GetEditorValueOr(radiusEditor, arc.Radius), MinShapeSize);
            arc.StartAngleRad = GetEditorValueOr(startDegEditor, arc.StartAngleRad * 180 / Math.PI) * Math.PI / 180;
            arc.SweepAngleRad = GetEditorValueOr(sweepDegEditor, arc.SweepAngleRad * 180 / Math.PI) * Math.PI / 180;
        };
    }

    private static TextEditor AddTextEditor(Panel parent, string title, string? value, string? watermark = null)
    {
        var editor = new TextEditor
        {
            Title = title,
            Text = value,
            Watermark = watermark,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        parent.Children.Add(editor);
        return editor;
    }

    private static MultiLineTextEditor AddMultiLineTextEditor(Panel parent, string title, string? value)
    {
        var editor = new MultiLineTextEditor
        {
            Title = title,
            Text = value,
            MinHeight = 120,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        parent.Children.Add(editor);
        return editor;
    }

    private static DoubleEditor AddNumberEditor(Panel parent, string title, double value, double? minimum = null, string? unit = null)
    {
        var editor = new DoubleEditor
        {
            Title = title,
            Value = value,
            Digits = 3,
            Minimum = minimum,
            Unit = unit,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        parent.Children.Add(editor);
        return editor;
    }

    private static CheckBox AddCheckBox(Panel parent, string title, bool value)
    {
        var editor = new CheckBox
        {
            Content = title,
            IsChecked = value
        };

        parent.Children.Add(editor);
        return editor;
    }

    private static double GetEditorValueOr(DoubleEditor editor, double fallback)
        => editor.Value ?? fallback;

    private static Pose CreatePose(double x, double y, Vector? orientation = null)
        => new(new Vector(x, y), orientation ?? new Vector(1, 0));
}
