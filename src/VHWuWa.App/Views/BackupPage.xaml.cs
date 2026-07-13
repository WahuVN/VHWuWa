using System.Windows.Controls;
using VHWuWa.App.ViewModels;

namespace VHWuWa.App.Views;

public partial class BackupPage : UserControl, IPageView
{
    private readonly BackupViewModel _vm;
    public BackupPage(BackupViewModel vm)
    {
        _vm = vm;
        InitializeComponent();
        DataContext = vm;
    }
    public void OnNavigated() => _vm.OnActivated();
}
