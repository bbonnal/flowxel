using Avalonia.Controls.Primitives;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia;

namespace Flowxel.UI.Controls;

public enum InfoBarSeverity
{
    Info,
    Success,
    Warning,
    Error
}

public class InfoBarControl : ContentControl
{
    private Button? _closeButtonPart;
    private TaskCompletionSource? _pendingShowTaskCompletionSource;

    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<InfoBarControl, string?>(nameof(Title));

    public static readonly StyledProperty<string?> MessageProperty =
        AvaloniaProperty.Register<InfoBarControl, string?>(nameof(Message));

    public static readonly StyledProperty<InfoBarSeverity> SeverityProperty =
        AvaloniaProperty.Register<InfoBarControl, InfoBarSeverity>(nameof(Severity), InfoBarSeverity.Info);

    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<InfoBarControl, bool>(nameof(IsOpen), false);

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public InfoBarSeverity Severity
    {
        get => GetValue(SeverityProperty);
        set => SetValue(SeverityProperty, value);
    }

    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public event EventHandler? Closed;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        UnregisterTemplatePartHandlers();

        _closeButtonPart = e.NameScope.Find<Button>("PART_CloseButton");
        if (_closeButtonPart is not null)
            _closeButtonPart.Click += OnCloseButtonClick;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        UnregisterTemplatePartHandlers();
    }

    private void OnCloseButtonClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    public void Close()
    {
        if (!IsOpen)
            return;

        IsOpen = false;
        _pendingShowTaskCompletionSource?.TrySetResult();
        _pendingShowTaskCompletionSource = null;
        Closed?.Invoke(this, EventArgs.Empty);
    }

    public Task CloseAsync()
    {
        if (!IsOpen)
            return Task.CompletedTask;

        var pendingTask = _pendingShowTaskCompletionSource?.Task;
        Close();
        return pendingTask ?? Task.CompletedTask;
    }

    public Task ShowAsync()
    {
        if (_pendingShowTaskCompletionSource is not null)
            return _pendingShowTaskCompletionSource.Task;

        _pendingShowTaskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        IsOpen = true;

        return _pendingShowTaskCompletionSource.Task;
    }

    private void UnregisterTemplatePartHandlers()
    {
        if (_closeButtonPart is null)
            return;

        _closeButtonPart.Click -= OnCloseButtonClick;
        _closeButtonPart = null;
    }
}
