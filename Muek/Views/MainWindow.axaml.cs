//设置了一个TRACK_UID，需要改在这改
global using TRACK_UID = string;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Muek.Services;

namespace Muek.Views;


public partial class MainWindow : Window
{
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

    public void DisableFx(object sender, RoutedEventArgs args)
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
