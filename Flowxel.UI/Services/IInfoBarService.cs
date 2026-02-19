using Flowxel.UI.Controls;

namespace Flowxel.UI.Services;

public interface IInfoBarService
{
    void RegisterHost(InfoBarControl infoBar);
    Task ShowAsync(Action<InfoBarControl>? configure = null);
    Task HideAsync();
}
