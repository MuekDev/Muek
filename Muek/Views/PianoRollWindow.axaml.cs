using System;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Muek.Models;

namespace Muek.Views;

public partial class PianoRollWindow : UserControl
{
    private bool _isShowing = false;
    private double _maxSize = 400.0;
    private bool _isDragging = false;
    public PianoRollWindow()
    {
        InitializeComponent();
        ClipToBounds = false;
        TopBar.PointerPressed += (sender, args) =>
        {
            if(_isShowing)
            {
                _isDragging = true;
                args.Handled = true;
            }
        };
        TopBar.PointerMoved += (sender, args) =>
        {
            if (_isDragging)
            {
                _maxSize = double.Clamp(Height - args.GetPosition(this).Y,90,600);
                Height = _maxSize;
                args.Handled = true;
            }
        };
        TopBar.PointerReleased += (sender, args) =>
        {
            _isDragging = false;
        };
    }

    private void Hide(object? sender, RoutedEventArgs e)
    {
        if(Height >= _maxSize)
        {
            _isShowing = false;
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
            _isShowing = true;
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
                                Value = _maxSize
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