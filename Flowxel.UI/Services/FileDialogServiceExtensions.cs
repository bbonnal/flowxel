using Avalonia.Platform.Storage;
using System.IO;

namespace Flowxel.UI.Services;

public static class FileDialogServiceExtensions
{
    public static async Task<IEnumerable<string>> ShowOpenFileDialogAsync(
        this IFileDialogService service,
        string? title = null,
        bool allowMultiple = false,
        string? suggestedStartLocation = null,
        string? suggestedFileName = null,
        IReadOnlyList<FilePickerFileType>? fileTypeFilter = null)
    {
        if (service.StorageProvider is null)
            throw new InvalidOperationException("Storage provider is not set");

        var options = new FilePickerOpenOptions
        {
            AllowMultiple = allowMultiple,
            FileTypeFilter = fileTypeFilter
        };

        if (title is not null)
            options.Title = title;

        if (suggestedStartLocation is not null)
            options.SuggestedStartLocation =
                await service.StorageProvider.TryGetFolderFromPathAsync(new Uri(Path.GetFullPath(suggestedStartLocation)));

        if (suggestedFileName is not null)
            options.SuggestedFileName = suggestedFileName;

        var files = await service.ShowOpenFileDialogAsync(options);
        return files.Select(file => ResolvePath(file.Path)).OfType<string>();
    }

    public static async Task<string?> ShowSaveFileDialogAsync(
        this IFileDialogService service,
        string? title = null,
        string? suggestedStartLocation = null,
        string? suggestedFileName = null,
        string? defaultExtension = null,
        bool showOverwritePrompt = true,
        IReadOnlyList<FilePickerFileType>? fileTypeChoices = null)
    {
        if (service.StorageProvider is null)
            throw new InvalidOperationException("Storage provider is not set");

        var options = new FilePickerSaveOptions
        {
            ShowOverwritePrompt = showOverwritePrompt,
            FileTypeChoices = fileTypeChoices
        };

        if (title is not null)
            options.Title = title;

        if (suggestedStartLocation is not null)
            options.SuggestedStartLocation =
                await service.StorageProvider.TryGetFolderFromPathAsync(new Uri(Path.GetFullPath(suggestedStartLocation)));

        if (suggestedFileName is not null)
            options.SuggestedFileName = suggestedFileName;

        if (defaultExtension is not null)
            options.DefaultExtension = defaultExtension;

        var file = await service.ShowSaveFileDialogAsync(options);
        return file is null ? null : ResolvePath(file.Path);
    }

    private static string ResolvePath(Uri uri)
        => uri.IsFile ? uri.LocalPath : uri.ToString();
}
