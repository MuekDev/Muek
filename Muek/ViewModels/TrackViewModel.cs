using System.Collections.Generic;
using Audio;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Muek.Services;
using Muek.Views;

namespace Muek.ViewModels;

public partial class TrackViewModel : ViewModelBase
{
    public Track Proto { get; }
    public string Color => Proto.Color;
    public string Id => Proto.Id;
    public string Index => Proto.Index.ToString();

    [ObservableProperty] private string _name;
    [ObservableProperty] private bool _selected = false;
    [ObservableProperty] private bool _byPassed = false;
    [ObservableProperty] private IBrush _byPassBtnColor = Brush.Parse("#D0FFE5");

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
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = desktop.MainWindow;
            if (mainWindow != null)
            {
                var window = new RenameWindow();
                window.ShowDialog(mainWindow);
                window.NameBox.Text = Name;
                window.Submit += (sender, s) => { Name = s; };
            }
        }
    }

    [RelayCommand]
    public void Remove()
    {
        DataStateService.RemoveTrack(Id);
    }

    [RelayCommand]
    public void OnTrackSelected()
    {
        foreach (var track in DataStateService.Tracks)
        {
            track.Selected = track.Id == Id;
        }
    }
}