using Avalonia.Controls;

namespace Flowxel.UI.Services.Shortcuts;

public interface IShortcutService
{
    IDisposable Bind(Control scope, IEnumerable<ShortcutDefinition> definitions);
}
