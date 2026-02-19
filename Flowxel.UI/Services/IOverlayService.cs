using Flowxel.UI.Controls;

namespace Flowxel.UI.Services;

public interface IOverlayService
{
    void RegisterHost(OverlayControl overlay);
    // TODO: What is this action here ?
    Task ShowAsync(Action<OverlayControl>? configure = null);
    // TODO: What is this action here ?
    void Update(Action<OverlayControl> configure);
    Task HideAsync();
}
