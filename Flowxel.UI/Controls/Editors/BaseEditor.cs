using Avalonia;
using Avalonia.Controls;

namespace Flowxel.UI.Controls.Editors;

public class BaseEditor : TextBox
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<BaseEditor, string?>(nameof(Title));

    public static readonly StyledProperty<string?> UnitProperty =
        AvaloniaProperty.Register<BaseEditor, string?>(nameof(Unit));

    public static readonly StyledProperty<object?> ActionContentProperty =
        AvaloniaProperty.Register<BaseEditor, object?>(nameof(ActionContent));

    public static readonly StyledProperty<bool> HasValidationErrorProperty =
        AvaloniaProperty.Register<BaseEditor, bool>(nameof(HasValidationError));

    public static readonly StyledProperty<string?> ValidationErrorMessageProperty =
        AvaloniaProperty.Register<BaseEditor, string?>(nameof(ValidationErrorMessage));

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Unit
    {
        get => GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
    }

    public object? ActionContent
    {
        get => GetValue(ActionContentProperty);
        set => SetValue(ActionContentProperty, value);
    }

    public bool HasValidationError
    {
        get => GetValue(HasValidationErrorProperty);
        set => SetValue(HasValidationErrorProperty, value);
    }

    public string? ValidationErrorMessage
    {
        get => GetValue(ValidationErrorMessageProperty);
        set => SetValue(ValidationErrorMessageProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(BaseEditor);

    static BaseEditor()
    {
        HasValidationErrorProperty.Changed.AddClassHandler<BaseEditor>((editor, _) =>
        {
            editor.PseudoClasses.Set(":error", editor.HasValidationError);
        });
    }
}
