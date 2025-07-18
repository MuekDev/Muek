using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Muek.Services;
using Muek.ViewModels;

namespace Muek.Views;

public partial class Mixer : UserControl
{
    //TODO
    private TrackViewModel? _currentTrack = DataStateService.GetSelectedTrack();
    private string _trackColor;
    private string _trackName;
    public Mixer()
    {
        this.IsVisible = _currentTrack != null;
        InitializeComponent();
        _trackColor = _currentTrack?.Color;
        _trackName = _currentTrack?.Name;
    }
}