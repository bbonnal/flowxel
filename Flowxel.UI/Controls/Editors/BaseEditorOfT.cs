using System.Globalization;
using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Flowxel.UI.Controls.Editors;

#pragma warning disable AVP1002 // Properties are accessed via non-generic concrete types in XAML
public abstract class BaseEditor<T> : BaseEditor where T : struct
{
    public static readonly StyledProperty<T?> ValueProperty =
        AvaloniaProperty.Register<BaseEditor<T>, T?>(nameof(Value), defaultBindingMode: global::Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<string?> FormatStringProperty =
        AvaloniaProperty.Register<BaseEditor<T>, string?>(nameof(FormatString));

    public static readonly StyledProperty<T?> MinimumProperty =
        AvaloniaProperty.Register<BaseEditor<T>, T?>(nameof(Minimum));

    public static readonly StyledProperty<T?> MaximumProperty =
        AvaloniaProperty.Register<BaseEditor<T>, T?>(nameof(Maximum));

    public static readonly StyledProperty<T?> IncrementProperty =
        AvaloniaProperty.Register<BaseEditor<T>, T?>(nameof(Increment));
#pragma warning restore AVP1002

    private bool _isSyncing;

    public T? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string? FormatString
    {
        get => GetValue(FormatStringProperty);
        set => SetValue(FormatStringProperty, value);
    }

    public T? Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public T? Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public T? Increment
    {
        get => GetValue(IncrementProperty);
        set => SetValue(IncrementProperty, value);
    }

    protected abstract bool TryParse(string? text, out T result);
    protected abstract string FormatValue(T value);
    protected abstract bool IsValidInput(string input);
    protected abstract T Clamp(T value);

    static BaseEditor()
    {
        ValueProperty.Changed.AddClassHandler<BaseEditor<T>>((editor, _) =>
        {
            editor.SyncTextFromValue();
        });

        TextProperty.Changed.AddClassHandler<BaseEditor<T>>((editor, _) =>
        {
            editor.SyncValueFromText();
        });
    }

    private void SyncTextFromValue()
    {
        if (_isSyncing) return;
        _isSyncing = true;
        try
        {
            Text = Value.HasValue ? FormatValue(Value.Value) : null;
            HasValidationError = false;
            ValidationErrorMessage = null;
        }
        finally
        {
            _isSyncing = false;
        }
    }

    private void SyncValueFromText()
    {
        if (_isSyncing) return;
        _isSyncing = true;
        try
        {
            if (string.IsNullOrEmpty(Text))
            {
                Value = null;
                HasValidationError = false;
                ValidationErrorMessage = null;
            }
            else if (TryParse(Text, out var parsed))
            {
                Value = parsed;
                HasValidationError = false;
                ValidationErrorMessage = null;
            }
            else
            {
                HasValidationError = true;
                ValidationErrorMessage = "Invalid value";
            }
        }
        finally
        {
            _isSyncing = false;
        }
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        if (e.Text != null && !IsValidInput(e.Text))
        {
            e.Handled = true;
            return;
        }

        base.OnTextInput(e);
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);
        CommitValue();
    }

    private void CommitValue()
    {
        if (string.IsNullOrEmpty(Text))
        {
            Value = null;
            HasValidationError = false;
            ValidationErrorMessage = null;
            return;
        }

        if (TryParse(Text, out var parsed))
        {
            var clamped = Clamp(parsed);
            _isSyncing = true;
            try
            {
                Value = clamped;
                Text = FormatValue(clamped);
                HasValidationError = false;
                ValidationErrorMessage = null;
            }
            finally
            {
                _isSyncing = false;
            }
        }
        else
        {
            HasValidationError = true;
            ValidationErrorMessage = "Invalid value";
        }
    }
}
