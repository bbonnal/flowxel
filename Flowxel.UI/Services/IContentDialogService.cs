using Flowxel.UI.Controls;

namespace Flowxel.UI.Services;

public interface IContentDialogService
{
    void RegisterHost(ContentDialog dialog);
    Task<DialogResult> ShowMessageAsync(string title, string message, string closeButtonText = "OK");
    Task<DialogResult> ShowAsync(Action<ContentDialog> configure);
    Task HideAsync();
}
