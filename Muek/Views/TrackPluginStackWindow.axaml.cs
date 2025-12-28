using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Muek.Engine;
using Muek.Models;
using Muek.ViewModels;

namespace Muek.Views;

public partial class TrackPluginStackWindow : Window
{
    private TrackPluginStackWindowViewModel _vm;

    public TrackPluginStackWindow()
    {
        throw new Exception("This constructor should never be called");
    }
    
    public TrackPluginStackWindow(TrackPluginStackWindowViewModel vm)
    {
        _vm = vm;
        DataContext = vm;
        InitializeComponent();
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        var window = new PluginManagerWindow();
        window.SubmitNewPlugin += WindowOnSubmitNewPlugin;
        window.ShowDialog(this);
    }

    private void WindowOnSubmitNewPlugin(object? sender, VstPlugin e)
    {
        _vm.PushPlugin(plugin:e);
    }

    private void InputElement_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (PluginListBox.SelectedItem is VstPlugin plugin)
        {
            var path = plugin.Path;
            
            unsafe
            {
                fixed (char* p = path)
                {
                    MuekEngine.run_vst_instance_by_path((ushort*)p, path.Length);
                }
            }
        }
    }
}