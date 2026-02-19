namespace Flowxel.ImagingTester.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel(ImagingCanvasPageViewModel imaging)
    {
        Imaging = imaging;
    }

    public ImagingCanvasPageViewModel Imaging { get; }
}
