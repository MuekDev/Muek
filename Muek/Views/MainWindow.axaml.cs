using System;
using Avalonia.Controls;
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
            TimeRulerBarControl.ScaleFactor = UiStateService.GlobalTimeLineScale;
        if (type is not TrackView)
            TrackViewControl.ScaleFactor = UiStateService.GlobalTimeLineScale;
    }
}