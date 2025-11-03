using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Muek.ViewModels;

namespace Muek.Views;

public partial class PluginManagerWindow : Window
{
    public PluginManagerWindow()
    {
        InitializeComponent();
        DataContext = new PluginManagerWindowViewModel(this);
    }
}