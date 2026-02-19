using System.Globalization;
using System.Text;

namespace Flowxel.UI.Controls.Drawing.Scene;

public sealed class SvgSceneExporter : ISvgSceneExporter
{
    public string Export(SceneDocument scene)
    {
        var (width, height) = ResolveCanvasSize(scene);
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{Fmt(width)}\" height=\"{Fmt(height)}\" viewBox=\"0 0 {Fmt(width)} {Fmt(height)}\">");

        if (!string.IsNullOrWhiteSpace(scene.CanvasBackgroundColor) &&
            !string.Equals(scene.CanvasBackgroundColor, "#00000000", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine($"  <rect x=\"0\" y=\"0\" width=\"{Fmt(width)}\" height=\"{Fmt(height)}\" fill=\"{Escape(scene.CanvasBackgroundColor)}\" />");
        }

        if (scene.ShowCanvasBoundary && scene.CanvasBoundaryWidth > 0 && scene.CanvasBoundaryHeight > 0)
        {
            sb.AppendLine($"  <rect x=\"0\" y=\"0\" width=\"{Fmt(scene.CanvasBoundaryWidth)}\" height=\"{Fmt(scene.CanvasBoundaryHeight)}\" fill=\"none\" stroke=\"#666\" stroke-width=\"1\" />");
        }

        foreach (var shape in scene.Shapes)
            AppendShape(sb, shape);

        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    private static (double Width, double Height) ResolveCanvasSize(SceneDocument scene)
    {
        if (scene.CanvasBoundaryWidth > 0 && scene.CanvasBoundaryHeight > 0)
            return (scene.CanvasBoundaryWidth, scene.CanvasBoundaryHeight);

        var maxX = 0d;
        var maxY = 0d;
        foreach (var s in scene.Shapes)
        {
            maxX = Math.Max(maxX, s.PositionX + 100);
            maxY = Math.Max(maxY, s.PositionY + 100);
        }

        return (Math.Max(100, maxX), Math.Max(100, maxY));
    }

    private static void AppendShape(StringBuilder sb, SceneShapeDto s)
    {
        switch (s.Kind)
        {
            case "Point":
                sb.AppendLine($"  <circle cx=\"{Fmt(s.PositionX)}\" cy=\"{Fmt(s.PositionY)}\" r=\"2\" fill=\"#00b7ff\" />");
                break;
            case "Line":
            {
                var len = s.Length ?? 0;
                var (dx, dy) = OrientationUnit(s);
                sb.AppendLine($"  <line x1=\"{Fmt(s.PositionX)}\" y1=\"{Fmt(s.PositionY)}\" x2=\"{Fmt(s.PositionX + (dx * len))}\" y2=\"{Fmt(s.PositionY + (dy * len))}\" stroke=\"#00b7ff\" stroke-width=\"{Fmt(StrokeWidth(s))}\" />");
                break;
            }
            case "Rectangle":
            case "Image":
            {
                var w = s.Width ?? 0;
                var h = s.Height ?? 0;
                var x = s.PositionX - (w * 0.5);
                var y = s.PositionY - (h * 0.5);
                if (s.Kind == "Image" && !string.IsNullOrWhiteSpace(s.SourcePath))
                {
                    sb.AppendLine($"  <image href=\"{Escape(s.SourcePath)}\" x=\"{Fmt(x)}\" y=\"{Fmt(y)}\" width=\"{Fmt(w)}\" height=\"{Fmt(h)}\" preserveAspectRatio=\"none\" />");
                    sb.AppendLine($"  <rect x=\"{Fmt(x)}\" y=\"{Fmt(y)}\" width=\"{Fmt(w)}\" height=\"{Fmt(h)}\" fill=\"{FillValue(s)}\" stroke=\"#00b7ff\" stroke-width=\"{Fmt(StrokeWidth(s))}\" />");
                }
                else
                {
                    sb.AppendLine($"  <rect x=\"{Fmt(x)}\" y=\"{Fmt(y)}\" width=\"{Fmt(w)}\" height=\"{Fmt(h)}\" fill=\"{FillValue(s)}\" stroke=\"#00b7ff\" stroke-width=\"{Fmt(StrokeWidth(s))}\" />");
                }
                break;
            }
            case "Circle":
                sb.AppendLine($"  <circle cx=\"{Fmt(s.PositionX)}\" cy=\"{Fmt(s.PositionY)}\" r=\"{Fmt(s.Radius ?? 0)}\" fill=\"{FillValue(s)}\" stroke=\"#00b7ff\" stroke-width=\"{Fmt(StrokeWidth(s))}\" />");
                break;
            case "Text":
                sb.AppendLine($"  <text x=\"{Fmt(s.PositionX)}\" y=\"{Fmt(s.PositionY)}\" font-size=\"{Fmt(s.FontSize ?? 20)}\" fill=\"#00b7ff\">{EscapeText(s.Text ?? string.Empty)}</text>");
                break;
            case "MultilineText":
            {
                var lines = (s.Text ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
                var lineHeight = (s.FontSize ?? 16) * 1.2;
                sb.AppendLine($"  <text x=\"{Fmt(s.PositionX)}\" y=\"{Fmt(s.PositionY)}\" font-size=\"{Fmt(s.FontSize ?? 16)}\" fill=\"#00b7ff\">");
                for (var i = 0; i < lines.Length; i++)
                {
                    var dy = i == 0 ? 0 : lineHeight;
                    sb.AppendLine($"    <tspan x=\"{Fmt(s.PositionX)}\" dy=\"{Fmt(dy)}\">{EscapeText(lines[i])}</tspan>");
                }

                sb.AppendLine("  </text>");
                break;
            }
            case "Icon":
                sb.AppendLine($"  <text x=\"{Fmt(s.PositionX)}\" y=\"{Fmt(s.PositionY)}\" font-size=\"{Fmt(s.Size ?? 32)}\" fill=\"#00b7ff\">{EscapeText(s.IconKey ?? "★")}</text>");
                break;
            case "Arc":
            {
                var path = BuildArcPath(s.PositionX, s.PositionY, s.Radius ?? 0, s.StartAngleRad ?? 0, s.SweepAngleRad ?? 0, s.OrientationX, s.OrientationY);
                sb.AppendLine($"  <path d=\"{path}\" fill=\"none\" stroke=\"#00b7ff\" stroke-width=\"{Fmt(StrokeWidth(s))}\" />");
                break;
            }
            case "AngleDimension":
            {
                var path = BuildArcPath(s.PositionX, s.PositionY, s.Radius ?? 0, s.StartAngleRad ?? 0, s.SweepAngleRad ?? 0, s.OrientationX, s.OrientationY);
                sb.AppendLine($"  <path d=\"{path}\" fill=\"none\" stroke=\"#00b7ff\" stroke-width=\"{Fmt(StrokeWidth(s))}\" />");
                break;
            }
            case "Arrow":
            case "CenterlineRectangle":
            case "Referential":
            case "Dimension":
            default:
                // Export of advanced annotation shapes will be added incrementally.
                break;
        }
    }

    private static (double X, double Y) OrientationUnit(SceneShapeDto s)
    {
        var ox = s.OrientationX;
        var oy = s.OrientationY;
        var norm = Math.Sqrt((ox * ox) + (oy * oy));
        if (norm <= 1e-9)
            return (1, 0);

        return (ox / norm, oy / norm);
    }

    private static string BuildArcPath(double cx, double cy, double radius, double startAngle, double sweepAngle, double ox, double oy)
    {
        var (ux, uy) = OrientationUnit(new SceneShapeDto { OrientationX = ox, OrientationY = oy });

        static (double X, double Y) Rot(double x, double y, double a)
        {
            var ca = Math.Cos(a);
            var sa = Math.Sin(a);
            return ((x * ca) - (y * sa), (x * sa) + (y * ca));
        }

        var startDir = Rot(ux, uy, startAngle);
        var endDir = Rot(ux, uy, startAngle + sweepAngle);

        var sx = cx + (startDir.X * radius);
        var sy = cy + (startDir.Y * radius);
        var ex = cx + (endDir.X * radius);
        var ey = cy + (endDir.Y * radius);

        var largeArcFlag = Math.Abs(sweepAngle) > Math.PI ? 1 : 0;
        var sweepFlag = sweepAngle >= 0 ? 1 : 0;

        return $"M {Fmt(sx)} {Fmt(sy)} A {Fmt(radius)} {Fmt(radius)} 0 {largeArcFlag} {sweepFlag} {Fmt(ex)} {Fmt(ey)}";
    }

    private static string Fmt(double value)
        => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static double StrokeWidth(SceneShapeDto shape)
        => Math.Max(0.1, shape.LineWeight ?? 1);

    private static string FillValue(SceneShapeDto shape)
        => shape.Fill == true ? "#00b7ff" : "none";

    private static string Escape(string value)
        => value.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);

    private static string EscapeText(string value)
        => value.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);
}
