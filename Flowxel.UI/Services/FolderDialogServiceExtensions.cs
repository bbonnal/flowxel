using System.IO;

namespace Flowxel.UI.Services;

public static class FolderDialogServiceExtensions
{
    public static async Task<IEnumerable<string>> ShowOpenFolderDialogAsync(
        this IFolderDialogService service,
        string? title = null,
        bool allowMultiple = false,
        string? suggestedStartLocation = null,
        string? suggestedFileName = null)
    {
        if (service.StorageProvider is null)
            throw new InvalidOperationException("Storage provider is not set");

        var options = new Avalonia.Platform.Storage.FolderPickerOpenOptions
        {
            AllowMultiple = allowMultiple
        };

        if (title is not null)
            options.Title = title;

        if (suggestedStartLocation is not null)
            options.SuggestedStartLocation =
                await service.StorageProvider.TryGetFolderFromPathAsync(new Uri(Path.GetFullPath(suggestedStartLocation)));

        if (suggestedFileName is not null)
            options.SuggestedFileName = suggestedFileName;

        var folders = await service.ShowOpenFolderDialogAsync(options);
        return folders.Select(folder => ResolvePath(folder.Path));
    }

    private static string ResolvePath(Uri uri)
        => uri.IsFile ? uri.LocalPath : uri.ToString();
}
