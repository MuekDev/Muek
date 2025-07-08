using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Runtime.InteropServices.JavaScript;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Muek.Services;
using Muek.ViewModels;

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

    public void AddNewTrack(object sender, RoutedEventArgs args)
    {
        MainWindowViewModel mvm = DataContext as MainWindowViewModel;
        mvm.addTrack();
    }

    public void SelectTrack(object sender, RoutedEventArgs args)
    {
        
    }
    public void RemoveTrack(object sender, RoutedEventArgs args)
    {
        
    }
}
