using System.Windows.Controls;
using VHWuWa.App.ViewModels;

namespace VHWuWa.App.Views;

public partial class GraphicsPage : UserControl, IPageView
{
    private readonly GraphicsViewModel _vm;
    public GraphicsPage(GraphicsViewModel vm)
    {
        _vm = vm;
        InitializeComponent();
        DataContext = vm;
    }
    public void OnNavigated() => _vm.OnActivated();
}
