using Avalonia.Platform.Storage;

namespace Flowxel.UI.Services;

public class FileDialogService : IFileDialogService
{
    public IStorageProvider? StorageProvider { get; private set; }

    public void SetStorageProvider(IStorageProvider storageProvider)
        => StorageProvider = storageProvider;

    public async Task<IReadOnlyList<IStorageFile>> ShowOpenFileDialogAsync(FilePickerOpenOptions options)
    {
        if (StorageProvider is null)
            throw new InvalidOperationException("Storage provider is not set");

        return await StorageProvider.OpenFilePickerAsync(options);
    }

    public async Task<IStorageFile?> ShowSaveFileDialogAsync(FilePickerSaveOptions options)
    {
        if (StorageProvider is null)
            throw new InvalidOperationException("Storage provider is not set");

        return await StorageProvider.SaveFilePickerAsync(options);
    }
}
