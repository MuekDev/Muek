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
using Muek.Commands;
using Muek.Services;
using Muek.Views;

namespace Muek.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public ObservableCollection<PlaylistTrack> Tracks { get; set; } = new ObservableCollection<PlaylistTrack>();
    [ObservableProperty] private int _count = 0;

    public MainWindowViewModel()
    {
        Tracks = new ObservableCollection<PlaylistTrack>
        {
            new PlaylistTrack(0, "Master", Brush.Parse("#51cc8c"))
        };
    }

    [RelayCommand]
    public async Task OnPlayButtonClick()
    {
        Console.WriteLine("Omg it is playing...");
        await RpcService.SendCommand(new PlayCommand());
    }

    [RelayCommand]
    public async Task OnStopButtonClick()
    {
        Console.WriteLine("Omg it is stopping...");
        await RpcService.SendCommand(new StopCommand());
    }

    [RelayCommand]
    public void OnRecordButtonClick()
    {
        Console.WriteLine("Omg it is recording...");
        Count += 1;
    }

    public void addTrack()
    {
        Tracks.Add(new PlaylistTrack(Tracks.Count));
    }

    public void selectTrack(long trackId)
    {
        //Console.WriteLine(trackId);
        foreach (var track in Tracks)
        {
            // if (track.trackId == trackId)
            // {
            //     track.Selected = true;
            // }
            // else
            // {
            //     track.Selected = false;
            // }
        }
    }
}

public partial class PlaylistTrack : ViewModelBase
{
    [ObservableProperty] private long _trackId;
    [ObservableProperty] private string _trackName;
    [ObservableProperty] private IBrush _trackColor;

    [ObservableProperty] private bool _selected = true;
    [ObservableProperty] private bool _byPassed = false;
    [ObservableProperty] private IBrush _byPassBtnColor = Brush.Parse("#D0FFE5");

    partial void OnByPassedChanged(bool value)
    {
        if (value)
        {
            ByPassBtnColor = Brush.Parse("#313131");
        }
        else
        {
            ByPassBtnColor = Brush.Parse("#D0FFE5");
        }
    }

    public PlaylistTrack(long trackId, string? trackName = null, IBrush? trackColor = null)
    {
        TrackId = trackId;
        TrackName = trackName ?? "New Track" + trackId;
        TrackColor = trackColor ?? Brush.Parse("#666666");
    }

    [RelayCommand]
    public void OnByPassButtonClick()
    {
        ByPassed = !ByPassed;
    }

    [RelayCommand]
    public void ShowRenameWindow()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = desktop.MainWindow;
            if (mainWindow != null)
            {
                var window = new RenameWindow();
                window.ShowDialog(mainWindow);
                window.NameBox.Text = TrackName;
                window.Submit += (sender, s) =>
                {
                    TrackName = s;
                };
            }
        }
    }
}