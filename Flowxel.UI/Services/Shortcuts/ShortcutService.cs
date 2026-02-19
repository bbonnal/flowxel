using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace Flowxel.UI.Services.Shortcuts;

public sealed class ShortcutService : IShortcutService
{
    private readonly Dictionary<Control, ShortcutBindingHandle> _bindingsByScope = [];

    public IDisposable Bind(Control scope, IEnumerable<ShortcutDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(definitions);

        if (_bindingsByScope.TryGetValue(scope, out var existing))
        {
            existing.Dispose();
            _bindingsByScope.Remove(scope);
        }

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

        if (createdBindings.Count == 0)
            return NoOpDisposable.Instance;

        var handle = new ShortcutBindingHandle(scope, createdBindings, () => _bindingsByScope.Remove(scope));
        _bindingsByScope[scope] = handle;
        return handle;
    }

    private sealed class ShortcutBindingHandle : IDisposable
    {
        private readonly Control _scope;
        private readonly IReadOnlyList<KeyBinding> _bindings;
        private readonly Action _onDisposed;
        private bool _disposed;

        public ShortcutBindingHandle(Control scope, IReadOnlyList<KeyBinding> bindings, Action onDisposed)
        {
            _scope = scope;
            _bindings = bindings;
            _onDisposed = onDisposed;
            _scope.DetachedFromVisualTree += OnDetachedFromVisualTree;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _scope.DetachedFromVisualTree -= OnDetachedFromVisualTree;
            foreach (var binding in _bindings)
            {
                _scope.KeyBindings.Remove(binding);
            }

            _onDisposed();
        }

        private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            Dispose();
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
            if (!IsFocusWithinScope())
                return false;

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

        private bool IsFocusWithinScope()
        {
            var topLevel = TopLevel.GetTopLevel(_scope);
            var focused = topLevel?.FocusManager?.GetFocusedElement() as Visual;
            return focused is not null &&
                   (ReferenceEquals(focused, _scope) ||
                    focused.GetVisualAncestors().Any(v => ReferenceEquals(v, _scope)));
        }
    }

    private sealed class NoOpDisposable : IDisposable
    {
        public static readonly NoOpDisposable Instance = new();

        private NoOpDisposable()
        {
        }

        public void Dispose()
        {
        }
    }
}
