using System.Windows.Controls;
using VHWuWa.App.ViewModels;

namespace VHWuWa.App.Views;

public partial class SettingsPage : UserControl, IPageView
{
    private readonly SettingsViewModel _vm;
    public SettingsPage(SettingsViewModel vm)
    {
        _vm = vm;
        InitializeComponent();
        DataContext = vm;
    }
    public void OnNavigated() => _vm.OnActivated();
}
