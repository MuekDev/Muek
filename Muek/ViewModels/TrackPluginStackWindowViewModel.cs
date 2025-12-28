using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using Muek.Models;

namespace Muek.ViewModels;

public partial class TrackPluginStackWindowViewModel : ViewModelBase
{
    // public Track AimTrack { get; set; }
    [ObservableProperty] private Track _aimTrack;

    [ObservableProperty] private ObservableCollection<VstPlugin> _plugins;

    [ObservableProperty] private string _windowTitle;

    public TrackPluginStackWindowViewModel(Track aimTrack)
    {
        AimTrack = aimTrack;
        Plugins = new ObservableCollection<VstPlugin>(aimTrack.Plugins);
        WindowTitle = $"[Plugins] {aimTrack.Name}";
    }

    [Obsolete]
    public TrackPluginStackWindowViewModel()
    {
        throw new Exception("This constructor should never be called");
    }

    public void PushPlugin(VstPlugin plugin)
    {
        AimTrack.Plugins.Add(plugin);
        Plugins.Add(plugin);        
        // Plugins = new ObservableCollection<VstPlugin>(AimTrack.Plugins);
    }
}