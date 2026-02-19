using Avalonia.Platform.Storage;

namespace Flowxel.UI.Services;

public class FolderDialogService : IFolderDialogService
{
    public IStorageProvider? StorageProvider { get; private set; }

    public void SetStorageProvider(IStorageProvider storageProvider)
        => StorageProvider = storageProvider;

    public async Task<IReadOnlyList<IStorageFolder>> ShowOpenFolderDialogAsync(FolderPickerOpenOptions options)
    {
        if (StorageProvider is null)
            throw new InvalidOperationException("Storage provider is not set");

        return await StorageProvider.OpenFolderPickerAsync(options);
    }
}
