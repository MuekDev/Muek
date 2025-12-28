using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Muek.Services;
using Muek.ViewModels;

namespace Muek.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataStateService.PianoRollWindow = PianoRollWindow;
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

    public void DisableFx(object sender, RoutedEventArgs args)
    {
        //TODO
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

    private void AddPlugin(object? sender, RoutedEventArgs e)
    {
        var window = new PluginManagerWindow();
        window.ShowDialog(this);
    }

    private void SettingsButton(object? sender, RoutedEventArgs e)
    {
        if (Settings.IsShowing && ProjectInfo.IsShowing)
        {
            Settings.Hide();
            ProjectInfo.Hide();
            Console.WriteLine("HIDE SETTINGS");
        }
        else
        {
            Settings.Show();
            ProjectInfo.Show();
            Console.WriteLine("SHOW SETTINGS");
        }
    }

    private void TrackHead_OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            InvalidateVisual();
            TrackViewControl.OffsetY = TrackHeadScrollViewer.Offset.Y;
        }, DispatcherPriority.Render);
    }
}