using System;
using System.Linq;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Utils;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Muek.Models;
using Muek.Services;
using Muek.Views;

namespace Muek.ViewModels;

public class TrackHeadViewModel : Button
{
    private bool _switchable;
    private Point _pressedPosition;
    private bool _isFirstClickTrackHead;
    private int _moveToIndexDelta;

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.ClickCount == 1 && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _pressedPosition = e.GetPosition(Parent.Parent.Parent.Parent.Parent as Visual) / 100;
            //Console.WriteLine(_pressedPosition);

            _switchable = true;
            _isFirstClickTrackHead = true;

            var mainWindow = ViewHelper.GetMainWindow();
            mainWindow.Mixer.ShowMixer();
            //mainWindow.TrackLineDrawer.IsVisible = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        // var releasedPosition =  e.GetPosition(this);
        var mainWindow = ViewHelper.GetMainWindow();

        if (_switchable)
        {
            
            var track = DataStateService.Tracks.FirstOrDefault(t => t.Id == Name);

            if (track != null)
            {
                var oldIndex = track.IntIndex;
                var newIndex = oldIndex + _moveToIndexDelta;

                if (newIndex >= 0 && newIndex < DataStateService.Tracks.Count)
                {
                    DataStateService.Tracks.Move(oldIndex, newIndex);

                    for (var i = 0; i < DataStateService.Tracks.Count; i++)
                    {
                        DataStateService.Tracks[i].Proto.Index = (uint)i;
                    }
                }

                mainWindow.TrackViewControl.InvalidateVisual();
            }
            
            new Animation
            {
                Duration = TimeSpan.FromMilliseconds(200),
                FillMode = FillMode.Forward,
                Easing = Easing.Parse("CubicEaseOut"),
                Delay = TimeSpan.FromMilliseconds(200),
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(1),
                        Setters =
                        {
                            new Setter
                            {
                                Property = TranslateTransform.XProperty,
                                Value = 0.0
                            }
                        }
                    }
                }
            }.RunAsync(this);
        }
        _switchable = false;

        mainWindow.TrackLineDrawer.IsVisible = false;
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        // base.OnPointerEntered(e);
        Console.WriteLine("Entered: " + Name);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_switchable)
        {
            var mainWindow = ViewHelper.GetMainWindow();
            var cont = mainWindow.ItemsControlX;

            var switchIndex = (int)e.GetPosition(cont).Y / 100 -
                              (int)_pressedPosition.Y;
            
            
            _moveToIndexDelta = switchIndex;

            var track = DataStateService.Tracks.FirstOrDefault(t => t.Id == Name);
            if (track != null)
            {
                var fromIndex = track.IntIndex;
                var toIndex = fromIndex + switchIndex;

                toIndex = Math.Clamp(toIndex, 0, DataStateService.Tracks.Count);

                var draggingDown = toIndex > fromIndex;

                if (switchIndex != 0)
                {
                    var visualIndex = draggingDown ? toIndex + 1 : toIndex;
                    visualIndex = Math.Clamp(visualIndex, 0, DataStateService.Tracks.Count);
                    mainWindow.TrackLineDrawer.LineY = visualIndex * 100;
                    mainWindow.TrackLineDrawer.IsVisible = true;
                }
                else
                {
                    mainWindow.TrackLineDrawer.IsVisible = false;
                }
            }

            Console.WriteLine(switchIndex);

            if (_isFirstClickTrackHead)
            {
                new Animation
                {
                    Duration = TimeSpan.FromMilliseconds(200.0),
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
                                    Property = TranslateTransform.XProperty,
                                    Value = 10.0
                                }
                            }
                        }
                    }
                }.RunAsync(this);
                _isFirstClickTrackHead = false;
            }
        }
    }
}