using System.Collections.Generic;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Input;

namespace Flowxel.UI.Services.Shortcuts;

public sealed class ShortcutService : IShortcutService
{
    public IDisposable Bind(Control scope, IEnumerable<ShortcutDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(definitions);

        var createdBindings = new List<KeyBinding>();

        foreach (var definition in definitions)
        {
            if (string.IsNullOrWhiteSpace(definition.Gesture))
            {
                continue;
            }

            KeyGesture keyGesture;
            try
            {
                keyGesture = KeyGesture.Parse(definition.Gesture);
            }
            catch (FormatException)
            {
                continue;
            }

            var command = new GuardedCommand(scope, definition.Command, definition.CommandParameter, definition.AllowWhenTextInputFocused);
            var keyBinding = new KeyBinding
            {
                Gesture = keyGesture,
                Command = command
            };

            if (definition.CommandParameter is not null)
            {
                keyBinding.CommandParameter = definition.CommandParameter;
            }

            scope.KeyBindings.Add(keyBinding);
            createdBindings.Add(keyBinding);
        }

        return new ShortcutBindingHandle(scope, createdBindings);
    }

    private sealed class ShortcutBindingHandle : IDisposable
    {
        private readonly Control _scope;
        private readonly IReadOnlyList<KeyBinding> _bindings;
        private bool _disposed;

        public ShortcutBindingHandle(Control scope, IReadOnlyList<KeyBinding> bindings)
        {
            _scope = scope;
            _bindings = bindings;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            foreach (var binding in _bindings)
            {
                _scope.KeyBindings.Remove(binding);
            }
        }
    }

    private sealed class GuardedCommand : ICommand
    {
        private readonly Control _scope;
        private readonly ICommand _inner;
        private readonly object? _parameter;
        private readonly bool _allowWhenTextInputFocused;

        public GuardedCommand(Control scope, ICommand inner, object? parameter, bool allowWhenTextInputFocused)
        {
            _scope = scope;
            _inner = inner;
            _parameter = parameter;
            _allowWhenTextInputFocused = allowWhenTextInputFocused;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => _inner.CanExecuteChanged += value;
            remove => _inner.CanExecuteChanged -= value;
        }

        public bool CanExecute(object? parameter)
        {
            if (!_allowWhenTextInputFocused && IsTextInputFocused())
            {
                return false;
            }

            return _inner.CanExecute(_parameter);
        }

        public void Execute(object? parameter)
        {
            if (!CanExecute(parameter))
            {
                return;
            }

            _inner.Execute(_parameter);
        }

        private bool IsTextInputFocused()
        {
            var topLevel = TopLevel.GetTopLevel(_scope);
            var focused = topLevel?.FocusManager?.GetFocusedElement();
            return focused is TextBox;
        }
    }
}
