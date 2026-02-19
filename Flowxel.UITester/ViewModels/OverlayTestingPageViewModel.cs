using System.Diagnostics;
using System.Threading.Tasks;
using global::Avalonia.Controls;
using global::Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Flowxel.UI.Services;

namespace Flowxel.UITester.ViewModels;

public class OverlayTestingPageViewModel : ViewModelBase
{
    public OverlayTestingPageViewModel(IOverlayService overlayService)
    {
        _overlayService = overlayService;
        RunSimpleTaskCommand = new AsyncRelayCommand(RunSimpleTask, CanRunTask);
        RunComplexTaskCommand = new AsyncRelayCommand(RunComplexTask, CanRunTask);
    }

    private readonly IOverlayService _overlayService;

    public string? LastResult
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool IsBusy
    {
        get;
        private set
        {
            if (!SetProperty(ref field, value)) return;
            RunSimpleTaskCommand.NotifyCanExecuteChanged();
            RunComplexTaskCommand.NotifyCanExecuteChanged();
        }
    }

    public IAsyncRelayCommand RunSimpleTaskCommand { get; }
    public IAsyncRelayCommand RunComplexTaskCommand { get; }

    private bool CanRunTask() => !IsBusy;

    private async Task RunSimpleTask()
    {
        IsBusy = true;
        var sw = Stopwatch.StartNew();

        try
        {
            // Phase 1: Indeterminate - Initializing
            await _overlayService.ShowAsync(o =>
            {
                o.Title = "Processing";
                o.IsIndeterminate = true;
                o.Message = "Initializing...";
            });
            await Task.Delay(2000);

            // Phase 2: Determinate - Steps 1-2
            await _overlayService.UpdateAsync(o =>
            {
                o.IsIndeterminate = false;
                o.Progress = 0;
                o.Message = "Step 1 of 4...";
            });
            await Task.Delay(1000);

            await _overlayService.UpdateAsync(o =>
            {
                o.Progress = 25;
                o.Message = "Step 2 of 4...";
            });
            await Task.Delay(1000);

            await _overlayService.UpdateAsync(o => o.Progress = 50);

            // Phase 3: Indeterminate - Analyzing
            await _overlayService.UpdateAsync(o =>
            {
                o.IsIndeterminate = true;
                o.Message = "Analyzing results...";
            });
            await Task.Delay(2000);

            // Phase 4: Determinate - Steps 3-4
            await _overlayService.UpdateAsync(o =>
            {
                o.IsIndeterminate = false;
                o.Progress = 75;
                o.Message = "Step 3 of 4...";
            });
            await Task.Delay(1000);

            await _overlayService.UpdateAsync(o =>
            {
                o.Progress = 100;
                o.Message = "Step 4 of 4...";
            });
            await Task.Delay(500);

            await _overlayService.HideAsync();
            sw.Stop();
            LastResult = $"Task completed in {sw.Elapsed.TotalSeconds:F1} seconds";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RunComplexTask()
    {
        IsBusy = true;
        var sw = Stopwatch.StartNew();

        var steps = new[]
        {
            "Validating input data",
            "Connecting to service",
            "Processing records",
            "Generating report",
            "Finalizing"
        };

        try
        {
            await _overlayService.ShowAsync(o =>
            {
                o.Title = "Complex Operation";
                o.IsIndeterminate = true;
                o.Message = "Starting...";
                o.Content = BuildStepList(steps, -1);
            });

            for (int i = 0; i < steps.Length; i++)
            {
                // Switch to indeterminate while "working" on this step
                await _overlayService.UpdateAsync(o =>
                {
                    o.IsIndeterminate = true;
                    o.Message = steps[i] + "...";
                    o.Content = BuildStepList(steps, i - 1);
                });
                await Task.Delay(1500);

                // Mark step complete with determinate progress
                int completed = i + 1;
                await _overlayService.UpdateAsync(o =>
                {
                    o.IsIndeterminate = false;
                    o.Progress = (double)completed / steps.Length * 100;
                    o.Content = BuildStepList(steps, i);
                });
                await Task.Delay(500);
            }

            await _overlayService.UpdateAsync(o => o.Message = "Done!");
            await Task.Delay(500);

            await _overlayService.HideAsync();
            sw.Stop();
            LastResult = $"Task completed in {sw.Elapsed.TotalSeconds:F1} seconds";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static StackPanel BuildStepList(string[] steps, int completedUpTo)
    {
        var panel = new StackPanel { Spacing = 4 };

        for (int i = 0; i < steps.Length; i++)
        {
            var prefix = i <= completedUpTo ? "\u2713 " : "  ";
            var tb = new TextBlock
            {
                Text = prefix + steps[i],
                Foreground = i <= completedUpTo
                    ? new SolidColorBrush(Color.Parse("#4CAF50"))
                    : new SolidColorBrush(Color.Parse("#888888"))
            };
            panel.Children.Add(tb);
        }

        return panel;
    }
}
