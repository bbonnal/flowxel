using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using Flowxel.UI.Services;

namespace Flowxel.UITester.ViewModels;

public class DialogsTestingPageViewModel : ViewModelBase
{
    private readonly IFileDialogService _fileDialogService;
    private readonly IFolderDialogService _folderDialogService;

    public DialogsTestingPageViewModel(IFileDialogService fileDialogService, IFolderDialogService folderDialogService)
    {
        _fileDialogService = fileDialogService;
        _folderDialogService = folderDialogService;

        OpenSingleFileCommand = new AsyncRelayCommand(OpenSingleFile);
        OpenMultipleFilesCommand = new AsyncRelayCommand(OpenMultipleFiles);
        OpenTextFilesCommand = new AsyncRelayCommand(OpenTextFiles);
        SaveFileCommand = new AsyncRelayCommand(SaveFile);
        SaveFileWithExtensionCommand = new AsyncRelayCommand(SaveFileWithExtension);
        OpenSingleFolderCommand = new AsyncRelayCommand(OpenSingleFolder);
        OpenMultipleFoldersCommand = new AsyncRelayCommand(OpenMultipleFolders);
    }

    public string? FileDialogResult
    {
        get;
        set => SetProperty(ref field, value);
    }

    public string? FolderDialogResult
    {
        get;
        set => SetProperty(ref field, value);
    }

    public IAsyncRelayCommand OpenSingleFileCommand { get; }
    public IAsyncRelayCommand OpenMultipleFilesCommand { get; }
    public IAsyncRelayCommand OpenTextFilesCommand { get; }
    public IAsyncRelayCommand SaveFileCommand { get; }
    public IAsyncRelayCommand SaveFileWithExtensionCommand { get; }
    public IAsyncRelayCommand OpenSingleFolderCommand { get; }
    public IAsyncRelayCommand OpenMultipleFoldersCommand { get; }

    private async Task OpenSingleFile()
    {
        var files = await _fileDialogService.ShowOpenFileDialogAsync(
            title: "Select a file",
            allowMultiple: false);

        FileDialogResult = files.Any() ? $"Selected: {files.First()}" : "No file selected";
    }

    private async Task OpenMultipleFiles()
    {
        var files = (await _fileDialogService.ShowOpenFileDialogAsync(
            title: "Select multiple files",
            allowMultiple: true)).ToList();

        FileDialogResult = files.Any()
            ? $"Selected {files.Count} file(s):\n{string.Join("\n", files)}"
            : "No files selected";
    }

    private async Task OpenTextFiles()
    {
        var textFileFilter = new FilePickerFileType("Text Files")
        {
            Patterns = ["*.txt", "*.md"],
            MimeTypes = ["text/plain", "text/markdown"]
        };

        var files = (await _fileDialogService.ShowOpenFileDialogAsync(
            title: "Select text files",
            allowMultiple: true,
            fileTypeFilter: [textFileFilter, FilePickerFileTypes.All])).ToList();

        FileDialogResult = files.Any()
            ? $"Selected {files.Count} text file(s):\n{string.Join("\n", files)}"
            : "No files selected";
    }

    private async Task SaveFile()
    {
        var file = await _fileDialogService.ShowSaveFileDialogAsync(
            title: "Save file",
            suggestedFileName: "document");

        FileDialogResult = file is not null ? $"Save location: {file}" : "Save cancelled";
    }

    private async Task SaveFileWithExtension()
    {
        var textFileType = new FilePickerFileType("Text File") { Patterns = ["*.txt"] };
        var markdownFileType = new FilePickerFileType("Markdown File") { Patterns = ["*.md"] };

        var file = await _fileDialogService.ShowSaveFileDialogAsync(
            title: "Save document",
            suggestedFileName: "document",
            defaultExtension: "txt",
            fileTypeChoices: [textFileType, markdownFileType, FilePickerFileTypes.All]);

        FileDialogResult = file is not null ? $"Save location: {file}" : "Save cancelled";
    }

    private async Task OpenSingleFolder()
    {
        var folders = (await _folderDialogService.ShowOpenFolderDialogAsync(
            title: "Select a folder",
            allowMultiple: false)).ToList();

        FolderDialogResult = folders.Any() ? $"Selected: {folders.First()}" : "No folder selected";
    }

    private async Task OpenMultipleFolders()
    {
        var folders = (await _folderDialogService.ShowOpenFolderDialogAsync(
            title: "Select multiple folders",
            allowMultiple: true)).ToList();

        FolderDialogResult = folders.Any()
            ? $"Selected {folders.Count} folder(s):\n{string.Join("\n", folders)}"
            : "No folders selected";
    }
}
