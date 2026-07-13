using System.Windows.Controls;
using VHWuWa.App.ViewModels;

namespace VHWuWa.App.Views;

public partial class HomePage : UserControl, IPageView
{
    private readonly HomeViewModel _vm;
    public HomePage(HomeViewModel vm)
    {
        _vm = vm;
        InitializeComponent();
        DataContext = vm;
    }
    public void OnNavigated() => _vm.OnActivated();
}
