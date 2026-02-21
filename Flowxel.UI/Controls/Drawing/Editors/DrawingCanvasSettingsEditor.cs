using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Flowxel.UI.Controls.Editors;

namespace Flowxel.UI.Controls.Drawing;

public sealed class DrawingCanvasSettingsEditor : StackPanel
{
    private readonly TextEditor _colorEditor;
    private readonly CheckBox _showBoundaryCheck;
    private readonly DoubleEditor _widthEditor;
    private readonly DoubleEditor _heightEditor;

    public DrawingCanvasSettingsEditor(DrawingCanvasControl canvas)
    {
        _colorEditor = new TextEditor
        {
            Title = "Background color",
            Text = (canvas.CanvasBackground as ISolidColorBrush)?.Color.ToString() ?? "#00000000",
            Watermark = "#RRGGBB or #AARRGGBB",
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        _showBoundaryCheck = new CheckBox
        {
            IsChecked = canvas.ShowCanvasBoundary,
            Content = "Show boundary"
        };

        _widthEditor = new DoubleEditor
        {
            Title = "Boundary width",
            Value = canvas.CanvasBoundaryWidth,
            Digits = 3,
            Minimum = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        _heightEditor = new DoubleEditor
        {
            Title = "Boundary height",
            Value = canvas.CanvasBoundaryHeight,
            Digits = 3,
            Minimum = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        Children.Add(_colorEditor);
        Children.Add(_showBoundaryCheck);
        Children.Add(_widthEditor);
        Children.Add(_heightEditor);
    }

    public void ApplyTo(DrawingCanvasControl canvas)
    {
        canvas.CanvasBackground = TryParseBrushFromText(_colorEditor.Text, canvas.CanvasBackground);
        canvas.ShowCanvasBoundary = _showBoundaryCheck.IsChecked == true;
        canvas.CanvasBoundaryWidth = _widthEditor.Value ?? canvas.CanvasBoundaryWidth;
        canvas.CanvasBoundaryHeight = _heightEditor.Value ?? canvas.CanvasBoundaryHeight;
    }

    private static IBrush TryParseBrushFromText(string? text, IBrush fallback)
    {
        if (string.IsNullOrWhiteSpace(text))
            return fallback;

        if (Color.TryParse(text.Trim(), out var color))
            return new SolidColorBrush(color);

        return fallback;
    }
}
