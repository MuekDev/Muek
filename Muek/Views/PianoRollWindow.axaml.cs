using System;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;

namespace Muek.Views;

public partial class PianoRollWindow : UserControl
{
    public PianoRollWindow()
    {
        InitializeComponent();
        ClipToBounds = false;
    }

    private void Hide(object? sender, RoutedEventArgs e)
    {
        if(Height >= 400.0)
        {
            CloseButton.IsVisible = false;
            OpenButton.IsVisible = true;
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
                                Property = HeightProperty,
                                Value = PatternPreview.Height + TopBar.Height
                            }
                        }
                    }
                }
            }.RunAsync(this);
        }
    }

    private void Show(object? sender, RoutedEventArgs e)
    {
        if(Height <= PatternPreview.Height + TopBar.Height)
        {
            CloseButton.IsVisible = true;
            OpenButton.IsVisible = false;
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
                                Property = HeightProperty,
                                Value = 400.0
                            }
                        }
                    }
                }
            }.RunAsync(this);
        }
    }

    private void ScrollChange(object? sender, ScrollChangedEventArgs e)
    {
        if (e.Source != null && e.Source.Equals(PianoRollLeft))
        {
            EditArea.Height = PianoBar.Height;
            PianoRollRight.Offset = new Vector(PianoRollRight.Offset.X,PianoRollLeft.Offset.Y);
            EditArea.NoteHeight = PianoBar.NoteHeight;
        }

        if (e.Source != null && e.Source.Equals(PianoRollRight))
        {
            PianoBar.Height = EditArea.Height;
            PianoRollLeft.Offset = new Vector(0,PianoRollRight.Offset.Y);
            PianoBar.NoteHeight = EditArea.NoteHeight;
            
        }
        EditArea.ScrollOffset = PianoRollRight.Offset.Y;
        EditArea.ClampValue = PianoRollRight.Offset.X;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        
        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            PianoRollRight.Offset = new Vector(PianoRollRight.Offset.X - e.Delta.Y * 44,PianoRollRight.Offset.Y );
            PianoRollRight.Offset = new Vector(Math.Max(0, PianoRollRight.Offset.X),PianoRollRight.Offset.Y) ; // 不允许左滚超过0
            
            InvalidateVisual();
            e.Handled = true;
        }
    }

    private void ImportMidiFile(object? sender, RoutedEventArgs e)
    {
        EditArea.ImportMidi();
        e.Handled = true;
    }

    private void MagnetPropertyChange(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        EditArea.Magnet = MagnetSettingsWindow.SelectedGrid.Value;
        EditArea.InvalidateVisual();
    }
}