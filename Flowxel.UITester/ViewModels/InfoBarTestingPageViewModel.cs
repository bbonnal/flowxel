using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Flowxel.UI.Controls;
using Flowxel.UI.Services;

namespace Flowxel.UITester.ViewModels;

public class InfoBarTestingPageViewModel : ViewModelBase
{
    public InfoBarTestingPageViewModel(IInfoBarService infoBarService)
    {
        _infoBarService = infoBarService;
        ShowInfoCommand = new AsyncRelayCommand(ShowInfo);
        ShowSuccessCommand = new AsyncRelayCommand(ShowSuccess);
        ShowWarningCommand = new AsyncRelayCommand(ShowWarning);
        ShowErrorCommand = new AsyncRelayCommand(ShowError);
        CloseCommand = new AsyncRelayCommand(Close);
    }

    private readonly IInfoBarService _infoBarService;

    public string? LastResult
    {
        get;
        set => SetProperty(ref field, value);
    }

    public IAsyncRelayCommand ShowInfoCommand { get; }
    public IAsyncRelayCommand ShowSuccessCommand { get; }
    public IAsyncRelayCommand ShowWarningCommand { get; }
    public IAsyncRelayCommand ShowErrorCommand { get; }
    public IAsyncRelayCommand CloseCommand { get; }

    private async Task ShowInfo()
    {
        await _infoBarService.ShowAsync(o =>
        {
            o.Title = "Information";
            o.Message = "This is an informational message.";
            o.Severity = InfoBarSeverity.Info;
        });
        LastResult = "Info closed";
    }

    private async Task ShowSuccess()
    {
        await _infoBarService.ShowAsync(o =>
        {
            o.Title = "Success";
            o.Message = "The operation completed successfully.";
            o.Severity = InfoBarSeverity.Success;
        });
        LastResult = "Success closed";
    }

    private async Task ShowWarning()
    {
        await _infoBarService.ShowAsync(o =>
        {
            o.Title = "Warning";
            o.Message = "Something might need your attention.";
            o.Severity = InfoBarSeverity.Warning;
        });
        LastResult = "Warning closed";
    }

    private async Task ShowError()
    {
        await _infoBarService.ShowAsync(o =>
        {
            o.Title = "Error";
            o.Message = "An error has occurred during the operation.";
            o.Severity = InfoBarSeverity.Error;
        });
        LastResult = "Error closed";
    }

    private async Task Close()
    {
        await _infoBarService.HideAsync();
    }
}
