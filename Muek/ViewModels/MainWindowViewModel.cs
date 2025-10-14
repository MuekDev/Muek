using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Muek.Services;
using Muek.Views;

namespace Muek.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public ObservableCollection<TrackViewModel> Tracks => DataStateService.Tracks;
    [ObservableProperty] private int _count = 0;

    public MainWindowViewModel()
    {
        // Tracks = [new TrackViewModel("Master", Brush.Parse("#51cc8c"))];
    }

    [RelayCommand]
    public async Task OnPlayButtonClick()
    {
        Console.WriteLine("Omg it is playing...");
        // await RpcService.SendCommand(new PlayCommand());
        AudioService.Play();
    }

    [RelayCommand]
    public async Task OnStopButtonClick()
    {
        Console.WriteLine("Omg it is stopping...");
        // await RpcService.SendCommand(new StopCommand());
        // TODO
        AudioService.Stop();
    }

    [RelayCommand]
    public void OnRecordButtonClick()
    {
        Console.WriteLine("Omg it is recording...");
        Count += 1;
    }

    [RelayCommand]
    public void AddTrack()
    {
        DataStateService.AddTrack();
    }
}