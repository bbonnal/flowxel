using Flowxel.UI.Controls;

namespace Flowxel.UI.Services;

public class OverlayService : IOverlayService
{
    private OverlayControl? _host;

    public void RegisterHost(OverlayControl overlay)
    {
        _host = overlay;
    }

    public Task ShowAsync(Action<OverlayControl>? configure = null)
    {
        if (_host is null)
            throw new InvalidOperationException("OverlayControl host has not been registered. Call RegisterHost first.");

        ResetOverlay(_host);
        configure?.Invoke(_host);
        _host.IsOpen = true;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Action<OverlayControl> configure)
    {
        if (_host is null)
            throw new InvalidOperationException("OverlayControl host has not been registered. Call RegisterHost first.");

        configure(_host);
        return Task.CompletedTask;
    }

    public Task HideAsync()
    {
        if (_host is not null)
            _host.IsOpen = false;
        return Task.CompletedTask;
    }

    private static void ResetOverlay(OverlayControl overlay)
    {
        overlay.Title = null;
        overlay.Message = null;
        overlay.Content = null;
        overlay.Progress = 0;
        overlay.IsIndeterminate = true;
    }
}
