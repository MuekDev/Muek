using System;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Muek.Models;
using Muek.Services;
using Muek.ViewModels;

namespace Muek.Views;

public partial class Mixer : UserControl
{
    //Mixer部分

    public Mixer()
    {
        InitializeComponent();
        Width = 0;
        Console.WriteLine("Mixer Initialized");
        
    }

    private void HideMixer(object? sender, RoutedEventArgs e)
    {
        Opacity = 1.0;
        new Animation
        {
            Duration = TimeSpan.FromMilliseconds(500),
            FillMode = FillMode.Forward,
            Easing = Easing.Parse("CubicEaseOut"),
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(1),
                    Setters =
                    {
                        new Setter
                        {
                            Property = OpacityProperty,
                            Value = 0.0
                        },
                        new Setter
                        {
                            Property = WidthProperty,
                            Value = 0.0
                        }
                    }
                }
            }
        }.RunAsync(this);
        if (Opacity == 0.0)
        {
            this.IsVisible = false;
        }
    }

    public void Show()
    {
        Opacity = 0.0;
        IsVisible = true;
        new Animation
        {
            Duration = TimeSpan.FromMilliseconds(500),
            FillMode = FillMode.Forward,
            Easing = Easing.Parse("CubicEaseOut"),
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(1),
                    Setters =
                    {
                        new Setter
                        {
                            Property = OpacityProperty,
                            Value = 1.0
                        }
                    }
                }
            }
        }.RunAsync(this);
        if (Width <= 0)
        {
            new Animation
            {
                Duration = TimeSpan.FromMilliseconds(500),
                FillMode = FillMode.Forward,
                Easing = Easing.Parse("CubicEaseOut"),
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(1),
                        Setters =
                        {
                            new Setter
                            {
                                Property = WidthProperty,
                                Value = 200.0
                            }
                        }
                    }
                }
            }.RunAsync(this);
        }
    }

    public void Refresh()
    {
        var track = DataStateService.ActiveTrack;
        if(track != null)
        {
            MixerName.Content = track.Name;
            MixerPan.ValuerColor = Brush.Parse(track.Color);
            MixerVol.ValuerColor = Brush.Parse(track.Color);
            MixerColor.Background = Brush.Parse(track.Color);
            Console.WriteLine($"TrackName: {track.Name}");
            Console.WriteLine($"Track Color: {track.Color}\n" +
                              $"Mixer Color: {MixerPan.ValuerColor}");
            LevelMeter.Track =  track;
            LevelMeter.InvalidateVisual();
        }
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        MixerColor.BoxShadow = BoxShadows.Parse($"0 0 25 2 {DataStateService.ActiveTrack.Color}");
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        MixerColor.BoxShadow = BoxShadows.Parse("0 0 0 0 transparent");
    }


    public override void Render(DrawingContext context)
    {
        base.Render(context);
    }
    
}