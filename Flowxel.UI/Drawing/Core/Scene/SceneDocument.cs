namespace Flowxel.UI.Drawing.Scene;

public sealed class SceneDocument
{
    public const int CurrentVersion = 1;

    public int Version { get; init; } = CurrentVersion;

    public string CanvasBackgroundColor { get; init; } = "#00000000";

    public bool ShowCanvasBoundary { get; init; }

    public double CanvasBoundaryWidth { get; init; }

    public double CanvasBoundaryHeight { get; init; }

    public List<SceneShapeDto> Shapes { get; init; } = [];
}

public sealed record class SceneShapeDto
{
    public string Kind { get; init; } = string.Empty;

    public string Id { get; init; } = string.Empty;

    public bool IsComputed { get; init; }

    public double PositionX { get; init; }

    public double PositionY { get; init; }

    public double OrientationX { get; init; } = 1;

    public double OrientationY { get; init; }

    public double? Length { get; init; }

    public double? Width { get; init; }

    public double? Height { get; init; }

    public double? Radius { get; init; }

    public double? Offset { get; init; }

    public double? FontSize { get; init; }

    public double? XAxisLength { get; init; }

    public double? YAxisLength { get; init; }

    public double? HeadLength { get; init; }

    public double? HeadAngleRad { get; init; }

    public double? StartAngleRad { get; init; }

    public double? SweepAngleRad { get; init; }

    public double? LineWeight { get; init; }

    public bool? Fill { get; init; }

    public string? Text { get; init; }

    public string? SourcePath { get; init; }

    public string? IconKey { get; init; }

    public double? Size { get; init; }
}
