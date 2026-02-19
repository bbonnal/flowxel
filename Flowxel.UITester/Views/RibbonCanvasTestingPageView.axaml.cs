using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using Flowxel.UI.Services.Shortcuts;

namespace Flowxel.UITester.Views;

public partial class RibbonCanvasTestingPageView : UserControl
{
    private IDisposable? _shortcutBinding;

    public RibbonCanvasTestingPageView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        _shortcutBinding?.Dispose();
        _shortcutBinding = null;

        var services = App.Services;
        if (services is null)
        {
            return;
        }

        if (DataContext is not IShortcutBindingProvider provider)
        {
            return;
        }

        var shortcutService = services.GetService<IShortcutService>();
        if (shortcutService is null)
        {
            return;
        }

        _shortcutBinding = shortcutService.Bind(this, provider.GetShortcutDefinitions());
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _shortcutBinding?.Dispose();
        _shortcutBinding = null;
        base.OnDetachedFromVisualTree(e);
    }
}
