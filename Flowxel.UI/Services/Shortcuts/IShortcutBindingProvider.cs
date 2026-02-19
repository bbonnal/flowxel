namespace Flowxel.UI.Services.Shortcuts;

public interface IShortcutBindingProvider
{
    IEnumerable<ShortcutDefinition> GetShortcutDefinitions();
}
