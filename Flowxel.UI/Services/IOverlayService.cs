using Flowxel.UI.Controls;

namespace Flowxel.UI.Services;

public interface IOverlayService
{
    void RegisterHost(OverlayControl overlay);
    Task ShowAsync(Action<OverlayControl>? configure = null);
    Task UpdateAsync(Action<OverlayControl> configure);
    Task HideAsync();
}
