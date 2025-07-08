using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
using Muek.Commands;
using Muek.Services;

namespace Muek.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public ObservableCollection<PlaylistTrack> Tracks {get;set;} =  new ObservableCollection<PlaylistTrack>();

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

    public void addTrack()
    {
        Tracks.Add(new PlaylistTrack(Tracks.Count));
    }

    public void selectTrack(long trackId)
    {
        //Console.WriteLine(trackId);
        foreach (var track in Tracks)
        {
            if (track.trackId == trackId)
            {
                track.selected = true;
            }
            else
            {
                track.selected = false;
            }
        }
        
    }
}

public class PlaylistTrack
{
    public long trackId {get; set;}
    public string trackName {get; set;}
    public IBrush trackColor {get; set;}
    public bool selected {get; set;} = false;

    public PlaylistTrack(long trackId)
    {
        this.trackId = trackId;
        this.trackName = "New Track" +  trackId;
        this.trackColor = Brush.Parse("#666666");
    }

    public PlaylistTrack(long trackId, string trackName)
    {
        this.trackId = trackId;
        this.trackName = trackName;
        this.trackColor = Brush.Parse("#666666");
    }

    public PlaylistTrack(long trackId, string trackName, IBrush trackColor)
    {
        this.trackId = trackId;
        this.trackName = trackName;
        this.trackColor = trackColor;
    }

}