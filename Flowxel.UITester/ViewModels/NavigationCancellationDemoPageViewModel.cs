using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Flowxel.UI;
using CommunityToolkit.Mvvm.Input;
using Flowxel.UI.Controls;
using Flowxel.UI.Services;

namespace Flowxel.UITester.ViewModels;

/// <summary>
/// Demonstrates navigation cancellation with INavigationLifecycleAsync.
/// Shows a confirmation dialog when trying to navigate away with unsaved changes.
/// </summary>
public partial class NavigationCancellationDemoPageViewModel : ViewModelBase, INavigationViewModel
{
    private readonly IContentDialogService _dialogService;
    private string _documentText = string.Empty;
    private string _savedText = string.Empty;
    private bool _hasUnsavedChanges;

    public NavigationCancellationDemoPageViewModel(IContentDialogService dialogService)
    {
        _dialogService = dialogService;
    }

    public string DocumentText
    {
        get => _documentText;
        set
        {
            if (SetProperty(ref _documentText, value))
            {
                // Mark as having unsaved changes whenever text changes
                HasUnsavedChanges = _documentText != _savedText;
            }
        }
    }

    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        private set => SetProperty(ref _hasUnsavedChanges, value);
    }

    [RelayCommand]
    private void Save()
    {
        _savedText = _documentText;
        HasUnsavedChanges = false;
    }

    [RelayCommand]
    private void Discard()
    {
        DocumentText = _savedText;
        HasUnsavedChanges = false;
    }

    /// <summary>
    /// Called when user tries to navigate away from this page.
    /// Returns false to cancel navigation if there are unsaved changes and user chooses to keep editing.
    /// </summary>
    public async Task<bool> OnDisappearingAsync()
    {
        if (!HasUnsavedChanges)
            return true; // No unsaved changes, allow navigation

        // Show confirmation dialog
        var result = await _dialogService.ShowAsync(dialog =>
        {
            dialog.Title = "Unsaved Changes";
            dialog.Content = new StackPanel
            {
                Spacing = 12,
                Children =
                {
                    new TextBlock
                    {
                        Text = "You have unsaved changes in your document.",
                        TextWrapping = global::Avalonia.Media.TextWrapping.Wrap
                    },
                    new TextBlock
                    {
                        Text = "Do you want to discard these changes and navigate away?",
                        TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
                        Foreground = new SolidColorBrush(Colors.Orange)
                    }
                }
            };
            dialog.PrimaryButtonText = "Discard Changes";
            dialog.SecondaryButtonText = "Keep Editing";
            dialog.DefaultButton = DefaultButton.Secondary; // Default to safe option
        });

        // Return true (allow navigation) only if user chose "Discard Changes"
        return result == DialogResult.Primary;
    }

    /// <summary>
    /// Called when the page appears.
    /// </summary>
    public Task OnAppearingAsync()
    {
        // Reset state when page appears
        return Task.CompletedTask;
    }
}
