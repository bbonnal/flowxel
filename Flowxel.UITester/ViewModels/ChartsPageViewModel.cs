using System;
using Avalonia;
using Avalonia.Styling;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace Flowxel.UITester.ViewModels;

public class ChartsPageViewModel : ViewModelBase
{
    public ChartsPageViewModel()
    {
        BuildSeries();

        if (Application.Current is { } app)
            app.ActualThemeVariantChanged += OnThemeChanged;
    }

    public ISeries[] LineSeries
    {
        get;
        set => SetProperty(ref field, value);
    } = [];

    public ISeries[] ColumnSeries
    {
        get;
        set => SetProperty(ref field, value);
    } = [];

    public ISeries[] PieSeries
    {
        get;
        set => SetProperty(ref field, value);
    } = [];

    public Axis[] XAxes
    {
        get;
        set => SetProperty(ref field, value);
    } = [];

    public Axis[] YAxes
    {
        get;
        set => SetProperty(ref field, value);
    } = [];

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        ConfigureLiveChartsTheme();
        BuildSeries();
    }

    private static void ConfigureLiveChartsTheme()
    {
        var isDark = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;

        LiveCharts.Configure(settings =>
            settings
                .AddSkiaSharp()
                .AddDefaultMappers());

        if (isDark)
            LiveCharts.Configure(settings => settings.AddDarkTheme());
        else
            LiveCharts.Configure(settings => settings.AddLightTheme());
    }

    private void BuildSeries()
    {
        LineSeries =
        [
            new LineSeries<double>
            {
                Name = "Downloads",
                Values = [2, 5, 4, 8, 6, 12, 9]
            },
            new LineSeries<double>
            {
                Name = "Revenue",
                Values = [1, 3, 5, 3, 7, 10, 13]
            }
        ];

        ColumnSeries =
        [
            new ColumnSeries<double>
            {
                Name = "2024",
                Values = [120, 95, 140, 110, 160]
            },
            new ColumnSeries<double>
            {
                Name = "2025",
                Values = [150, 120, 170, 130, 190]
            }
        ];

        XAxes =
        [
            new Axis
            {
                Labels = ["Mon", "Tue", "Wed", "Thu", "Fri"]
            }
        ];

        YAxes =
        [
            new Axis
            {
                Name = "Sales"
            }
        ];

        PieSeries =
        [
            new PieSeries<double> { Name = "Chrome", Values = [65] },
            new PieSeries<double> { Name = "Firefox", Values = [12] },
            new PieSeries<double> { Name = "Safari", Values = [10] },
            new PieSeries<double> { Name = "Edge", Values = [8] },
            new PieSeries<double> { Name = "Other", Values = [5] }
        ];
    }
}
