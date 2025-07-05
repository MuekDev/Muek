using System.Collections.Generic;
using Audio;

namespace Muek.ViewModels;

public class TrackViewModel
{
    public Track Proto { get; }
    public string Color => Proto.Color;

    public List<ClipViewModel> Clips { get; } = new();

    public TrackViewModel(Track proto)
    {
        Proto = proto;

        // 初始化包装
        foreach (var clip in proto.Clips)
        {
            Clips.Add(new ClipViewModel(clip));
        }
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
}