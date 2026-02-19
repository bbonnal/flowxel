using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Data;

namespace Flowxel.UI.Controls.Processing;

public sealed class ProcessControl : TemplatedControl
{
    public static readonly StyledProperty<IReadOnlyList<ProcessNodeDescriptor>?> NodesProperty =
        AvaloniaProperty.Register<ProcessControl, IReadOnlyList<ProcessNodeDescriptor>?>(nameof(Nodes));

    public static readonly StyledProperty<IReadOnlyList<ProcessLinkDescriptor>?> LinksProperty =
        AvaloniaProperty.Register<ProcessControl, IReadOnlyList<ProcessLinkDescriptor>?>(nameof(Links));

    public static readonly StyledProperty<ProcessNodeDescriptor?> SelectedNodeProperty =
        AvaloniaProperty.Register<ProcessControl, ProcessNodeDescriptor?>(
            nameof(SelectedNode),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<ProcessNodeDescriptor?> SelectedFromOperationProperty =
        AvaloniaProperty.Register<ProcessControl, ProcessNodeDescriptor?>(
            nameof(SelectedFromOperation),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<ProcessNodeDescriptor?> SelectedToOperationProperty =
        AvaloniaProperty.Register<ProcessControl, ProcessNodeDescriptor?>(
            nameof(SelectedToOperation),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IReadOnlyList<ProcessPortDescriptor>?> AvailableOutputPortsProperty =
        AvaloniaProperty.Register<ProcessControl, IReadOnlyList<ProcessPortDescriptor>?>(nameof(AvailableOutputPorts));

    public static readonly StyledProperty<IReadOnlyList<ProcessPortDescriptor>?> AvailableInputPortsProperty =
        AvaloniaProperty.Register<ProcessControl, IReadOnlyList<ProcessPortDescriptor>?>(nameof(AvailableInputPorts));

    public static readonly StyledProperty<ProcessPortDescriptor?> SelectedFromPortProperty =
        AvaloniaProperty.Register<ProcessControl, ProcessPortDescriptor?>(
            nameof(SelectedFromPort),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<ProcessPortDescriptor?> SelectedToPortProperty =
        AvaloniaProperty.Register<ProcessControl, ProcessPortDescriptor?>(
            nameof(SelectedToPort),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<ProcessLinkDescriptor?> SelectedLinkProperty =
        AvaloniaProperty.Register<ProcessControl, ProcessLinkDescriptor?>(
            nameof(SelectedLink),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<ICommand?> ConnectPortsCommandProperty =
        AvaloniaProperty.Register<ProcessControl, ICommand?>(nameof(ConnectPortsCommand));

    public static readonly StyledProperty<ICommand?> RemoveConnectionCommandProperty =
        AvaloniaProperty.Register<ProcessControl, ICommand?>(nameof(RemoveConnectionCommand));

    public static readonly StyledProperty<ICommand?> RunProcessCommandProperty =
        AvaloniaProperty.Register<ProcessControl, ICommand?>(nameof(RunProcessCommand));

    public IReadOnlyList<ProcessNodeDescriptor>? Nodes
    {
        get => GetValue(NodesProperty);
        set => SetValue(NodesProperty, value);
    }

    public IReadOnlyList<ProcessLinkDescriptor>? Links
    {
        get => GetValue(LinksProperty);
        set => SetValue(LinksProperty, value);
    }

    public ProcessNodeDescriptor? SelectedNode
    {
        get => GetValue(SelectedNodeProperty);
        set => SetValue(SelectedNodeProperty, value);
    }

    public ProcessNodeDescriptor? SelectedFromOperation
    {
        get => GetValue(SelectedFromOperationProperty);
        set => SetValue(SelectedFromOperationProperty, value);
    }

    public ProcessNodeDescriptor? SelectedToOperation
    {
        get => GetValue(SelectedToOperationProperty);
        set => SetValue(SelectedToOperationProperty, value);
    }

    public IReadOnlyList<ProcessPortDescriptor>? AvailableOutputPorts
    {
        get => GetValue(AvailableOutputPortsProperty);
        set => SetValue(AvailableOutputPortsProperty, value);
    }

    public IReadOnlyList<ProcessPortDescriptor>? AvailableInputPorts
    {
        get => GetValue(AvailableInputPortsProperty);
        set => SetValue(AvailableInputPortsProperty, value);
    }

    public ProcessPortDescriptor? SelectedFromPort
    {
        get => GetValue(SelectedFromPortProperty);
        set => SetValue(SelectedFromPortProperty, value);
    }

    public ProcessPortDescriptor? SelectedToPort
    {
        get => GetValue(SelectedToPortProperty);
        set => SetValue(SelectedToPortProperty, value);
    }

    public ProcessLinkDescriptor? SelectedLink
    {
        get => GetValue(SelectedLinkProperty);
        set => SetValue(SelectedLinkProperty, value);
    }

    public ICommand? ConnectPortsCommand
    {
        get => GetValue(ConnectPortsCommandProperty);
        set => SetValue(ConnectPortsCommandProperty, value);
    }

    public ICommand? RemoveConnectionCommand
    {
        get => GetValue(RemoveConnectionCommandProperty);
        set => SetValue(RemoveConnectionCommandProperty, value);
    }

    public ICommand? RunProcessCommand
    {
        get => GetValue(RunProcessCommandProperty);
        set => SetValue(RunProcessCommandProperty, value);
    }
}
