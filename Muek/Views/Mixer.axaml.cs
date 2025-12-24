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
    private bool _isDragging = false;
    private double _maxSize = 200;
    public Mixer()
    {
        InitializeComponent();
        Width = 0;
        Console.WriteLine("Mixer Initialized");
        ResizePanel.PointerPressed += (sender, args) =>
        {
            _isDragging = true;
            args.Handled = true;
        };
        ResizePanel.PointerMoved += (sender, args) =>
        {
            if (!_isDragging) return;
            _maxSize = double.Clamp(Width - args.GetPosition(this).X,120,600);
            Width = _maxSize;
            args.Handled = true;
        };
        ResizePanel.PointerReleased += (sender, args) =>
        {
            _isDragging = false;
            ResizeBorder.IsVisible = false;
            Cursor = new Cursor(StandardCursorType.Arrow);
        };
        ResizePanel.PointerEntered += (sender, args) =>
        {
            ResizeBorder.IsVisible = true;
            Cursor = new Cursor(StandardCursorType.LeftSide);
        };
        ResizePanel.PointerExited += (sender, args) =>
        {
            if(_isDragging) return;
            ResizeBorder.IsVisible = false;
            Cursor = new Cursor(StandardCursorType.Arrow);
        };
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
                                Value = _maxSize
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
            LevelMeter.Track = track;
            LevelMeter.InvalidateVisual();
        }
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        // MixerColor.BoxShadow = BoxShadows.Parse($"0 0 25 2 {DataStateService.ActiveTrack.Color}");
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        // MixerColor.BoxShadow = BoxShadows.Parse("0 0 0 0 transparent");
    }


    public override void Render(DrawingContext context)
    {
        base.Render(context);
    }

    private void PeakRmsSelected(object? sender, RoutedEventArgs e)
    {
        MeterModeButton.Content = "Peak/RMS";
        LevelMeter.Mode = MixerLevelMeter.LevelMeterMode.PeakRms;
        InvalidateVisual();
        LevelMeter.InvalidateVisual();
    }

    private void PeakSelected(object? sender, RoutedEventArgs e)
    {
        MeterModeButton.Content = "Peak";
        LevelMeter.Mode =  MixerLevelMeter.LevelMeterMode.Peak;
        InvalidateVisual();
    }

    private void RmsSelected(object? sender, RoutedEventArgs e)
    {
        MeterModeButton.Content = "RMS";
        LevelMeter.Mode = MixerLevelMeter.LevelMeterMode.Rms;
        InvalidateVisual();
    }
}