using System.Windows.Input;

namespace Flowxel.UI.Services.Shortcuts;

public sealed record ShortcutDefinition(
    string Gesture,
    ICommand Command,
    object? CommandParameter = null,
    bool AllowWhenTextInputFocused = false,
    string? Description = null);
