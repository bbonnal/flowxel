using Avalonia;
using Avalonia.Interactivity;

namespace Flowxel.UI.Controls.Editors;

public abstract class ByteArrayEditor : MultiLineTextEditor
{
    public static readonly StyledProperty<byte[]?> ValueProperty =
        AvaloniaProperty.Register<ByteArrayEditor, byte[]?>(nameof(Value),
            defaultBindingMode: global::Avalonia.Data.BindingMode.TwoWay);

    private bool _isSyncing;

    public byte[]? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(MultiLineTextEditor);

    protected abstract bool TryParse(string? text, out byte[] result);
    protected abstract string FormatValue(byte[] value);

    static ByteArrayEditor()
    {
        ValueProperty.Changed.AddClassHandler<ByteArrayEditor>((editor, _) =>
        {
            editor.SyncTextFromValue();
        });

        TextProperty.Changed.AddClassHandler<ByteArrayEditor>((editor, _) =>
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
            Text = Value is { } v ? FormatValue(v) : null;
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
            _isSyncing = true;
            try
            {
                Value = parsed;
                Text = FormatValue(parsed);
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
