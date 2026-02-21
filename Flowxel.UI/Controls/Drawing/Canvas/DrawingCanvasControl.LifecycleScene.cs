using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Flowxel.Core.Geometry.Primitives;
using Flowxel.Core.Geometry.Shapes;
using Flowxel.UI.Controls;

using AvaloniaPoint = global::Avalonia.Point;
using FlowVector = Flowxel.Core.Geometry.Primitives.Vector;
using Shape = Flowxel.Core.Geometry.Shapes.Shape;

namespace Flowxel.UI.Controls.Drawing;

public partial class DrawingCanvasControl
{
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ShapesProperty)
        {
            if (change.OldValue is IList<Shape> oldShapes)
                DetachShapesCollection(oldShapes);

            if (change.NewValue is IList<Shape> newShapes)
                AttachShapesCollection(newShapes);
        }

        if (change.Property == ComputedShapeIdsProperty)
        {
            if (change.OldValue is IList<string> oldComputed)
                DetachComputedShapeIdsCollection(oldComputed);

            if (change.NewValue is IList<string> newComputed)
                AttachComputedShapeIdsCollection(newComputed);

            InvalidateScene();
        }

        if (change.Property == BindingCandidateShapeIdsProperty)
        {
            if (change.OldValue is IList<string> oldCandidates)
                DetachBindingCandidateShapeIdsCollection(oldCandidates);

            if (change.NewValue is IList<string> newCandidates)
                AttachBindingCandidateShapeIdsCollection(newCandidates);

            InvalidateScene();
        }

        if (change.Property == ZoomProperty ||
            change.Property == PanProperty ||
            change.Property == ActiveToolProperty ||
            change.Property == InteractionModeProperty ||
            change.Property == UseDebugOverlayRendererProperty ||
            change.Property == RenderBackendKindProperty ||
            change.Property == CanvasBackgroundProperty ||
            change.Property == ShowCanvasBoundaryProperty ||
            change.Property == CanvasBoundaryWidthProperty ||
            change.Property == CanvasBoundaryHeightProperty ||
            change.Property == CanvasBoundaryStrokeProperty ||
            change.Property == HoverStrokeProperty ||
            change.Property == ComputedStrokeProperty ||
            change.Property == SelectedStrokeProperty ||
            change.Property == HandleFillProperty ||
            change.Property == HandleStrokeProperty ||
            change.Property == HandleSizeProperty ||
            change.Property == OriginMarkerSizeProperty ||
            change.Property == OriginXAxisBrushProperty ||
            change.Property == OriginYAxisBrushProperty)
        {
            if (change.Property == UseDebugOverlayRendererProperty)
                _renderer = CreateRenderer();
            else if (change.Property == RenderBackendKindProperty)
                _renderBackend = CreateRenderBackend();

            InvalidateScene();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        AttachLifecycleHandlers();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        DetachLifecycleHandlers();
    }

    private void AttachLifecycleHandlers()
    {
        if (_isLifecycleAttached)
            return;

        _isLifecycleAttached = true;
        _contextMenu.CanvasSettingsRequested += OnCanvasSettingsRequested;
        _contextMenu.DeleteShapeRequested += OnDeleteShapeRequested;
        _contextMenu.CenterViewRequested += OnCenterViewRequested;
        _contextMenu.PropertiesRequested += OnPropertiesRequested;
        ContextRequested += OnContextRequested;
        AttachShapesCollection(Shapes);
        AttachComputedShapeIdsCollection(ComputedShapeIds);
        AttachBindingCandidateShapeIdsCollection(BindingCandidateShapeIds);
    }

    private void DetachLifecycleHandlers()
    {
        if (!_isLifecycleAttached)
            return;

        _contextMenu.CanvasSettingsRequested -= OnCanvasSettingsRequested;
        _contextMenu.DeleteShapeRequested -= OnDeleteShapeRequested;
        _contextMenu.CenterViewRequested -= OnCenterViewRequested;
        _contextMenu.PropertiesRequested -= OnPropertiesRequested;
        ContextRequested -= OnContextRequested;
        DetachShapesCollection(Shapes);
        DetachComputedShapeIdsCollection(ComputedShapeIds);
        DetachBindingCandidateShapeIdsCollection(BindingCandidateShapeIds);
        _contextMenu.Close();
        _openContextMenuOnRightRelease = false;
        _isMiddlePanning = false;
        _gestureStartWorld = null;
        _gestureStartScreen = null;
        _activeHandle = ShapeHandleKind.None;
        _lastDragWorld = null;
        _previewShape = null;
        _hoveredShape = null;
        _contextMenuTargetShape = null;
        _selectedShape = null;
        ClearImageCache();
        ClearTextLayoutCache();
        _isLifecycleAttached = false;
    }

    private void ConfigureContextMenuTarget(Shape? shape)
    {
        if (shape is null)
        {
            _contextMenu.ConfigureForCanvas();
            return;
        }

        if (IsComputedShape(shape))
            _contextMenu.ConfigureForComputedShape();
        else
            _contextMenu.ConfigureForShape();
    }

    private void AttachShapesCollection(IList<Shape> shapes)
    {
        if (!_isLifecycleAttached)
            return;

        if (shapes is INotifyCollectionChanged observableCollection)
            observableCollection.CollectionChanged += OnShapesCollectionChanged;
    }

    private void DetachShapesCollection(IList<Shape> shapes)
    {
        if (shapes is INotifyCollectionChanged observableCollection)
            observableCollection.CollectionChanged -= OnShapesCollectionChanged;
    }

    private void AttachComputedShapeIdsCollection(IList<string> computedShapeIds)
    {
        if (!_isLifecycleAttached)
            return;

        if (computedShapeIds is INotifyCollectionChanged observableCollection)
            observableCollection.CollectionChanged += OnComputedShapeIdsCollectionChanged;
    }

    private void DetachComputedShapeIdsCollection(IList<string> computedShapeIds)
    {
        if (computedShapeIds is INotifyCollectionChanged observableCollection)
            observableCollection.CollectionChanged -= OnComputedShapeIdsCollectionChanged;
    }

    private void OnShapesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_hoveredShape is not null && !Shapes.Contains(_hoveredShape))
            _hoveredShape = null;

        if (_selectedShape is not null && !Shapes.Contains(_selectedShape))
            _selectedShape = null;

        for (var i = ComputedShapeIds.Count - 1; i >= 0; i--)
        {
            var id = ComputedShapeIds[i];
            if (!Shapes.Any(shape => shape.Id == id))
                ComputedShapeIds.RemoveAt(i);
        }

        InvalidateScene();
    }

    private void OnComputedShapeIdsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => InvalidateScene();

    private void AttachBindingCandidateShapeIdsCollection(IList<string> candidateShapeIds)
    {
        if (!_isLifecycleAttached)
            return;

        if (candidateShapeIds is INotifyCollectionChanged observableCollection)
            observableCollection.CollectionChanged += OnBindingCandidateShapeIdsCollectionChanged;
    }

    private void DetachBindingCandidateShapeIdsCollection(IList<string> candidateShapeIds)
    {
        if (candidateShapeIds is INotifyCollectionChanged observableCollection)
            observableCollection.CollectionChanged -= OnBindingCandidateShapeIdsCollectionChanged;
    }

    private void OnBindingCandidateShapeIdsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => InvalidateScene();

    private void OnDeleteShapeRequested(object? sender, EventArgs e)
    {
        if (_contextMenuTargetShape is null || IsComputedShape(_contextMenuTargetShape))
            return;

        Shapes.Remove(_contextMenuTargetShape);
        if (ReferenceEquals(_selectedShape, _contextMenuTargetShape))
            _selectedShape = null;

        _hoveredShape = null;
        _contextMenuTargetShape = null;
        _activeHandle = ShapeHandleKind.None;
        _lastDragWorld = null;
        InvalidateScene();
    }

    private void OnCenterViewRequested(object? sender, EventArgs e)
        => CenterViewOnOrigin();

    private async void OnCanvasSettingsRequested(object? sender, EventArgs e)
        => await ShowCanvasSettingsDialogAsync();

    private async void OnPropertiesRequested(object? sender, EventArgs e)
    {
        if (_contextMenuTargetShape is null || IsComputedShape(_contextMenuTargetShape))
            return;

        await ShowShapePropertiesDialogAsync(_contextMenuTargetShape);
    }

    private bool IsComputedShape(Shape shape)
        => ComputedShapeIds.Contains(shape.Id);

    private bool IsBindingCandidate(Shape shape)
    {
        if (InteractionMode != DrawingInteractionMode.Bind)
            return false;

        return BindingCandidateShapeIds.Contains(shape.Id);
    }

    public void SetShapeComputed(Shape shape, bool isComputed)
    {
        if (isComputed)
        {
            if (!ComputedShapeIds.Contains(shape.Id))
                ComputedShapeIds.Add(shape.Id);
        }
        else
        {
            ComputedShapeIds.Remove(shape.Id);
        }

        InvalidateScene();
    }

    private async Task ShowShapePropertiesDialogAsync(Shape shape)
    {
        if (DialogService is null)
            return;

        var editor = new DrawingShapePropertiesEditor(shape);

        await DialogService.ShowAsync(dialog =>
        {
            dialog.Title = $"{shape.Type} properties";
            dialog.Content = editor;
            dialog.PrimaryButtonText = "Apply";
            dialog.CloseButtonText = "Cancel";
            dialog.DefaultButton = DefaultButton.Primary;
            dialog.PrimaryButtonCommand = new ActionCommand(() =>
            {
                editor.ApplyChanges();
                ClearImageCache();
                InvalidateScene();
            });
        });
    }

    private async Task ShowCanvasSettingsDialogAsync()
    {
        if (DialogService is null)
            return;

        var editor = new DrawingCanvasSettingsEditor(this);

        await DialogService.ShowAsync(dialog =>
        {
            dialog.Title = "Canvas settings";
            dialog.Content = editor;
            dialog.PrimaryButtonText = "Apply";
            dialog.CloseButtonText = "Cancel";
            dialog.DefaultButton = DefaultButton.Primary;
            dialog.PrimaryButtonCommand = new ActionCommand(() =>
            {
                editor.ApplyTo(this);
                InvalidateScene();
            });
        });
    }

    private void OnContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        if (!e.TryGetPosition(this, out var position))
            position = CursorAvaloniaPosition;

        var world = ScreenToWorld(position);
        _contextMenuTargetShape = FindHitShape(world);
        if (_contextMenuTargetShape is not null)
            _selectedShape = _contextMenuTargetShape;

        ConfigureContextMenuTarget(_contextMenuTargetShape);
        InvalidateScene();
    }

    private void UpdateCursorPositions(AvaloniaPoint screen, FlowVector world)
    {
        CursorAvaloniaPosition = screen;
        CursorCanvasPosition = new AvaloniaPoint(world.X, world.Y);
    }

    private DrawingCanvasSceneSnapshot GetSceneSnapshot()
    {
        if (!_isSceneSnapshotDirty)
            return _sceneSnapshot;

        _sceneSnapshot = BuildSceneSnapshot();
        _isSceneSnapshotDirty = false;
        return _sceneSnapshot;
    }

    private DrawingCanvasSceneSnapshot BuildSceneSnapshot()
    {
        var shapes = new List<DrawingCanvasSceneShape>(Shapes.Count);
        foreach (var shape in Shapes)
        {
            if (ReferenceEquals(shape, _selectedShape) || ReferenceEquals(shape, _hoveredShape))
                continue;

            var isComputed = IsComputedShape(shape);
            var isBindingCandidate = IsBindingCandidate(shape);
            var stroke = isBindingCandidate ? SelectedStroke : (isComputed ? ComputedStroke : ShapeStroke);
            var thickness = GetShapeStrokeThickness(shape) + (isBindingCandidate ? 1.25 : 0);
            var dashPattern = isBindingCandidate
                ? BindingDash
                : (isComputed && UseDashedComputedStroke) ? ComputedDash : null;
            shapes.Add(new DrawingCanvasSceneShape(shape, stroke, thickness, dashPattern, isComputed));
        }

        DrawingCanvasSceneShape? hoverShape = null;
        if (_hoveredShape is not null && !ReferenceEquals(_hoveredShape, _selectedShape))
            hoverShape = new DrawingCanvasSceneShape(_hoveredShape, HoverStroke, GetShapeStrokeThickness(_hoveredShape) + 1.25, null, IsComputedShape(_hoveredShape));

        DrawingCanvasSceneShape? selectedShape = null;
        var drawSelectedHandles = false;
        if (_selectedShape is not null)
        {
            var selectedComputed = IsComputedShape(_selectedShape);
            selectedShape = new DrawingCanvasSceneShape(
                _selectedShape,
                selectedComputed ? ComputedStroke : SelectedStroke,
                GetShapeStrokeThickness(_selectedShape) + 1.5,
                (selectedComputed && UseDashedComputedStroke) ? ComputedDash : null,
                selectedComputed);
            drawSelectedHandles = !selectedComputed;
        }

        return new DrawingCanvasSceneSnapshot
        {
            Shapes = shapes,
            HoverShape = hoverShape,
            SelectedShape = selectedShape,
            DrawSelectedHandles = drawSelectedHandles,
            PreviewShape = _previewShape,
            PreviewThickness = _previewShape is null ? 0d : GetShapeStrokeThickness(_previewShape),
        };
    }

    private void InvalidateScene()
    {
        _isSceneSnapshotDirty = true;
        InvalidateVisual();
    }

    private IDrawingCanvasRenderer CreateRenderer()
    {
        if (!UseDebugOverlayRenderer)
            return new DefaultDrawingCanvasRenderer();

        return new DebugOverlayDrawingCanvasRenderer(new DefaultDrawingCanvasRenderer());
    }

    private IDrawingCanvasRenderBackend CreateRenderBackend()
        => RenderBackendKind switch
        {
            DrawingCanvasRenderBackendKind.Immediate => new ImmediateDrawingCanvasRenderBackend(),
            DrawingCanvasRenderBackendKind.CulledImmediate => new CulledImmediateDrawingCanvasRenderBackend(),
            _ => new ImmediateDrawingCanvasRenderBackend(),
        };

    private sealed class ActionCommand(Action execute) : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => execute();
    }
}
