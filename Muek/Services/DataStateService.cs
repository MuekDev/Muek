using System.Collections.Generic;
using Audio;
using Avalonia.Media;
using Muek.ViewModels;

namespace Muek.Services;

public static class DataStateService
{
    // public static List<Track> Tracks { get; set; } =
    // [
    //     new() { Clips = [new Clip { Duration = 10, StartBeat = 2 }] },
    //     new() { Clips = [new Clip { Duration = 2, StartBeat = 0 }] }
    // ];
    //
    public static List<TrackViewModel> Tracks { get; set; } = [
        new TrackViewModel(new Track { Color = Colors.YellowGreen.ToString(),Id = "114514" })
    ];

    public static float Bpm { get; set; } = 140f;
    
    public static bool IsPlaying { get; set; }
}