using Avalonia.Collections;
using Avalonia.Controls.Primitives;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia;

namespace Flowxel.UI.Controls.Docking;

public class DockTabGroupEventArgs : EventArgs
{
    public DockPane Pane { get; }
    public DockTabGroup SourceGroup { get; }
    public IPointer? Pointer { get; }

    public DockTabGroupEventArgs(DockPane pane, DockTabGroup sourceGroup, IPointer? pointer = null)
    {
        Pane = pane;
        SourceGroup = sourceGroup;
        Pointer = pointer;
    }
}

public class DockTabGroup : TemplatedControl
{
    private ListBox? _tabStrip;
    private Point _dragStartPoint;
    private DockPane? _dragCandidate;
    private bool _isDragging;
    private const double DragThreshold = 5.0;

    public AvaloniaList<DockPane> Panes { get; } = new();

    public static readonly StyledProperty<DockPane?> SelectedPaneProperty =
        AvaloniaProperty.Register<DockTabGroup, DockPane?>(
            nameof(SelectedPane),
            defaultBindingMode: BindingMode.TwoWay);

    public DockPane? SelectedPane
    {
        get => GetValue(SelectedPaneProperty);
        set => SetValue(SelectedPaneProperty, value);
    }

    public event EventHandler<DockTabGroupEventArgs>? PaneDragStarted;
    public event EventHandler<DockTabGroupEventArgs>? PaneCloseRequested;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_tabStrip is not null)
        {
            _tabStrip.SelectionChanged -= OnTabStripSelectionChanged;
            _tabStrip.RemoveHandler(PointerPressedEvent, OnTabStripPointerPressed);
            _tabStrip.RemoveHandler(PointerMovedEvent, OnTabStripPointerMoved);
            _tabStrip.RemoveHandler(PointerReleasedEvent, OnTabStripPointerReleased);
        }

        _tabStrip = e.NameScope.Find<ListBox>("PART_TabStrip");

        if (_tabStrip is not null)
        {
            _tabStrip.SelectionChanged += OnTabStripSelectionChanged;
            _tabStrip.AddHandler(PointerPressedEvent, OnTabStripPointerPressed, RoutingStrategies.Bubble, true);
            _tabStrip.AddHandler(PointerMovedEvent, OnTabStripPointerMoved, RoutingStrategies.Bubble, true);
            _tabStrip.AddHandler(PointerReleasedEvent, OnTabStripPointerReleased, RoutingStrategies.Bubble, true);
        }

        if (SelectedPane is null && Panes.Count > 0)
            SelectedPane = Panes[0];
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SelectedPaneProperty && _tabStrip is not null)
        {
            var pane = change.GetNewValue<DockPane?>();
            if (_tabStrip.SelectedItem != pane)
                _tabStrip.SelectedItem = pane;
        }
    }

    private void OnTabStripSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_tabStrip?.SelectedItem is DockPane pane)
            SelectedPane = pane;
    }

    private void OnTabStripPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _isDragging = false;
        _dragCandidate = null;

        if (_tabStrip == null || !e.GetCurrentPoint(_tabStrip).Properties.IsLeftButtonPressed)
            return;

        var hitVisual = _tabStrip.InputHitTest(e.GetPosition(_tabStrip)) as Visual;
        if (hitVisual == null)
            return;

        if (IsCloseButton(hitVisual))
        {
            var pane = FindPaneFromVisual(hitVisual);
            if (pane != null && pane.CanClose)
            {
                PaneCloseRequested?.Invoke(this, new DockTabGroupEventArgs(pane, this));
                e.Handled = true;
            }
            return;
        }

        var paneForDrag = FindPaneFromVisual(hitVisual);
        if (paneForDrag != null && paneForDrag.CanMove)
        {
            _dragCandidate = paneForDrag;
            _dragStartPoint = e.GetPosition(this);
        }
    }

    private void OnTabStripPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragCandidate == null || _isDragging)
            return;

        var currentPoint = e.GetPosition(this);
        var delta = currentPoint - _dragStartPoint;

        if (Math.Abs(delta.X) > DragThreshold || Math.Abs(delta.Y) > DragThreshold)
        {
            _isDragging = true;
            var pane = _dragCandidate;
            _dragCandidate = null;
            PaneDragStarted?.Invoke(this, new DockTabGroupEventArgs(pane!, this, e.Pointer));
        }
    }

    private void OnTabStripPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _dragCandidate = null;
        _isDragging = false;
    }

    private static bool IsCloseButton(Visual? visual)
    {
        var current = visual;
        while (current != null)
        {
            if (current is Button button && button.Classes.Contains("dock-pane-close"))
                return true;
            current = current.GetVisualParent() as Visual;
        }
        return false;
    }

    private static DockPane? FindPaneFromVisual(Visual? visual)
    {
        var current = visual;
        while (current != null)
        {
            if (current is ListBoxItem item && item.Content is DockPane pane)
                return pane;
            current = current.GetVisualParent() as Visual;
        }
        return null;
    }
}
