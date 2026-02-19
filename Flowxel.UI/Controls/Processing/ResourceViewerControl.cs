using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Flowxel.UI.Controls.Processing;

public sealed class ResourceViewerControl : ContentControl
{
    public static readonly StyledProperty<ResourceEntryDescriptor?> ResourceProperty =
        AvaloniaProperty.Register<ResourceViewerControl, ResourceEntryDescriptor?>(nameof(Resource));

    public ResourceEntryDescriptor? Resource
    {
        get => GetValue(ResourceProperty);
        set => SetValue(ResourceProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property != ResourceProperty)
            return;

        Content = BuildContent(change.GetNewValue<ResourceEntryDescriptor?>());
    }

    private static Control BuildContent(ResourceEntryDescriptor? resource)
    {
        if (resource?.Value is null)
        {
            return new TextBlock
            {
                Text = "No resource selected.",
                Foreground = Brushes.Gray
            };
        }

        return resource.Value switch
        {
            ImageResourceViewData image => new ImageResourceViewerControl { Source = image.Bitmap, SourcePath = image.Path },
            NumericArrayResourceViewData values => new NumericArrayResourceViewerControl { Values = values.Values },
            ShapePropertyResourceViewData shape => new ShapePropertyResourceViewerControl { ShapeType = shape.ShapeType, Properties = shape.Properties },
            GraphSeriesResourceViewData series => new GraphSeriesResourceViewerControl { Values = series.Values },
            LineCoordinatesResourceViewData lines => new LineCoordinatesResourceViewerControl { Lines = lines.Lines },
            _ => new TextBlock { Text = resource.Preview }
        };
    }
}

public sealed class ImageResourceViewerControl : ContentControl
{
    public static readonly StyledProperty<Bitmap?> SourceProperty =
        AvaloniaProperty.Register<ImageResourceViewerControl, Bitmap?>(nameof(Source));

    public static readonly StyledProperty<string?> SourcePathProperty =
        AvaloniaProperty.Register<ImageResourceViewerControl, string?>(nameof(SourcePath));

    public Bitmap? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public string? SourcePath
    {
        get => GetValue(SourcePathProperty);
        set => SetValue(SourcePathProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SourceProperty || change.Property == SourcePathProperty)
            Content = Build();
    }

    private Control Build()
    {
        return new StackPanel
        {
            Spacing = 8,
            Children =
            {
                new TextBlock { Text = SourcePath ?? string.Empty, TextWrapping = TextWrapping.Wrap },
                new Border
                {
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(1),
                    Child = new Image
                    {
                        Source = Source,
                        Stretch = Stretch.Uniform,
                        Height = 420
                    }
                }
            }
        };
    }
}

public sealed class NumericArrayResourceViewerControl : ContentControl
{
    public static readonly StyledProperty<IReadOnlyList<double>> ValuesProperty =
        AvaloniaProperty.Register<NumericArrayResourceViewerControl, IReadOnlyList<double>>(nameof(Values), []);

    public IReadOnlyList<double> Values
    {
        get => GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValuesProperty)
            Content = Build();
    }

    private Control Build()
    {
        var list = new ListBox
        {
            ItemsSource = Values.Select((v, i) => $"[{i}] {v:0.###}").ToArray(),
            Height = 420
        };

        return list;
    }
}

public sealed class ShapePropertyResourceViewerControl : ContentControl
{
    public static readonly StyledProperty<string> ShapeTypeProperty =
        AvaloniaProperty.Register<ShapePropertyResourceViewerControl, string>(nameof(ShapeType), string.Empty);

    public static readonly StyledProperty<IReadOnlyList<KeyValuePair<string, string>>> PropertiesProperty =
        AvaloniaProperty.Register<ShapePropertyResourceViewerControl, IReadOnlyList<KeyValuePair<string, string>>>(nameof(Properties), []);

    public string ShapeType
    {
        get => GetValue(ShapeTypeProperty);
        set => SetValue(ShapeTypeProperty, value);
    }

    public IReadOnlyList<KeyValuePair<string, string>> Properties
    {
        get => GetValue(PropertiesProperty);
        set => SetValue(PropertiesProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ShapeTypeProperty || change.Property == PropertiesProperty)
            Content = Build();
    }

    private Control Build()
    {
        var panel = new StackPanel { Spacing = 6 };
        panel.Children.Add(new TextBlock { Text = ShapeType, FontWeight = FontWeight.Bold, FontSize = 16 });

        foreach (var kv in Properties)
        {
            panel.Children.Add(new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("160,*"),
                Children =
                {
                    new TextBlock { Text = kv.Key, Foreground = Brushes.Gray },
                    new TextBlock { Text = kv.Value, Margin = new Thickness(8,0,0,0), [Grid.ColumnProperty] = 1 }
                }
            });
        }

        return new ScrollViewer { Content = panel, Height = 420 };
    }
}

public sealed class GraphSeriesResourceViewerControl : Control
{
    public static readonly StyledProperty<IReadOnlyList<double>> ValuesProperty =
        AvaloniaProperty.Register<GraphSeriesResourceViewerControl, IReadOnlyList<double>>(nameof(Values), []);

    public IReadOnlyList<double> Values
    {
        get => GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        context.FillRectangle(Brushes.Black, new Rect(Bounds.Size));
        if (Values.Count < 2)
            return;

        var min = Values.Min();
        var max = Values.Max();
        var range = Math.Max(max - min, 0.000001);

        var pen = new Pen(Brushes.LimeGreen, 1.5);
        var stepX = Bounds.Width / (Values.Count - 1);

        Point? previous = null;
        for (var i = 0; i < Values.Count; i++)
        {
            var x = i * stepX;
            var yNorm = (Values[i] - min) / range;
            var y = Bounds.Height - (yNorm * Bounds.Height);
            var current = new Point(x, y);
            if (previous is not null)
                context.DrawLine(pen, previous.Value, current);

            previous = current;
        }
    }
}

public sealed class LineCoordinatesResourceViewerControl : ContentControl
{
    public static readonly StyledProperty<IReadOnlyList<LineCoordinateEntry>> LinesProperty =
        AvaloniaProperty.Register<LineCoordinatesResourceViewerControl, IReadOnlyList<LineCoordinateEntry>>(nameof(Lines), []);

    public IReadOnlyList<LineCoordinateEntry> Lines
    {
        get => GetValue(LinesProperty);
        set => SetValue(LinesProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == LinesProperty)
            Content = Build();
    }

    private Control Build()
    {
        var list = new ListBox
        {
            ItemsSource = Lines.Select((line, index) => $"[{index}] {line.DisplayLabel}").ToArray(),
            Height = 420
        };

        return list;
    }
}
