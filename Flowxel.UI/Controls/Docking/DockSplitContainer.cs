using Avalonia.Controls.Primitives;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia;

namespace Flowxel.UI.Controls.Docking;

public class DockSplitContainer : TemplatedControl
{
    private Grid? _grid;
    private ContentControl? _first;
    private GridSplitter? _splitter;
    private ContentControl? _second;

    public static readonly StyledProperty<Control?> FirstProperty =
        AvaloniaProperty.Register<DockSplitContainer, Control?>(nameof(First));

    public static readonly StyledProperty<Control?> SecondProperty =
        AvaloniaProperty.Register<DockSplitContainer, Control?>(nameof(Second));

    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<DockSplitContainer, Orientation>(nameof(Orientation), Orientation.Horizontal);

    public static readonly StyledProperty<GridLength> FirstSizeProperty =
        AvaloniaProperty.Register<DockSplitContainer, GridLength>(nameof(FirstSize), new GridLength(1, GridUnitType.Star));

    public static readonly StyledProperty<GridLength> SecondSizeProperty =
        AvaloniaProperty.Register<DockSplitContainer, GridLength>(nameof(SecondSize), new GridLength(1, GridUnitType.Star));

    public Control? First
    {
        get => GetValue(FirstProperty);
        set => SetValue(FirstProperty, value);
    }

    public Control? Second
    {
        get => GetValue(SecondProperty);
        set => SetValue(SecondProperty, value);
    }

    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public GridLength FirstSize
    {
        get => GetValue(FirstSizeProperty);
        set => SetValue(FirstSizeProperty, value);
    }

    public GridLength SecondSize
    {
        get => GetValue(SecondSizeProperty);
        set => SetValue(SecondSizeProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _grid = e.NameScope.Find<Grid>("PART_Grid");
        _first = e.NameScope.Find<ContentControl>("PART_First");
        _splitter = e.NameScope.Find<GridSplitter>("PART_Splitter");
        _second = e.NameScope.Find<ContentControl>("PART_Second");

        ConfigureLayout();
        UpdatePseudoClasses();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == OrientationProperty)
        {
            ConfigureLayout();
            UpdatePseudoClasses();
        }
        else if (change.Property == FirstSizeProperty || change.Property == SecondSizeProperty)
        {
            ConfigureLayout();
        }
    }

    private void ConfigureLayout()
    {
        if (_grid == null || _first == null || _splitter == null || _second == null)
            return;

        _grid.ColumnDefinitions.Clear();
        _grid.RowDefinitions.Clear();

        if (Orientation == Orientation.Horizontal)
        {
            _grid.ColumnDefinitions.Add(new ColumnDefinition(FirstSize));
            _grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            _grid.ColumnDefinitions.Add(new ColumnDefinition(SecondSize));

            Grid.SetColumn(_first, 0);
            Grid.SetRow(_first, 0);
            Grid.SetColumn(_splitter, 1);
            Grid.SetRow(_splitter, 0);
            Grid.SetColumn(_second, 2);
            Grid.SetRow(_second, 0);

            // Reset rows
            Grid.SetRowSpan(_first, 1);
            Grid.SetRowSpan(_splitter, 1);
            Grid.SetRowSpan(_second, 1);
            Grid.SetColumnSpan(_first, 1);
            Grid.SetColumnSpan(_splitter, 1);
            Grid.SetColumnSpan(_second, 1);

            _splitter.ResizeDirection = GridResizeDirection.Columns;
        }
        else
        {
            _grid.RowDefinitions.Add(new RowDefinition(FirstSize));
            _grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            _grid.RowDefinitions.Add(new RowDefinition(SecondSize));

            Grid.SetRow(_first, 0);
            Grid.SetColumn(_first, 0);
            Grid.SetRow(_splitter, 1);
            Grid.SetColumn(_splitter, 0);
            Grid.SetRow(_second, 2);
            Grid.SetColumn(_second, 0);

            // Reset spans
            Grid.SetRowSpan(_first, 1);
            Grid.SetRowSpan(_splitter, 1);
            Grid.SetRowSpan(_second, 1);
            Grid.SetColumnSpan(_first, 1);
            Grid.SetColumnSpan(_splitter, 1);
            Grid.SetColumnSpan(_second, 1);

            _splitter.ResizeDirection = GridResizeDirection.Rows;
        }
    }

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(":horizontal", Orientation == Orientation.Horizontal);
        PseudoClasses.Set(":vertical", Orientation == Orientation.Vertical);
    }
}
