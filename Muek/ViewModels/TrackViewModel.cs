using System.Collections.Generic;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Muek.Models;
using Muek.Services;
using Muek.Views;

namespace Muek.ViewModels;

public partial class TrackViewModel : ViewModelBase
{
    public Track Proto { get; }
    public string Color => Proto.Color;
    public string Id => Proto.Id;
    public int IntIndex => (int)Proto.Index;

    [ObservableProperty] private string _name;
    [ObservableProperty] private bool _selected = false;
    [ObservableProperty] private bool _byPassed = false;
    [ObservableProperty] private IBrush _byPassBtnColor = Brush.Parse("#D0FFE5");

    partial void OnSelectedChanged(bool value)
    {
        if (value)
        {
            DataStateService.ActiveTrack = this;
        }
    }

    partial void OnNameChanged(string value)
    {
        Proto.Name = value;
    }

    partial void OnByPassedChanged(bool value)
    {
        if (value)
        {
            ByPassBtnColor = Brush.Parse("#313131");
        }
        else
        {
            ByPassBtnColor = Brush.Parse("#D0FFE5");
        }
    }

    public List<ClipViewModel> Clips { get; } = new();

    public TrackViewModel(Track proto)
    {
        Proto = proto;

        // 初始化包装
        foreach (var clip in proto.Clips)
        {
            Clips.Add(new ClipViewModel(clip));
        }

        // 同步proto属性
        Name = proto.Name;
    }

    /// <summary>
    /// 添加 clip：会自动更新 proto.Clips 和本地 viewmodel
    /// </summary>
    public ClipViewModel AddClip(Clip clip)
    {
        var vm = new ClipViewModel(clip);
        Proto.Clips.Add(clip);
        Clips.Add(vm);
        vm.GenerateWaveformPreviewPure();

        return vm;
    }


    [RelayCommand]
    public void OnByPassButtonClick()
    {
        ByPassed = !ByPassed;
    }

    [RelayCommand]
    public void ShowRenameWindow()
    {
        var mainWindow = ViewHelper.GetMainWindow();
        var window = new RenameWindow();
        window.ShowDialog(mainWindow);
        window.NameBox.Text = Name;
        window.Submit += (sender, s) => { Name = s; };
    }

    [RelayCommand]
    public void Remove()
    {
        DataStateService.RemoveTrack(Id);
    }

    [RelayCommand]
    public void HandleTrackSelected()
    {
        foreach (var track in DataStateService.Tracks)
        {
            track.Selected = track.Id == Id;
        }
    }
    
    [RelayCommand]
    public void ShowRecolorWindow()
    {
        var recolorWindow = new RecolorWindow();
        recolorWindow.MyColorView.SelectedColor = Avalonia.Media.Color.Parse(Proto.Color);
        recolorWindow.Show();
        recolorWindow.Submit += (sender, color) =>
        {
           Proto.Color = color.ToString();
           OnPropertyChanged(nameof(Color));
           var mainWindow =  ViewHelper.GetMainWindow();
           mainWindow.TrackViewControl.InvalidateVisual();
        };
    }
}