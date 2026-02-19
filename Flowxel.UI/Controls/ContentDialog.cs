using Avalonia.Controls.Primitives;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia;
using System.Windows.Input;

namespace Flowxel.UI.Controls;

public enum DialogResult
{
    None,
    Primary,
    Secondary,
    Close
}

public class ContentDialog : ContentControl
{
    private Border? _overlayPart;
    private Button? _primaryButtonPart;
    private Button? _secondaryButtonPart;
    private Button? _closeButtonPart;
    private TaskCompletionSource<DialogResult>? _pendingShowTaskCompletionSource;

    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<ContentDialog, string?>(nameof(Title));

    public static readonly StyledProperty<string?> PrimaryButtonTextProperty =
        AvaloniaProperty.Register<ContentDialog, string?>(nameof(PrimaryButtonText));

    public static readonly StyledProperty<string?> SecondaryButtonTextProperty =
        AvaloniaProperty.Register<ContentDialog, string?>(nameof(SecondaryButtonText));

    public static readonly StyledProperty<string?> CloseButtonTextProperty =
        AvaloniaProperty.Register<ContentDialog, string?>(nameof(CloseButtonText));

    public static readonly StyledProperty<ICommand?> PrimaryButtonCommandProperty =
        AvaloniaProperty.Register<ContentDialog, ICommand?>(nameof(PrimaryButtonCommand));

    public static readonly StyledProperty<ICommand?> SecondaryButtonCommandProperty =
        AvaloniaProperty.Register<ContentDialog, ICommand?>(nameof(SecondaryButtonCommand));

    public static readonly StyledProperty<ICommand?> CloseButtonCommandProperty =
        AvaloniaProperty.Register<ContentDialog, ICommand?>(nameof(CloseButtonCommand));

    public static readonly StyledProperty<bool> IsPrimaryButtonEnabledProperty =
        AvaloniaProperty.Register<ContentDialog, bool>(nameof(IsPrimaryButtonEnabled), true);

    public static readonly StyledProperty<bool> IsSecondaryButtonEnabledProperty =
        AvaloniaProperty.Register<ContentDialog, bool>(nameof(IsSecondaryButtonEnabled), true);

    public static readonly StyledProperty<bool> IsCloseButtonEnabledProperty =
        AvaloniaProperty.Register<ContentDialog, bool>(nameof(IsCloseButtonEnabled), true);

    public static readonly StyledProperty<DefaultButton> DefaultButtonProperty =
        AvaloniaProperty.Register<ContentDialog, DefaultButton>(nameof(DefaultButton), DefaultButton.None);

    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<ContentDialog, bool>(nameof(IsOpen), false);

