using Avalonia.Collections;
using Avalonia.Controls.Primitives;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.VisualTree;
using Avalonia;
using System.Collections.Specialized;

namespace Flowxel.UI.Controls.Docking;

public class DockingControl : TemplatedControl
{
    private ContentControl? _rootHost;
    private Border? _dropOverlay;
    private Panel? _rootPanel;
    private DockDragSession? _dragSession;
    private DockPane? _focusedPane;

    [Content]
    public AvaloniaList<DockPane> Panes { get; } = new();

    public DockingControl()
    {
        Panes.CollectionChanged += OnPanesCollectionChanged;
    }

    public static readonly StyledProperty<DockLayoutNode?> LayoutRootProperty =
        AvaloniaProperty.Register<DockingControl, DockLayoutNode?>(nameof(LayoutRoot));

    public static readonly StyledProperty<bool> EnablePaneFocusTrackingProperty =
        AvaloniaProperty.Register<DockingControl, bool>(nameof(EnablePaneFocusTracking), true);

    public static readonly StyledProperty<bool> HighlightFocusedPaneProperty =
        AvaloniaProperty.Register<DockingControl, bool>(nameof(HighlightFocusedPane), false);

    public static readonly StyledProperty<DockInitialLayoutMode> InitialLayoutModeProperty =
        AvaloniaProperty.Register<DockingControl, DockInitialLayoutMode>(nameof(InitialLayoutMode), DockInitialLayoutMode.Tabs);

    public static readonly StyledProperty<Orientation> InitialSplitOrientationProperty =
        AvaloniaProperty.Register<DockingControl, Orientation>(nameof(InitialSplitOrientation), Orientation.Horizontal);

    public DockLayoutNode? LayoutRoot
    {
        get => GetValue(LayoutRootProperty);
        set => SetValue(LayoutRootProperty, value);
    }

    public bool EnablePaneFocusTracking
    {
        get => GetValue(EnablePaneFocusTrackingProperty);
        set => SetValue(EnablePaneFocusTrackingProperty, value);
    }

    public bool HighlightFocusedPane
    {
        get => GetValue(HighlightFocusedPaneProperty);
        set => SetValue(HighlightFocusedPaneProperty, value);
    }

    public DockInitialLayoutMode InitialLayoutMode
    {
        get => GetValue(InitialLayoutModeProperty);
        set => SetValue(InitialLayoutModeProperty, value);
    }

    public Orientation InitialSplitOrientation
    {
        get => GetValue(InitialSplitOrientationProperty);
        set => SetValue(InitialSplitOrientationProperty, value);
    }

    public event EventHandler<DockPane?>? FocusedPaneChanged;
    public event EventHandler<DockPane>? PaneClosed;

