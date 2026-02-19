using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Data;

namespace Flowxel.UI.Controls.Processing;

public sealed class ResourcePoolControl : TemplatedControl
{
    public static readonly StyledProperty<IReadOnlyList<ResourceEntryDescriptor>?> ResourceItemsProperty =
        AvaloniaProperty.Register<ResourcePoolControl, IReadOnlyList<ResourceEntryDescriptor>?>(nameof(ResourceItems));

    public static readonly StyledProperty<ResourceEntryDescriptor?> SelectedResourceProperty =
        AvaloniaProperty.Register<ResourcePoolControl, ResourceEntryDescriptor?>(
            nameof(SelectedResource),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<ICommand?> ViewResourceCommandProperty =
        AvaloniaProperty.Register<ResourcePoolControl, ICommand?>(nameof(ViewResourceCommand));

    public static readonly StyledProperty<ICommand?> ClearResourcesCommandProperty =
        AvaloniaProperty.Register<ResourcePoolControl, ICommand?>(nameof(ClearResourcesCommand));

    public IReadOnlyList<ResourceEntryDescriptor>? ResourceItems
    {
        get => GetValue(ResourceItemsProperty);
        set => SetValue(ResourceItemsProperty, value);
    }

    public ResourceEntryDescriptor? SelectedResource
    {
        get => GetValue(SelectedResourceProperty);
        set => SetValue(SelectedResourceProperty, value);
    }

    public ICommand? ViewResourceCommand
    {
        get => GetValue(ViewResourceCommandProperty);
        set => SetValue(ViewResourceCommandProperty, value);
    }

    public ICommand? ClearResourcesCommand
    {
        get => GetValue(ClearResourcesCommandProperty);
        set => SetValue(ClearResourcesCommandProperty, value);
    }
}