    public static readonly StyledProperty<IBrush?> OverlayBrushProperty =
        AvaloniaProperty.Register<ContentDialog, IBrush?>(
            nameof(OverlayBrush),
            new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)));

    public static readonly StyledProperty<bool> IsLightDismissEnabledProperty =
        AvaloniaProperty.Register<ContentDialog, bool>(nameof(IsLightDismissEnabled), true);

    public static readonly StyledProperty<DialogResult> DialogResultProperty =
        AvaloniaProperty.Register<ContentDialog, DialogResult>(nameof(DialogResult), DialogResult.None);

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? PrimaryButtonText
    {
        get => GetValue(PrimaryButtonTextProperty);
        set => SetValue(PrimaryButtonTextProperty, value);
    }

    public string? SecondaryButtonText
    {
        get => GetValue(SecondaryButtonTextProperty);
        set => SetValue(SecondaryButtonTextProperty, value);
    }

    public string? CloseButtonText
    {
        get => GetValue(CloseButtonTextProperty);
        set => SetValue(CloseButtonTextProperty, value);
    }

    public ICommand? PrimaryButtonCommand
    {
        get => GetValue(PrimaryButtonCommandProperty);
        set => SetValue(PrimaryButtonCommandProperty, value);
    }

    public ICommand? SecondaryButtonCommand
    {
        get => GetValue(SecondaryButtonCommandProperty);
        set => SetValue(SecondaryButtonCommandProperty, value);
    }

    public ICommand? CloseButtonCommand
    {
        get => GetValue(CloseButtonCommandProperty);
        set => SetValue(CloseButtonCommandProperty, value);
    }

    public bool IsPrimaryButtonEnabled
    {
        get => GetValue(IsPrimaryButtonEnabledProperty);
        set => SetValue(IsPrimaryButtonEnabledProperty, value);
    }

    public bool IsSecondaryButtonEnabled
    {
        get => GetValue(IsSecondaryButtonEnabledProperty);
        set => SetValue(IsSecondaryButtonEnabledProperty, value);
    }

    public bool IsCloseButtonEnabled
    {
        get => GetValue(IsCloseButtonEnabledProperty);
        set => SetValue(IsCloseButtonEnabledProperty, value);
    }

    public DefaultButton DefaultButton
    {
        get => GetValue(DefaultButtonProperty);
        set => SetValue(DefaultButtonProperty, value);
    }

    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public IBrush? OverlayBrush
    {
        get => GetValue(OverlayBrushProperty);
        set => SetValue(OverlayBrushProperty, value);
    }

    public bool IsLightDismissEnabled
    {
        get => GetValue(IsLightDismissEnabledProperty);
        set => SetValue(IsLightDismissEnabledProperty, value);
    }

    public DialogResult DialogResult
    {
        get => GetValue(DialogResultProperty);
        set => SetValue(DialogResultProperty, value);
    }

    public event EventHandler<DialogResult>? Closed;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        UnregisterTemplatePartHandlers();

        _overlayPart = e.NameScope.Find<Border>("PART_Overlay");
        if (_overlayPart is not null)
            _overlayPart.PointerPressed += OnOverlayPointerPressed;

        _primaryButtonPart = e.NameScope.Find<Button>("PART_PrimaryButton");
        if (_primaryButtonPart is not null)
            _primaryButtonPart.Click += OnPrimaryButtonClick;

        _secondaryButtonPart = e.NameScope.Find<Button>("PART_SecondaryButton");
        if (_secondaryButtonPart is not null)
            _secondaryButtonPart.Click += OnSecondaryButtonClick;

        _closeButtonPart = e.NameScope.Find<Button>("PART_CloseButton");
        if (_closeButtonPart is not null)
            _closeButtonPart.Click += OnCloseButtonClick;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (!IsOpen)
            return;

        if (e.Key == Key.Escape)
        {
            if (IsCloseButtonEnabled && !string.IsNullOrWhiteSpace(CloseButtonText))
                OnCloseButtonClick(this, new RoutedEventArgs());
            else
                CloseDialog(DialogResult.None);
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            if (!TryInvokeDefaultButton())
                OnPrimaryButtonClick(this, new RoutedEventArgs());

            e.Handled = true;
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        UnregisterTemplatePartHandlers();
    }

    private void OnOverlayPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!IsLightDismissEnabled)
            return;

        CloseDialog(DialogResult.None);
    }

    private void OnPrimaryButtonClick(object? sender, RoutedEventArgs e)
    {
        if (!IsPrimaryButtonEnabled || string.IsNullOrWhiteSpace(PrimaryButtonText))
            return;

        if (PrimaryButtonCommand is not null && !PrimaryButtonCommand.CanExecute(null))
            return;

        PrimaryButtonCommand?.Execute(null);
        CloseDialog(DialogResult.Primary);
    }

    private void OnSecondaryButtonClick(object? sender, RoutedEventArgs e)
    {
        if (!IsSecondaryButtonEnabled || string.IsNullOrWhiteSpace(SecondaryButtonText))
            return;

        if (SecondaryButtonCommand is not null && !SecondaryButtonCommand.CanExecute(null))
            return;

        SecondaryButtonCommand?.Execute(null);
        CloseDialog(DialogResult.Secondary);
    }

    private void OnCloseButtonClick(object? sender, RoutedEventArgs e)
    {
        if (!IsCloseButtonEnabled || string.IsNullOrWhiteSpace(CloseButtonText))
            return;

        if (CloseButtonCommand is not null && !CloseButtonCommand.CanExecute(null))
            return;

        CloseButtonCommand?.Execute(null);
        CloseDialog(DialogResult.Close);
    }

    private void CloseDialog(DialogResult result)
    {
        if (!IsOpen)
            return;

        DialogResult = result;
        IsOpen = false;
        _pendingShowTaskCompletionSource?.TrySetResult(result);
        _pendingShowTaskCompletionSource = null;
        Closed?.Invoke(this, result);
    }

    public Task HideAsync()
    {
        if (!IsOpen)
            return Task.CompletedTask;

        var pendingTask = _pendingShowTaskCompletionSource?.Task;
        CloseDialog(DialogResult.None);
        return pendingTask ?? Task.CompletedTask;
    }

    public Task<DialogResult> ShowAsync()
    {
        if (_pendingShowTaskCompletionSource is not null)
            return _pendingShowTaskCompletionSource.Task;

        _pendingShowTaskCompletionSource = new TaskCompletionSource<DialogResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        DialogResult = DialogResult.None;
        IsOpen = true;
        FocusDefaultButton();

        return _pendingShowTaskCompletionSource.Task;
    }

    private void UnregisterTemplatePartHandlers()
    {
        if (_overlayPart is not null)
        {
            _overlayPart.PointerPressed -= OnOverlayPointerPressed;
            _overlayPart = null;
        }

        if (_primaryButtonPart is not null)
        {
            _primaryButtonPart.Click -= OnPrimaryButtonClick;
            _primaryButtonPart = null;
        }

        if (_secondaryButtonPart is not null)
        {
            _secondaryButtonPart.Click -= OnSecondaryButtonClick;
            _secondaryButtonPart = null;
        }

        if (_closeButtonPart is not null)
        {
            _closeButtonPart.Click -= OnCloseButtonClick;
            _closeButtonPart = null;
        }
    }

    private bool TryInvokeDefaultButton()
    {
        return DefaultButton switch
        {
            DefaultButton.Primary => TryInvokeButton(_primaryButtonPart, OnPrimaryButtonClick),
            DefaultButton.Secondary => TryInvokeButton(_secondaryButtonPart, OnSecondaryButtonClick),
            DefaultButton.Close => TryInvokeButton(_closeButtonPart, OnCloseButtonClick),
            _ => false
        };
    }

    private static bool TryInvokeButton(Button? button, EventHandler<RoutedEventArgs> clickHandler)
    {
        if (button is null || !button.IsVisible || !button.IsEnabled)
            return false;

        clickHandler(button, new RoutedEventArgs());
        return true;
    }

    private void FocusDefaultButton()
    {
        Dispatcher.UIThread.Post(() =>
        {
            var button = DefaultButton switch
            {
                DefaultButton.Primary => _primaryButtonPart,
                DefaultButton.Secondary => _secondaryButtonPart,
                DefaultButton.Close => _closeButtonPart,
                _ => _primaryButtonPart
            };

            if (button is not null && button.IsVisible && button.IsEnabled)
                button.Focus();
        }, DispatcherPriority.Loaded);
    }
}

public enum DefaultButton
{
    None,
    Primary,
    Secondary,
    Close
}
