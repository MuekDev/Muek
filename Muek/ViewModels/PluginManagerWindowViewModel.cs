using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Mime;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Muek.Engine;
using Muek.Helpers;
using Muek.Models;
using Muek.Views;

namespace Muek.ViewModels;

public partial class PluginManagerWindowViewModel(PluginManagerWindow window) : ViewModelBase
{
    public PluginManagerWindow Window { get; } = window;

    public List<string> VstPath = [
    @"C:\Program Files\VstPlugins\",
    @"C:\Program Files (x86)\VstPlugins\",
    @"C:\Program Files (x86)\Steinberg\VstPlugins\",
    @"C:\Program Files\Common Files\VST2\",
    @"C:\Program Files\Steinberg\VstPlugins\",
    @"C:\Program Files (x86)\Common Files\VST3\",
    @"C:\Program Files\Common Files\VST3\",
    ];

    public ObservableCollection<VstPlugin> Plugins { get; } =
    [
        new VstPlugin() { Name = "ddd", Path = "omg" }
    ];

    [RelayCommand]
    public async Task ReScan()
    {
        Plugins.Clear();
        foreach (var path in VstPath)
        {
            await foreach (var file in VstHelper.ScanDirAsync(path))
            {
                Console.WriteLine($"Name: {file.Name}");
                Console.WriteLine($"Full path: {file.FullName}");
                Console.WriteLine($"Size: {file.Length} bytes");
                Console.WriteLine($"Last modified: {file.LastWriteTime}");
                Plugins.Add(new VstPlugin() { Name = file.Name, Path = file.FullName });
            }
        }
    }

    [RelayCommand]
    public void TestPlugin(string path)
    {
        // var handle = Window.TryGetPlatformHandle();
        // Console.WriteLine(handle.Handle);

        unsafe
        {
            fixed (char* p = path)
            {
                // Avalonia.Controls.Window window = new();
                // window.Show();
                // var handle = window.TryGetPlatformHandle();
                // Console.WriteLine(handle.Handle);
                // Debug.Assert(handle != null, nameof(handle) + " != null");
                // MuekEngine.run_vst_instance_by_path_with_handle((ushort*)p, path.Length, (nuint)handle.Handle);
                MuekEngine.run_vst_instance_by_path((ushort*)p, path.Length);
            }
        }


        // 在 STA UI 线程执行
        // Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
        // {
        //     // Console.WriteLine(path);
        //     unsafe
        //     {
        //         fixed (char* p = path)
        //         {
        //             MuekEngine.run_vst_instance_by_path((ushort*)p, path.Length);
        //         }
        //     }
        // });
    }
}