    private void OnPanesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_rootHost is null)
            return;

        if (Panes.Count != 0)
            return;

        _rootHost.Content = null;
        if (EnablePaneFocusTracking)
            SetFocusedPane(null);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_rootPanel is not null)
        {
            _rootPanel.RemoveHandler(PointerPressedEvent, OnRootPointerPressed);
            _rootPanel.RemoveHandler(PointerMovedEvent, OnRootPointerMoved);
            _rootPanel.RemoveHandler(PointerReleasedEvent, OnRootPointerReleased);
        }

        _rootHost = e.NameScope.Find<ContentControl>("PART_RootHost");
        _dropOverlay = e.NameScope.Find<Border>("PART_DropOverlay");
        _rootPanel = e.NameScope.Find<Panel>("PART_RootPanel");

        if (_rootPanel is not null)
        {
            _rootPanel.AddHandler(PointerPressedEvent, OnRootPointerPressed, RoutingStrategies.Bubble, true);
            _rootPanel.AddHandler(PointerMovedEvent, OnRootPointerMoved, RoutingStrategies.Bubble, true);
            _rootPanel.AddHandler(PointerReleasedEvent, OnRootPointerReleased, RoutingStrategies.Bubble, true);
        }

        InitializeLayout();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == LayoutRootProperty)
        {
            BuildLayoutFromModel();
        }
    }

    public void SetRootLayout(Control root)
    {
        if (_rootHost == null)
            throw new InvalidOperationException("Cannot set layout before OnApplyTemplate");

        _rootHost.Content = root;
        WireAllGroups(root);
        RefreshSplitResizability(root);
    }

    private void BuildLayoutFromModel()
    {
        if (_rootHost == null || LayoutRoot == null)
            return;

        var visualRoot = BuildVisualTree(LayoutRoot);
        if (visualRoot != null)
        {
            _rootHost.Content = visualRoot;
            WireAllGroups(visualRoot);
            RefreshSplitResizability(visualRoot);
        }
    }

    private Control? BuildVisualTree(DockLayoutNode node)
    {
        return node switch
        {
            DockPaneModel paneModel => BuildPane(paneModel),
            DockTabGroupModel groupModel => BuildTabGroup(groupModel),
            DockSplitModel splitModel => BuildSplit(splitModel),
            _ => null
        };
    }

    private DockPane BuildPane(DockPaneModel model)
    {
        return new DockPane
        {
            Header = model.Header,
            PaneContent = model.Content,
            CanClose = model.CanClose,
            CanMove = model.CanMove,
            HorizontalSizeMode = model.HorizontalSizeMode,
            VerticalSizeMode = model.VerticalSizeMode,
            PreferredWidth = model.PreferredWidth,
            PreferredHeight = model.PreferredHeight
        };
    }

    private DockTabGroup BuildTabGroup(DockTabGroupModel model)
    {
        var group = new DockTabGroup();

        foreach (var paneModel in model.Panes)
        {
            var pane = BuildPane(paneModel);
            group.Panes.Add(pane);

            if (paneModel == model.SelectedPane)
                group.SelectedPane = pane;
        }

        return group;
    }

    private DockSplitContainer BuildSplit(DockSplitModel model)
    {
        var split = new DockSplitContainer
        {
            Orientation = model.Orientation,
            FirstSize = model.FirstSize,
            SecondSize = model.SecondSize,
            FirstResizable = model.FirstResizable,
            SecondResizable = model.SecondResizable
        };

        if (model.First != null)
            split.First = BuildVisualTree(model.First);

        if (model.Second != null)
            split.Second = BuildVisualTree(model.Second);

        return split;
    }

    private void InitializeLayout()
    {
        if (_rootHost == null)
            return;

        // If LayoutRoot model is set, build from model
        if (LayoutRoot != null)
        {
            BuildLayoutFromModel();
            return;
        }

        // If no panes, nothing to do
        if (Panes.Count == 0)
            return;

        // Skip if layout already set programmatically
        if (_rootHost.Content != null)
        {
            WireAllGroups(_rootHost.Content as Control);
            RefreshSplitResizability(_rootHost.Content as Control);
            return;
        }

        // Default: single group with all panes
        if (InitialLayoutMode == DockInitialLayoutMode.SideBySide && Panes.Count >= 2)
        {
            var firstGroup = new DockTabGroup();
            firstGroup.Panes.Add(Panes[0]);
            firstGroup.SelectedPane = Panes[0];
            WireGroup(firstGroup);

            var secondGroup = new DockTabGroup();
            for (var i = 1; i < Panes.Count; i++)
                secondGroup.Panes.Add(Panes[i]);
            secondGroup.SelectedPane = secondGroup.Panes.FirstOrDefault();
            WireGroup(secondGroup);

            var split = new DockSplitContainer
            {
                Orientation = InitialSplitOrientation,
                First = firstGroup,
                Second = secondGroup,
                FirstSize = ResolveInitialSize(Panes[0], InitialSplitOrientation),
                SecondSize = ResolveInitialSize(Panes[1], InitialSplitOrientation),
                FirstResizable = IsPaneResizable(Panes[0], InitialSplitOrientation),
                SecondResizable = IsPaneResizable(Panes[1], InitialSplitOrientation)
            };

            _rootHost.Content = split;
            SetFocusedPane(firstGroup.SelectedPane);
            RefreshSplitResizability(split);
            return;
        }

        var group = new DockTabGroup();
        foreach (var pane in Panes)
            group.Panes.Add(pane);

        WireGroup(group);
        _rootHost.Content = group;
        RefreshSplitResizability(group);
    }

    private void WireAllGroups(Control? control)
    {
        if (control is DockTabGroup group)
        {
            WireGroup(group);
            return;
        }

        if (control is DockSplitContainer split)
        {
            if (split.First != null) WireAllGroups(split.First);
            if (split.Second != null) WireAllGroups(split.Second);
        }
    }

    private void WireGroup(DockTabGroup group)
    {
        group.PaneDragStarted += OnPaneDragStarted;
        group.PaneCloseRequested += OnPaneCloseRequested;
        group.PropertyChanged += OnGroupPropertyChanged;
    }

    private void UnwireGroup(DockTabGroup group)
    {
        group.PaneDragStarted -= OnPaneDragStarted;
        group.PaneCloseRequested -= OnPaneCloseRequested;
        group.PropertyChanged -= OnGroupPropertyChanged;
    }

    private void OnGroupPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != DockTabGroup.SelectedPaneProperty || sender is not DockTabGroup group)
            return;

        if (EnablePaneFocusTracking)
            SetFocusedPane(group.SelectedPane);

        UpdateAncestorSplitResizability(group);
    }

    private void SetFocusedPane(DockPane? pane)
    {
        if (ReferenceEquals(_focusedPane, pane))
            return;

        _focusedPane = pane;
        FocusedPaneChanged?.Invoke(this, pane);
    }

    public void AddPane(DockPane pane)
    {
        Panes.Add(pane);

        if (_rootHost?.Content is DockTabGroup rootGroup)
        {
            rootGroup.Panes.Add(pane);
            rootGroup.SelectedPane = pane;
            SetFocusedPane(pane);
            return;
        }

        if (_rootHost?.Content is null)
        {
            InitializeLayout();
            if (_rootHost?.Content is DockTabGroup initGroup)
            {
                initGroup.SelectedPane = pane;
                SetFocusedPane(pane);
            }
            return;
        }

        var first = FindFirstTabGroup(_rootHost.Content as Control);
        if (first is null)
        {
            var group = new DockTabGroup();
            group.Panes.Add(pane);
            group.SelectedPane = pane;
            WireGroup(group);
            _rootHost.Content = group;
            SetFocusedPane(pane);
            return;
        }

        first.Panes.Add(pane);
        first.SelectedPane = pane;
        SetFocusedPane(pane);
    }

    private static DockTabGroup? FindFirstTabGroup(Control? control)
    {
        if (control is null)
            return null;
        if (control is DockTabGroup group)
            return group;
        if (control is not DockSplitContainer split)
            return null;
        return FindFirstTabGroup(split.First) ?? FindFirstTabGroup(split.Second);
    }

    private void OnPaneDragStarted(object? sender, DockTabGroupEventArgs e)
    {
        if (_rootPanel == null || _dropOverlay == null)
            return;

        _dragSession = new DockDragSession(e.Pane, e.SourceGroup);
        _dropOverlay.IsVisible = false;

        e.Pointer?.Capture(_rootPanel);
    }

    private void OnRootPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragSession == null || _dropOverlay == null || _rootPanel == null)
            return;

        var position = e.GetPosition(_rootPanel);
        var targetGroup = HitTestTabGroup(position);

        if (targetGroup == null)
        {
            _dropOverlay.IsVisible = false;
            _dragSession.TargetGroup = null;
            _dragSession.TargetPosition = DockPosition.Center;
            return;
        }

        _dragSession.TargetGroup = targetGroup;

        // Determine drop zone based on pointer position relative to target
        var groupBounds = targetGroup.Bounds;
        var groupTopLeft = targetGroup.TranslatePoint(new Point(0, 0), _rootPanel);
        if (groupTopLeft == null)
        {
            _dropOverlay.IsVisible = false;
            return;
        }

        var relativePos = position - groupTopLeft.Value;
        var zone = DetermineDropZone(relativePos, groupBounds.Width, groupBounds.Height);
        _dragSession.TargetPosition = zone;

        // Position and show overlay
        ShowDropOverlay(groupTopLeft.Value, groupBounds.Width, groupBounds.Height, zone);
    }

    private void OnRootPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!EnablePaneFocusTracking || _rootPanel is null || _dragSession is not null)
            return;

        var position = e.GetPosition(_rootPanel);
        var hitVisual = _rootPanel.InputHitTest(position) as Visual;
        if (hitVisual is null)
            return;

        var group = hitVisual.GetSelfAndVisualAncestors().OfType<DockTabGroup>().FirstOrDefault();
        if (group is null)
            return;

        SetFocusedPane(group.SelectedPane);
    }

    private void OnRootPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_dragSession == null)
            return;

        // Release pointer capture
        e.Pointer.Capture(null);

        var session = _dragSession;
        _dragSession = null;

        if (_dropOverlay != null)
            _dropOverlay.IsVisible = false;

        if (session.TargetGroup == null)
            return;

        ExecuteDrop(session);
    }

    private DockPosition DetermineDropZone(Point relativePos, double width, double height)
    {
        double edgeBand = 0.25;

        double leftBand = width * edgeBand;
        double rightBand = width * (1 - edgeBand);
        double topBand = height * edgeBand;
        double bottomBand = height * (1 - edgeBand);

        if (relativePos.X < leftBand)
            return DockPosition.Left;
        if (relativePos.X > rightBand)
            return DockPosition.Right;
        if (relativePos.Y < topBand)
            return DockPosition.Top;
        if (relativePos.Y > bottomBand)
            return DockPosition.Bottom;

        return DockPosition.Center;
    }

    private void ShowDropOverlay(Point groupOrigin, double groupWidth, double groupHeight, DockPosition zone)
    {
        if (_dropOverlay == null)
            return;

        double x = groupOrigin.X;
        double y = groupOrigin.Y;
        double w = groupWidth;
        double h = groupHeight;

        switch (zone)
        {
            case DockPosition.Left:
                w = groupWidth * 0.5;
                break;
            case DockPosition.Right:
                x += groupWidth * 0.5;
                w = groupWidth * 0.5;
                break;
            case DockPosition.Top:
                h = groupHeight * 0.5;
                break;
            case DockPosition.Bottom:
                y += groupHeight * 0.5;
                h = groupHeight * 0.5;
                break;
        }

        _dropOverlay.Width = w;
        _dropOverlay.Height = h;
        _dropOverlay.RenderTransform = new TranslateTransform(x, y);
        _dropOverlay.IsVisible = true;
    }

    private void ExecuteDrop(DockDragSession session)
    {
        var pane = session.Pane;
        var sourceGroup = session.SourceGroup;
        var targetGroup = session.TargetGroup!;
        var position = session.TargetPosition;

        // Drop on own group at center → no-op
        if (sourceGroup == targetGroup && position == DockPosition.Center)
            return;

        // Drop on own group at edge with single pane → no-op
        if (sourceGroup == targetGroup && sourceGroup.Panes.Count <= 1)
            return;

        // Switch selection away first so the ContentPresenter releases the
        // pane from the logical tree before we re-parent it.
        if (sourceGroup.SelectedPane == pane)
        {
            var idx = sourceGroup.Panes.IndexOf(pane);
            sourceGroup.SelectedPane = sourceGroup.Panes.Count > 1
                ? sourceGroup.Panes[idx == 0 ? 1 : idx - 1]
                : null;
        }

        sourceGroup.Panes.Remove(pane);

        if (position == DockPosition.Center)
        {
            // Move to target group as new tab
            targetGroup.Panes.Add(pane);
            targetGroup.SelectedPane = pane;
        }
        else
        {
            // Split target group
            SplitGroup(targetGroup, pane, position);
        }

        // Collapse empty groups
        if (sourceGroup.Panes.Count == 0)
            CollapseEmptyGroup(sourceGroup);
    }

    private void SplitGroup(DockTabGroup targetGroup, DockPane pane, DockPosition position)
    {
        if (_rootHost == null)
            return;

        var newGroup = new DockTabGroup();
        newGroup.Panes.Add(pane);
        newGroup.SelectedPane = pane;
        WireGroup(newGroup);

        var orientation = position is DockPosition.Left or DockPosition.Right
            ? Orientation.Horizontal
            : Orientation.Vertical;

        var split = new DockSplitContainer { Orientation = orientation };
        var paneSize = ResolveInitialSize(pane, orientation);
        var targetSize = new GridLength(1, GridUnitType.Star);
        var paneResizable = IsPaneResizable(pane, orientation);
        var targetResizable = IsGroupResizable(targetGroup, orientation);

        bool newIsFirst = position is DockPosition.Left or DockPosition.Top;

        if (newIsFirst)
        {
            split.First = newGroup;
            split.Second = targetGroup;
            split.FirstSize = paneSize;
            split.SecondSize = targetSize;
            split.FirstResizable = paneResizable;
            split.SecondResizable = targetResizable;
        }
        else
        {
            split.First = targetGroup;
            split.Second = newGroup;
            split.FirstSize = targetSize;
            split.SecondSize = paneSize;
            split.FirstResizable = targetResizable;
            split.SecondResizable = paneResizable;
        }

        ReplaceInParent(targetGroup, split);
    }

    private static GridLength ResolveInitialSize(DockPane pane, Orientation orientation)
    {
        if (orientation == Orientation.Horizontal)
        {
            if (!double.IsNaN(pane.PreferredWidth) && pane.PreferredWidth > 0)
                return new GridLength(pane.PreferredWidth, GridUnitType.Pixel);

            return pane.HorizontalSizeMode == DockPaneSizeMode.Content
                ? GridLength.Auto
                : new GridLength(1, GridUnitType.Star);
        }

        if (!double.IsNaN(pane.PreferredHeight) && pane.PreferredHeight > 0)
            return new GridLength(pane.PreferredHeight, GridUnitType.Pixel);

        return pane.VerticalSizeMode == DockPaneSizeMode.Content
            ? GridLength.Auto
            : new GridLength(1, GridUnitType.Star);
    }

    private static bool IsPaneResizable(DockPane pane, Orientation orientation)
    {
        return orientation == Orientation.Horizontal
            ? pane.HorizontalSizeMode == DockPaneSizeMode.Stretch
            : pane.VerticalSizeMode == DockPaneSizeMode.Stretch;
    }

    private static bool IsGroupResizable(DockTabGroup group, Orientation orientation)
    {
        var pane = group.SelectedPane ?? group.Panes.FirstOrDefault();
        return pane is null || IsPaneResizable(pane, orientation);
    }

    private static bool IsControlResizable(Control? control, Orientation orientation)
    {
        return control switch
        {
            DockTabGroup group => IsGroupResizable(group, orientation),
            DockPane pane => IsPaneResizable(pane, orientation),
            _ => true
        };
    }

    private void UpdateAncestorSplitResizability(Control child)
    {
        var current = child;
        while (true)
        {
            var parent = FindParentSplit(current);
            if (parent is null)
                return;

            parent.FirstResizable = IsControlResizable(parent.First, parent.Orientation);
            parent.SecondResizable = IsControlResizable(parent.Second, parent.Orientation);
            current = parent;
        }
    }

    private void CollapseEmptyGroup(DockTabGroup emptyGroup)
    {
        if (_rootHost == null)
            return;

        UnwireGroup(emptyGroup);

        // If the empty group is the root content
        if (_rootHost.Content == emptyGroup)
        {
            _rootHost.Content = null;
            return;
        }

        // Find parent DockSplitContainer
        var parent = FindParentSplit(emptyGroup);
        if (parent == null)
            return;

        // Get the surviving child
        Control? survivor = null;
        if (parent.First == emptyGroup)
            survivor = parent.Second;
        else if (parent.Second == emptyGroup)
            survivor = parent.First;

        if (survivor == null)
            return;

        // Detach survivor from the split
        parent.First = null;
        parent.Second = null;

        // Replace the split with the survivor
        ReplaceInParent(parent, survivor);
    }

    private void ReplaceInParent(Control target, Control replacement)
    {
        if (_rootHost == null)
            return;

        if (_rootHost.Content == target)
        {
            _rootHost.Content = replacement;
            RefreshSplitResizability(replacement);
            return;
        }

        var parent = FindParentSplit(target);
        if (parent == null)
            return;

        if (parent.First == target)
            parent.First = replacement;
        else if (parent.Second == target)
            parent.Second = replacement;

        UpdateAncestorSplitResizability(parent);
    }

    private DockSplitContainer? FindParentSplit(Control child)
    {
        if (_rootHost?.Content is not Control root)
            return null;

        return FindParentSplitRecursive(root, child);
    }

    private DockSplitContainer? FindParentSplitRecursive(Control current, Control target)
    {
        if (current is DockSplitContainer split)
        {
            if (split.First == target || split.Second == target)
                return split;

            if (split.First != null)
            {
                var result = FindParentSplitRecursive(split.First, target);
                if (result != null) return result;
            }

            if (split.Second != null)
            {
                var result = FindParentSplitRecursive(split.Second, target);
                if (result != null) return result;
            }
        }

        return null;
    }

    private DockTabGroup? HitTestTabGroup(Point position)
    {
        if (_rootPanel == null)
            return null;

        var groups = new List<DockTabGroup>();
        CollectTabGroups(_rootHost?.Content as Control, groups);

        foreach (var group in groups)
        {
            var topLeft = group.TranslatePoint(new Point(0, 0), _rootPanel);
            if (topLeft == null) continue;

            var bounds = new Rect(topLeft.Value, group.Bounds.Size);
            if (bounds.Contains(position))
                return group;
        }

        return null;
    }

    private void CollectTabGroups(Control? control, List<DockTabGroup> groups)
    {
        if (control is DockTabGroup group)
        {
            groups.Add(group);
            return;
        }

        if (control is DockSplitContainer split)
        {
            if (split.First != null) CollectTabGroups(split.First, groups);
            if (split.Second != null) CollectTabGroups(split.Second, groups);
        }
    }

    private void RefreshSplitResizability(Control? root)
    {
        if (root is not DockSplitContainer split)
            return;

        RefreshSplitResizability(split.First);
        RefreshSplitResizability(split.Second);
        split.FirstResizable = IsControlResizable(split.First, split.Orientation);
        split.SecondResizable = IsControlResizable(split.Second, split.Orientation);
    }

    public void ClosePane(DockPane pane)
    {
        if (_rootHost?.Content == null)
            return;

        var groups = new List<DockTabGroup>();
        CollectTabGroups(_rootHost.Content as Control, groups);

        foreach (var group in groups)
        {
            if (group.Panes.Contains(pane))
            {
                if (group.SelectedPane == pane)
                {
                    var idx = group.Panes.IndexOf(pane);
                    group.SelectedPane = group.Panes.Count > 1
                        ? group.Panes[idx == 0 ? 1 : idx - 1]
                        : null;
                }

                group.Panes.Remove(pane);
                PaneClosed?.Invoke(this, pane);

                if (group.Panes.Count == 0)
                {
                    CollapseEmptyGroup(group);
                    if (EnablePaneFocusTracking)
                        SetFocusedPane(null);
                }
                else if (EnablePaneFocusTracking && group.SelectedPane is not null)
                    SetFocusedPane(group.SelectedPane);

                return;
            }
        }
    }

    private void OnPaneCloseRequested(object? sender, DockTabGroupEventArgs e)
    {
        ClosePane(e.Pane);
    }

    private class DockDragSession
    {
        public DockPane Pane { get; }
        public DockTabGroup SourceGroup { get; }
        public DockTabGroup? TargetGroup { get; set; }
        public DockPosition TargetPosition { get; set; } = DockPosition.Center;

        public DockDragSession(DockPane pane, DockTabGroup sourceGroup)
        {
            Pane = pane;
            SourceGroup = sourceGroup;
        }
    }
}
