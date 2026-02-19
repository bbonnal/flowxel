using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Controls;
using Flowxel.UI.Controls.Docking;
using Flowxel.UITester.ViewModels;

namespace Flowxel.UITester.Views;

public partial class DockingCanvasTestingPageView : UserControl
{
    private readonly Dictionary<Guid, DockPane> _paneByCanvasId = [];
    private DockingCanvasTestingPageViewModel? _currentViewModel;

    public DockingCanvasTestingPageView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        DockHost.FocusedPaneChanged += OnDockHostFocusedPaneChanged;
        DockHost.PaneClosed += OnDockHostPaneClosed;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_currentViewModel is not null)
            _currentViewModel.Canvases.CollectionChanged -= OnCanvasCollectionChanged;
        _currentViewModel = null;

        if (DataContext is not DockingCanvasTestingPageViewModel vm)
            return;

        _currentViewModel = vm;
        vm.Canvases.CollectionChanged += OnCanvasCollectionChanged;
        RebuildDockPanes(vm);
    }

    private void OnCanvasCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (DataContext is not DockingCanvasTestingPageViewModel)
            return;

        if (e.NewItems is not null)
        {
            foreach (var item in e.NewItems.OfType<CanvasDocumentViewModel>())
                AddPaneForCanvas(item);
        }

        if (e.OldItems is not null)
        {
            foreach (var item in e.OldItems.OfType<CanvasDocumentViewModel>())
                RemovePaneForCanvas(item.Id);
        }
    }

    private void RebuildDockPanes(DockingCanvasTestingPageViewModel vm)
    {
        _paneByCanvasId.Clear();
        DockHost.Panes.Clear();

        foreach (var canvas in vm.Canvases)
            AddPaneForCanvas(canvas);
    }

    private void AddPaneForCanvas(CanvasDocumentViewModel canvas)
    {
        if (_paneByCanvasId.ContainsKey(canvas.Id))
            return;

        var pane = new DockPane
        {
            Header = canvas.Title,
            PaneContent = new CanvasDocumentView
            {
                DataContext = canvas
            },
            Tag = canvas.Id
        };

        _paneByCanvasId[canvas.Id] = pane;
        DockHost.AddPane(pane);
    }

    private void RemovePaneForCanvas(Guid canvasId)
    {
        if (!_paneByCanvasId.TryGetValue(canvasId, out var pane))
            return;

        _paneByCanvasId.Remove(canvasId);
        DockHost.ClosePane(pane);
    }

    private void OnDockHostFocusedPaneChanged(object? sender, DockPane? pane)
    {
        if (DataContext is not DockingCanvasTestingPageViewModel vm || pane?.Tag is not Guid canvasId)
            return;

        vm.FocusCanvas(canvasId);
    }

    private void OnDockHostPaneClosed(object? sender, DockPane pane)
    {
        if (DataContext is not DockingCanvasTestingPageViewModel vm || pane.Tag is not Guid canvasId)
            return;

        vm.RemoveCanvas(canvasId);
    }
}
