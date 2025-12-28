using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using Muek.Models;
using Muek.ViewModels;
using Muek.Views;

namespace Muek.Services;

public static class DataStateService
{
    public static Color MuekColor = new Color(255,100, 200, 150);
    public static IBrush MuekColorBrush = new SolidColorBrush(MuekColor);
    
    public static ObservableCollection<TrackViewModel> Tracks { get; private set; } =
    [
        new TrackViewModel(new Track
            { Color = DataStateService.MuekColor.ToString(), Id = Guid.NewGuid().ToString(), Name = "Master" })
    ];

    private static float _bpm = 120f;

    public static float Bpm
    {
        get => _bpm;
        set => _bpm = value;
    }

    public static bool IsPlaying { get; set; }
    public static TrackViewModel? ActiveTrack { get; set; }
    
    // 即拍数
    public static int Subdivisions { get; set; } = 4;
    public static readonly int Midi2TrackFactor = 16;

    public static PianoRollWindow PianoRollWindow { get; set; } = new();

    public static void AddTrack(string? name = null, Color? color = null)
    {
        Tracks.Add(
            new TrackViewModel(
                new Track
                {
                    Color = color != null ? color.ToString()! : Colors.DimGray.ToString(),
                    Id = Guid.NewGuid().ToString(),
                    Index = (uint)Tracks.Count,
                    Name = name ?? "New Track",
                }
            ));
        ViewHelper.GetMainWindow().TrackViewControl.InvalidateVisual();
    }
    
    public static void RemoveTrack(String trackId)
    {
        var track = Tracks.FirstOrDefault(x => x.Id == trackId);
        if (track != null)
            Tracks.Remove(track);
        ViewHelper.GetMainWindow().TrackViewControl.InvalidateVisual();
    }
    
    // public static TrackViewModel GetSelectedTrack()
    // {
    //     foreach (var track in Tracks)
    //     {
    //         if (track.Selected)
    //         {
    //             return track;
    //         }
    //     }
    //     return null;
    // }
}