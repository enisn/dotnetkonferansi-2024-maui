using ReactiveUI;

namespace ReactiveApp;

public partial class MainPage
{
    public MainPage()
    {
        InitializeComponent();

        ViewModel = new ViewModels.MainViewModel();

        this.WhenActivated(_ => { });
    }
}

