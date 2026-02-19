using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Flowxel.UITester.ViewModels;

public partial class EditorsTestingPageViewModel : ViewModelBase
{
    [ObservableProperty]
    private string? _textValue = "Hello world";

    [ObservableProperty]
    private double? _doubleValue = 3.14;

    [ObservableProperty]
    private int? _intValue = 42;

    [ObservableProperty]
    private ulong? _ulongValue = 1024;

    [ObservableProperty]
    private string? _multiLineTextValue = "Line one\nLine two\nLine three";

    [ObservableProperty]
    private byte[]? _hexValue = [0x0A, 0xFF, 0x1B, 0x42, 0xDE, 0xAD, 0xBE, 0xEF];

    [ObservableProperty]
    private byte[]? _base64Value = Encoding.UTF8.GetBytes("Hello, fx!");
}
