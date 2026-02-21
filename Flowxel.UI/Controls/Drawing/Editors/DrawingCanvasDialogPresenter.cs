using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using Flowxel.Core.Geometry.Shapes;
using Flowxel.UI.Controls;
using Flowxel.UI.Services;

namespace Flowxel.UI.Controls.Drawing;

internal static class DrawingCanvasDialogPresenter
{
    public static Task ShowShapePropertiesAsync(IContentDialogService? dialogService, Shape shape, Action onApplied)
    {
        if (dialogService is null)
            return Task.CompletedTask;

        var editor = new DrawingShapePropertiesEditor(shape);
        return dialogService.ShowAsync(dialog =>
        {
            dialog.Title = $"{shape.Type} properties";
            dialog.Content = editor;
            dialog.PrimaryButtonText = "Apply";
            dialog.CloseButtonText = "Cancel";
            dialog.DefaultButton = DefaultButton.Primary;
            dialog.PrimaryButtonCommand = new RelayCommand(() =>
            {
                editor.ApplyChanges();
                onApplied();
            });
        });
    }

    public static Task ShowCanvasSettingsAsync(IContentDialogService? dialogService, DrawingCanvasControl canvas, Action onApplied)
    {
        if (dialogService is null)
            return Task.CompletedTask;

        var editor = new DrawingCanvasSettingsEditor(canvas);
        return dialogService.ShowAsync(dialog =>
        {
            dialog.Title = "Canvas settings";
            dialog.Content = editor;
            dialog.PrimaryButtonText = "Apply";
            dialog.CloseButtonText = "Cancel";
            dialog.DefaultButton = DefaultButton.Primary;
            dialog.PrimaryButtonCommand = new RelayCommand(() =>
            {
                editor.ApplyTo(canvas);
                onApplied();
            });
        });
    }
}
