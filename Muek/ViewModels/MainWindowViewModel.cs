using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Muek.Commands;
using Muek.Services;
using Muek.Views;

//CHANNELUID
using CHANNELUID = long;

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
        Tracks[0].Selected = true;
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

    public void AddTrack()
    {
        TRACKUID _maxTrackId = 0;
        foreach (var track in Tracks)
        {
            _maxTrackId = track.TrackId > _maxTrackId ? track.TrackId : _maxTrackId;
        }
        Tracks.Add(new PlaylistTrack(_maxTrackId + 1));
    }

    public void RemoveTrack(TRACKUID trackId)
    {
        PlaylistTrack deleteTrack = null;
        foreach (var track in Tracks)
        {
            if (track.TrackId == trackId)
            {
                deleteTrack = track;
            }
        }
        Tracks.Remove(deleteTrack);
    }
    public void SelectTrack(TRACKUID? trackId = null)
    {
        //Console.WriteLine(trackId);
        foreach (var track in Tracks)
        {
            if (track.TrackId == trackId)
            {
                track.Selected = true;
            }
            else
            {
                 track.Selected = false;
            }
        }
    }

    public void SwitchTracks(TRACKUID trackId, int moveIndex)
    {
        PlaylistTrack TrackFrom = null;
        PlaylistTrack TrackTo = null;
        foreach (var track in Tracks)
        {
            Console.WriteLine(track.TrackId);
            if (track.TrackId == trackId)
            {
                TrackFrom = track;
                
            }
        
            if (track.TrackId == trackId + moveIndex)
            {
                TrackTo = track;
            }
        }
        // Console.WriteLine("From: "+TrackFrom.TrackId);
        // Console.WriteLine("To: "+TrackTo.TrackId);
        var tempTrack = TrackFrom;
        if (TrackTo != null)
        {
            TrackFrom =  TrackTo;
            TrackTo = tempTrack;
        }
        
    }

    
    public void addSideChain(TRACKUID trackId)
    {
        foreach (var track in Tracks)
        {
            if (track.Selected == true)
            {
                foreach (var _2track in Tracks)
                {
                    
                }
            }
        }
    }
    
}

public partial class PlaylistTrack : ViewModelBase
{
    [ObservableProperty] private TRACKUID _trackId;
    [ObservableProperty] private string _trackName;
    [ObservableProperty] private IBrush _trackColor;

    [ObservableProperty] private bool _selected = false;
    [ObservableProperty] private bool _byPassed = false;

    
    //Channel Connection
    /*                                                              *\
     Left Channel           L ---o----|-------- R       Volume[0dB] [Negative]
     Right Channel          L -------o|-------- R       Volume[0dB] [Negative]
     L/R调整左右声像 offset调整响度偏移
    \*                                                              */
    
    //
    public class Channel
    {
        private CHANNELUID _channelId;
        private string _channelName;
        
        private bool _leftNegative = false;
        private bool _rightNegative = false;
        private float _leftVolume;
        private float _rightVolume;
        private float _leftPan;
        private float _rightPan;
        
        private bool _bypassFX;
        private bool _sidechainMode;

        public Channel(CHANNELUID channelId, string? channelName = null)
        {
            this._channelId = channelId;
            this._channelName = channelName ?? "New Channel" + channelId;
            this._leftVolume = 0;
            this._rightVolume = 0;
            this._leftPan = -100;
            this._rightPan = 100;
            this._bypassFX = false;
            this._sidechainMode = false;
        }

        public void setLeft(float leftVolume, float? leftPan, bool? leftNegative)
        {
            this._leftVolume = leftVolume;
            this._leftPan = leftPan ?? this._leftPan;
            this._leftNegative = leftNegative ?? this._leftNegative;
        }

        public void setRight(float rightVolume, float? rightPan, bool? rightNegative)
        {
            this._rightVolume = rightVolume;
            this._rightPan = rightPan ?? this._rightPan;
            this._rightNegative = rightNegative ?? this._rightNegative;
        }

    } 
    [ObservableProperty] private List<Channel> _channelConnection = new List<Channel>();

    public void addChannel(TRACKUID trackId)
    {
        ChannelConnection.Add(new Channel(ChannelConnection.Count));
    }
    
    
    public PlaylistTrack(TRACKUID trackId, string? trackName = null, IBrush? trackColor = null)
    {
        TrackId = trackId;
        TrackName = trackName ?? "New Track" + trackId;
        TrackColor = trackColor ?? Brush.Parse("#666666");
        
        
        Channel MasterChannel = new Channel(0,"Master Channel");
        ChannelConnection.Add(MasterChannel);
    }

    [RelayCommand]
    public void OnByPassButtonClick()
    {
        ByPassed = !ByPassed;
    }

    [RelayCommand]
    public void ShowRenameWindow()
    {
        var renameWindow = new RenameWindow();
        renameWindow.Show();
        renameWindow.NameBox.Text = TrackName;
        renameWindow.Submit += (sender, args) =>
        {
            TrackName = args;
        };
        // if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        // {
        //     var mainWindow = desktop.MainWindow;
        //     if (mainWindow != null)
        //     {
        //         var window = new RenameWindow();
        //         window.ShowDialog(mainWindow);
        //         window.NameBox.Text = TrackName;
        //         window.Submit += (sender, s) =>
        //         {
        //             TrackName = s;
        //         };
        //     }
        // }
    }

    [RelayCommand]
    public void showRecolorWindow()
    {
        var recolorWindow = new RecolorWindow();
        recolorWindow.Show();
        
    }
    
}