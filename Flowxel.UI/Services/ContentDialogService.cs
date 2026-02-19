using Avalonia.Controls;
using Avalonia.Media;
using Flowxel.UI.Controls;

namespace Flowxel.UI.Services;

public class ContentDialogService : IContentDialogService
{
    private ContentDialog? _host;

    public void RegisterHost(ContentDialog dialog)
    {
        _host = dialog;
    }

    public async Task<DialogResult> ShowMessageAsync(string title, string message, string closeButtonText = "OK")
    {
        return await ShowAsync(dialog =>
        {
            dialog.Title = title;
            dialog.Content = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap };
            dialog.CloseButtonText = closeButtonText;
        });
    }

    public async Task<DialogResult> ShowAsync(Action<ContentDialog> configure)
    {
        if (_host is null)
            throw new InvalidOperationException("ContentDialog host has not been registered. Call RegisterHost first.");

        ResetDialog(_host);
        configure(_host);
        return await _host.ShowAsync();
    }

    public async Task HideAsync()
    {
        if (_host is not null)
            await _host.HideAsync();
    }

    private static void ResetDialog(ContentDialog dialog)
    {
        dialog.Title = null;
        dialog.Content = null;
        dialog.PrimaryButtonText = null;
        dialog.SecondaryButtonText = null;
        dialog.CloseButtonText = null;
        dialog.PrimaryButtonCommand = null;
        dialog.SecondaryButtonCommand = null;
        dialog.CloseButtonCommand = null;
        dialog.IsPrimaryButtonEnabled = true;
        dialog.IsSecondaryButtonEnabled = true;
        dialog.IsCloseButtonEnabled = true;
        dialog.IsLightDismissEnabled = true;
        dialog.DefaultButton = DefaultButton.None;
    }
}
