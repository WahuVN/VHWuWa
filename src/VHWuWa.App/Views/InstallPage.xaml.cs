using System.Windows.Controls;
using VHWuWa.App.ViewModels;

namespace VHWuWa.App.Views;

public partial class InstallPage : UserControl, IPageView
{
    private readonly InstallViewModel _vm;
    public InstallPage(InstallViewModel vm)
    {
        _vm = vm;
        InitializeComponent();
        DataContext = vm;
    }
    public void OnNavigated() => _vm.OnActivated();
}
