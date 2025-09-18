using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using Muek.Services;
using Muek.ViewModels;

namespace Muek.Views;

public partial class Mixer : UserControl
{
    //TODO
    //Name
    //Color

    public Mixer()
    {
        InitializeComponent();
        Console.WriteLine("Mixer Initialized");
    }

    private void HideMixer(object? sender, RoutedEventArgs e)
    {
        this.IsVisible = false;
    }

    public void ShowMixer()
    {
        var track = DataStateService.ActiveTrack;
        
        MixerName.Content = track.Name;
        MixerPan.ValuerColor = Brush.Parse(track.Color);
        MixerVol.ValuerColor = Brush.Parse(track.Color);
        MixerColor.Background = Brush.Parse(track.Color);
        Console.WriteLine($"TrackName: {track.Name}");
        Console.WriteLine($"Track Color: {track.Color}\n" +
                          $"Mixer Color: {MixerPan.ValuerColor}");
        IsVisible = true;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
    }
}