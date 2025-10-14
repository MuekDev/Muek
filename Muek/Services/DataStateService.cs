using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
using Muek.Models;
using Muek.ViewModels;

namespace Muek.Services;

public static class DataStateService
{
    public static ObservableCollection<TrackViewModel> Tracks { get; private set; } =
    [
        new TrackViewModel(new Track
            { Color = Colors.YellowGreen.ToString(), Id = Guid.NewGuid().ToString(), Name = "Master" })
    ];

    public static float Bpm { get; set; } = 120f;

    public static bool IsPlaying { get; set; }
    public static TrackViewModel? ActiveTrack { get; set; }
    
    // 即拍数
    public static int Subdivisions { get; set; } = 4;

    public static void AddTrack(string? name = null, Color? color = null)
    {
        Tracks.Add(
            new TrackViewModel(
                new Track
                {
                    Color = color == null ? Colors.DimGray.ToString() : color.ToString(),
                    Id = Guid.NewGuid().ToString(),
                    Index = (uint)Tracks.Count,
                    Name = name ?? "New Track",
                }
            ));
    }
    
    public static void RemoveTrack(String trackId)
    {
        var track = Tracks.FirstOrDefault(x => x.Id == trackId);
        if (track != null)
            Tracks.Remove(track);
    }
    
    public static TrackViewModel GetSelectedTrack()
    {
        foreach (var track in Tracks)
        {
            if (track.Selected)
            {
                return track;
            }
        }
        return null;
    }
}