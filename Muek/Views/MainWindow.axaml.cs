//设置了一个TRACKUID，需要改在这改
global using TRACKUID = long;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Muek.Services;
using Muek.ViewModels;
using Point = Avalonia.Point;

namespace Muek.Views;


public partial class MainWindow : Window
{
    private bool _moveTrack = false;

    public MainWindow()
    {
        InitializeComponent();
        
    }

    public void SyncTimeline(object type)
    {
        if (type is not TimeRulerBar)
        {
            TimeRulerBarControl.ScaleFactor = UiStateService.GlobalTimelineScale;
            TimeRulerBarControl.OffsetX = UiStateService.GlobalTimelineOffsetX;
        }

        if (type is not TrackView)
        {
            TrackViewControl.ScaleFactor = UiStateService.GlobalTimelineScale;
            TrackViewControl.OffsetX = UiStateService.GlobalTimelineOffsetX;
        }
    }

    public void AddNewTrack(object sender, RoutedEventArgs args)
    {
        MainWindowViewModel mvm = DataContext as MainWindowViewModel;
        mvm.AddTrack();
    }

    public void RemoveTrack(object sender, RoutedEventArgs args)
    {
        var button = sender as Button;
        MainWindowViewModel mvm = DataContext as MainWindowViewModel;
        mvm.RemoveTrack((TRACKUID)button.Tag);
    }

    public void SelectTrack(object sender, RoutedEventArgs args)
    {
        var button = sender as Button;
        if (button.Tag != null)
        {
            MainWindowViewModel mvm = DataContext as MainWindowViewModel;
            mvm.SelectTrack((TRACKUID)button.Tag);
            //InvalidateVisual();
        }
    }


    public void DisableFX(object sender, RoutedEventArgs args)
    {
        args.Handled = true;
    }


    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            this.BeginMoveDrag(e);
        }
    }
    
}
public class MuekTrack : Button
{
    private bool _switchable;
    private Point _pressedPosition;
    

    public MuekTrack()
    {
    }
    
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.ClickCount == 2)
        {
            
            _pressedPosition = e.GetPosition(Parent.Parent.Parent.Parent.Parent as Visual) / 100;
            //Console.WriteLine(_pressedPosition);
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
            _switchable = true;
            
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
        //Console.WriteLine("Entered: "+Name);
    }
    

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_switchable)
        {
            var switchIndex = (int)e.GetPosition(Parent.Parent.Parent.Parent.Parent as Visual).Y / 100 - (int)_pressedPosition.Y;
            Console.WriteLine(switchIndex);
            

        }
        
    }
    
}

