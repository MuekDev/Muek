using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Muek.Models;
using Muek.ViewModels;

namespace Muek.Views;

public partial class PluginManagerWindow : Window
{
    public event EventHandler<VstPlugin>? SubmitNewPlugin; 
    
    public PluginManagerWindow()
    {
        InitializeComponent();
        DataContext = new PluginManagerWindowViewModel(this);
    }

    private void SubmitPlugin(object? sender, RoutedEventArgs e)
    {
        if (PluginsDataGrid.SelectedItem is VstPlugin plugin)
        {
            SubmitNewPlugin?.Invoke(this, plugin);
        }
    }
}