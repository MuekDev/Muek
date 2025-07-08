using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Audio;
using Avalonia.Media;
using Muek.ViewModels;

namespace Muek.Services;

public static class DataStateService
{
    public static ObservableCollection<TrackViewModel> Tracks { get; private set; } =
    [
        new TrackViewModel(new Track
            { Color = Colors.YellowGreen.ToString(), Id = Guid.NewGuid().ToString(), Name = "Master" })
    ];

    public static float Bpm { get; set; } = 140f;

    public static bool IsPlaying { get; set; }

    public static void AddTrack(string? name = null, Color? color = null)
    {
        Tracks.Add(
            new TrackViewModel(
                new Track
                {
                    Color = color.ToString() ?? Colors.DimGray.ToString(),
                    Id = Guid.NewGuid().ToString(),
                    Name = name ?? "New Track",
                    Index = (uint)Tracks.Count,
                }
            ));
    }
}