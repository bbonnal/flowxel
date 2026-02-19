using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Metadata;

namespace Flowxel.UI.Controls.Ribbon;

public class RibbonControl : TemplatedControl
{
    private ListBox? _tabStrip;

    [Content]
    public AvaloniaList<RibbonTab> Tabs { get; } = new();

    public static readonly StyledProperty<RibbonTab?> SelectedTabProperty =
        AvaloniaProperty.Register<RibbonControl, RibbonTab?>(
            nameof(SelectedTab),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<RibbonControl, int>(
            nameof(SelectedIndex),
            defaultValue: -1,
            defaultBindingMode: BindingMode.TwoWay);

    public RibbonTab? SelectedTab
    {
        get => GetValue(SelectedTabProperty);
        set => SetValue(SelectedTabProperty, value);
    }

    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_tabStrip is not null)
            _tabStrip.SelectionChanged -= OnTabStripSelectionChanged;

        _tabStrip = e.NameScope.Find<ListBox>("PART_TabStrip");

        if (_tabStrip is not null)
        {
            _tabStrip.SelectionChanged += OnTabStripSelectionChanged;
            _tabStrip.SelectedItem = SelectedTab;
        }

        if (SelectedTab is null && Tabs.Count > 0)
            SelectedTab = Tabs[0];
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SelectedTabProperty)
        {
            var tab = change.GetNewValue<RibbonTab?>();

            var newIndex = tab is null ? -1 : Tabs.IndexOf(tab);
            if (newIndex != SelectedIndex)
                SelectedIndex = newIndex;

            if (_tabStrip is not null && _tabStrip.SelectedItem != tab)
                _tabStrip.SelectedItem = tab;
        }
        else if (change.Property == SelectedIndexProperty)
        {
            var index = change.GetNewValue<int>();
            var tab = index >= 0 && index < Tabs.Count ? Tabs[index] : null;

            if (SelectedTab != tab)
                SelectedTab = tab;
        }
    }

    private void OnTabStripSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_tabStrip?.SelectedItem is RibbonTab tab)
            SelectedTab = tab;
    }
}
