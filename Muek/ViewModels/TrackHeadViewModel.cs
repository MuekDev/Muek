using System;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Utils;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Muek.Views;

namespace Muek.ViewModels;

public class TrackHeadViewModel : Button
{
    private bool _switchable;
    private Point _pressedPosition;
    private bool _isFirstClickTrackHead;

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.ClickCount == 1)
        {
            _pressedPosition = e.GetPosition(Parent.Parent.Parent.Parent.Parent as Visual) / 100;
            //Console.WriteLine(_pressedPosition);

            _switchable = true;
            _isFirstClickTrackHead = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        //_releasedPosition =  e.GetPosition(this);
        if (_switchable)
        {
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
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        Console.WriteLine("Entered: " + Name);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_switchable)
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = desktop.MainWindow as MainWindow;
                var cont = mainWindow.ItemsControlX;
                
                var switchIndex = (int)e.GetPosition(cont).Y / 100 -
                                  (int)_pressedPosition.Y;
                Console.WriteLine(switchIndex);
            }

            if (_isFirstClickTrackHead)
            {
                new Animation
                {
                    Duration = TimeSpan.FromMilliseconds(200),
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