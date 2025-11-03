using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Muek.Engine;
using Muek.ViewModels;

namespace Muek.Models;

public partial class VstPlugin: ViewModelBase, IPlugin
{
    [ObservableProperty] public string _name;
    [ObservableProperty] public string _path;

    // [RelayCommand]
    // public void TestPlugin(string path)
    // {
    //     // Console.WriteLine(path);
    //     unsafe
    //     {
    //         fixed (char* p = path)
    //         {
    //             MuekEngine.run_vst_instance_by_path((ushort*)p, path.Length);
    //         }
    //     }
    // }
    //
    public void ShowEditor()
    {
        throw new System.NotImplementedException();
    }

    public void CloseEditor()
    {
        throw new System.NotImplementedException();
    }

    public string GetPluginName()
    {
        throw new System.NotImplementedException();
    }
}