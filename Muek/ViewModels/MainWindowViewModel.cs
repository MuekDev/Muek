using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Muek.Engine;
using Muek.Models;
using Muek.Services;
using Muek.Views;

namespace Muek.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public ObservableCollection<TrackViewModel> Tracks => DataStateService.Tracks;
    [ObservableProperty] private int _count = 0;

    private float PlayPosition
    {
        get => AudioService.PlayPosition;
        set => AudioService.PlayPosition = value;
    }

    public MainWindowViewModel()
    {
        // Tracks = [new TrackViewModel("Master", Brush.Parse("#51cc8c"))];
    }
    
    [RelayCommand]
    public void OnPlayButtonClick()
    {
        if(!DataStateService.IsPlaying)
            Play();
        else
            Stop();
    }

    [RelayCommand]
    public void OnStopButtonClick()
    {
        Stop();
    }

    private unsafe void Play()
    {
        Console.WriteLine("Omg it is playing...");
        // await RpcService.SendCommand(new PlayCommand());
        // AudioService.Play();
        DataStateService.IsPlaying = true;
        var clips = new List<ClipProto>();
        foreach (var track in Tracks)
        {
            foreach (var clip in track.Clips)
            {
                var id = clip.Proto.Id;
                fixed (char* idStr = id)
                {
                    var proto = new ClipProto()
                    {
                        clip_id = (ushort*)idStr,
                        clip_id_len = id.Length,
                        end_time = (float)(clip.Proto.StartBeat + clip.Proto.Duration),
                        start_time = (float)clip.StartBeat
                    };
                    clips.Add(proto);
                }
            }

            var protoArr = clips.ToArray();
            fixed (ClipProto* clipPtr = protoArr)
            {
                MuekEngine.sync_all_clips(clipPtr, protoArr.Length);
            }
        }
        
        MuekEngine.stream_play(PlayPosition);
        AudioService.TriggerAudioStarted();
    }

    private void Stop()
    {
        DataStateService.IsPlaying = false;
        Console.WriteLine("Omg it is stopping...");
        PlayPosition = MuekEngine.get_current_position_beat();
        if(MuekEngine.stream_stop())
            AudioService.TriggerAudioStopped();
        else
        {
            AudioService.TriggerAudioStarted();
            Thread.Sleep(1);
            AudioService.TriggerAudioStopped();
        }
        // Console.WriteLine(PlayPosition);
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

    [RelayCommand]
    public void ShowTrackPlugins(TrackViewModel tvm)
    {
        var vm = new TrackPluginStackWindowViewModel(tvm.Proto);
        var window = new TrackPluginStackWindow(vm)
        {
            Topmost = true
        };
        window.Show();
    }
}