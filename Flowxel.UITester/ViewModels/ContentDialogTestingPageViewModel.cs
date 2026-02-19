using System.Diagnostics;
using System.Threading.Tasks;
using global::Avalonia.Controls;
using global::Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Flowxel.UI.Services;

namespace Flowxel.UITester.ViewModels;

public class ContentDialogTestingPageViewModel : ViewModelBase
{
    public ContentDialogTestingPageViewModel(IContentDialogService dialogService)
    {
        _dialogService = dialogService;
        ShowSimpleDialogCommand = new AsyncRelayCommand(ShowSimpleDialog);
        ShowComplexDialogCommand = new AsyncRelayCommand(ShowComplexDialog);
        ShowPasswordDialogCommand = new AsyncRelayCommand(ShowPasswordDialog);
    }

    private readonly IContentDialogService _dialogService;

    public string? LastResult
    {
        get;
        set => SetProperty(ref field, value);
    }

    public IAsyncRelayCommand ShowSimpleDialogCommand { get; }
    public IAsyncRelayCommand ShowComplexDialogCommand { get; }
    public IAsyncRelayCommand ShowPasswordDialogCommand { get; }

    private async Task ShowSimpleDialog()
    {
        var result = await _dialogService.ShowMessageAsync(
            "Information",
            "This is a simple message dialog. Click OK to close it.",
            "OK");

        LastResult = $"Simple dialog result: {result}";
    }

    private async Task ShowComplexDialog()
    {
        var result = await _dialogService.ShowAsync(dialog =>
        {
            dialog.Title = "Confirm Action";
            dialog.Content = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Are you sure you want to perform this action?",
                        TextWrapping = TextWrapping.Wrap
                    },
                    new TextBlock
                    {
                        Text = "This dialog demonstrates multiple buttons with different results.",
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = new SolidColorBrush(Color.Parse("#888888"))
                    }
                }
            };
            dialog.PrimaryButtonText = "Confirm";
            dialog.SecondaryButtonText = "Maybe Later";
            dialog.CloseButtonText = "Cancel";
        });

        LastResult = $"Complex dialog result: {result}";
    }

    private async Task ShowPasswordDialog()
    {
        var passwordBox = new TextBox
        {
            PasswordChar = '\u2022',
            Watermark = "Enter your password",
            Width = 300
        };

        var result = await _dialogService.ShowAsync(dialog =>
        {
            dialog.Title = "Password Required";
            dialog.Content = new StackPanel
            {
                Spacing = 12,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Please enter your password:",
                        TextWrapping = TextWrapping.Wrap
                    },
                    passwordBox
                }
            };
            dialog.PrimaryButtonText = "OK";
            dialog.CloseButtonText = "Cancel";
        });

        LastResult = result == Flowxel.UI.Controls.DialogResult.Primary
            ? $"Password entered: {passwordBox.Text}"
            : "Password dialog cancelled.";
    }
}