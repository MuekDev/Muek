//设置了一个TRACKUID，需要改在这改
global using TRACKUID = string;

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
        // mvm.AddTrack();
    }

    public void RemoveTrack(object sender, RoutedEventArgs args)
    {
        var button = sender as Button;
        MainWindowViewModel mvm = DataContext as MainWindowViewModel;
        // mvm.RemoveTrack((TRACKUID)button.Tag);
    }

    public void SelectTrack(object sender, RoutedEventArgs args)
    {
        var button = sender as Button;
        if (button.Tag != null)
        {
            MainWindowViewModel mvm = DataContext as MainWindowViewModel;
            // mvm.SelectTrack((TRACKUID)button.Tag);
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
