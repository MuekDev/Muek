using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Muek.Helpers;
using Muek.Models;
using Muek.Services;
using Muek.ViewModels;
using NAudio.Wave;

namespace Muek.Views;

public partial class TrackView : UserControl
{
    private bool _isDraggingPlayhead;          // 是否在拖拽播放指针 （TODO: 改成ruler
    private double _offsetX;                   // 横向的轨道滚动距离
    private double _playHeadPosX;              // 播放指示指针
    private int _scaleFactor = 100;            // 横向的轨道缩放
    private double _timeRulerPosX;             // 好像没被使用
    private bool _isDropping = false;          // 是否正在向轨道上拖拽文件
    private Point _mousePosition = new();      // 鼠标指针位置
    public int TrackHeight = 100;              // 轨道高度（可变）
    private bool _isMovingClip;                // 是否在移动片段
    private bool _isResizingClipRight;         // 是否在调整片段长度
    private ClipViewModel? _activeClip = null; // 当前被激活的片段
    private double _lastClickedBeatOfClip = 0; // 最后一次点击clip title的位置（相对于clip，单位为beat）
    private bool _isResizingClipLeft;

    public TrackView()
    {
        InitializeComponent();
        Focusable = true;
        AddHandler(DragDrop.DropEvent, OnDrop);
        AddHandler(DragDrop.DragEnterEvent, OnDropEnter);
        AddHandler(DragDrop.DragLeaveEvent, OnDropLeave);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);

        UiStateService.GlobalPlayHeadPosXUpdated += UiStateServiceOnGlobalPlayHeadPosXUpdated;
    }

    private void UiStateServiceOnGlobalPlayHeadPosXUpdated(object? sender, double e)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() => { PlayHeadPosX = e; });
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        _mousePosition = e.GetPosition(this);
        InvalidateVisual();
    }

    private void OnDropLeave(object? sender, DragEventArgs e)
    {
        _isDropping = false;
    }

    public new IBrush? Background { get; set; } = Brushes.Transparent;

    public double TimeRulerPosX
    {
        get => _timeRulerPosX;
        set
        {
            if (Math.Abs(_timeRulerPosX - value) < 0.01) return;
            if (value < 0) return;
            _timeRulerPosX = value;
            InvalidateVisual();
        }
    }

    public double PlayHeadPosX
    {
        get => _playHeadPosX;
        set
        {
            if (Math.Abs(_playHeadPosX - value) < 0.01) return;
            if (value < 0) return;
            _playHeadPosX = value;
            InvalidateVisual();
        }
    }

    public double OffsetX
    {
        get => _offsetX;
        set
        {
            if (Math.Abs(_offsetX - value) < 0.01)
                return;

            if (value < 0)
                value = 0;

            _offsetX = value;
            InvalidateVisual();
        }
    }


    public int ScaleFactor
    {
        get => _scaleFactor;
        set
        {
            _scaleFactor = value;
            InvalidateVisual(); // 重新调用 Render
        }
    }

    public static int Subdivisions => DataStateService.Subdivisions;


    private void OnDropEnter(object? sender, DragEventArgs e)
    {
        _isDropping = true;
    }

    [Obsolete("老子就用GetFileNames能怎么滴")]
    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
        {
            var files = e.Data.GetFileNames()?.ToList();
            if (files == null) return;
            Console.WriteLine($"[TrackView] Dropped: {files.Count}");

            var (x, y) = e.GetPosition(this);

            // var beat = (int)Math.Floor(x / ScaleFactor);
            x = e.GetPosition(this).X + OffsetX;
            var beat = x / ScaleFactor;
            beat = double.Round(beat * Subdivisions, 0) / Subdivisions;

            var trackIndex = (int)Math.Floor(y / TrackHeight);

            
            //如果在最后一行，则创建新轨道
            if (trackIndex == DataStateService.Tracks.Count)
            {
                new MainWindowViewModel().AddTrack();
            }
            
            
            Console.WriteLine($"Dropped at beat: {beat}, track: {trackIndex}");

            foreach (var file in files)
            {
                if (!Path.Exists(file)) continue;

                var ext = Path.GetExtension(file).ToLower();
                if (ext != ".wav" && ext != ".mp3" && ext != ".ogg")
                    continue;

                var durationSec = GetAudioDurationInSeconds(file);
                var durationBeats = (durationSec / 60f) * DataStateService.Bpm / Subdivisions;

                var newClip = new Clip
                {
                    Name = Path.GetFileNameWithoutExtension(file),
                    StartBeat = beat,
                    Duration = durationBeats,
                    Path = file,
                    Id = Guid.NewGuid().ToString(),
                    CachedWaveform = AudioService.DecodeFromFile(file,48000,2).ToArray()
                };

                // 确保轨道存在
                if (trackIndex >= 0 && trackIndex < DataStateService.Tracks.Count)
                {
                    DataStateService.Tracks[trackIndex].AddClip(newClip);
                }

                var track = DataStateService.Tracks[trackIndex].Proto;
                // HandleNewClipCommand.Execute(track, newClip);
                // TODO
            }

            _isDropping = false;
            InvalidateVisual();
        }
    }

    private float GetAudioDurationInSeconds(string path)
    {
        try
        {
            using var audio = new AudioFileReader(path);
            return (float)audio.TotalTime.TotalSeconds;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get duration: {ex.Message}");
            return 0f;
        }
    }


    public override void Render(DrawingContext context)
    {
        var renderSize = Bounds.Size;
        var brushWhite = new SolidColorBrush(Colors.Gray);
        var brushGray = new SolidColorBrush(Colors.DimGray, 0.5);
        var penWhite = new Pen(brushWhite);
        var penGray = new Pen(brushGray);

        if (Background != null)
            context.FillRectangle(Background, new Rect(renderSize));

        //// 绘制交替色的背景

        var beatStart = (int)(OffsetX / ScaleFactor);
        var beatEnd = (int)((OffsetX + renderSize.Width) / ScaleFactor) + 1;

        for (var beat = beatStart; beat < beatEnd; beat++)
        {
            var groupIndex = beat / Subdivisions % 2; // 0 or 1，看拍数是奇数还是偶数了

            var color = groupIndex == 0 ? Color.Parse("#353535") : Color.Parse("#2A2A2A");
            var brush = new SolidColorBrush(color);

            var x1 = beat * ScaleFactor - OffsetX;
            var x2 = (beat + 1) * ScaleFactor - OffsetX;

            // 不绘制边界外的
            if (x2 < 0 || x1 > renderSize.Width)
                continue;

            context.FillRectangle(brush, new Rect(x1, 0, x2 - x1, renderSize.Height));
        }

        //// 绘制片段
        for (var i = 0; i < DataStateService.Tracks.Count; i++)
        {
            var track = DataStateService.Tracks[i];
            foreach (var clip in track.Clips)
            {
                var x = clip.StartBeat * ScaleFactor - OffsetX;
                var width = clip.Duration * ScaleFactor;

                if (x + width < 0 || x > renderSize.Width)
                    continue; // 跳过可视区域外的片段

                var rect = new Rect(x, i * TrackHeight, width, TrackHeight);
                _ = Color.TryParse(track.Color, out var color) ? color : DataStateService.MuekColor;
                var background = new SolidColorBrush(color);

                context.FillRectangle(background, rect);
                context.DrawRectangle(new Pen(Brushes.Black), rect);

                // 渲染波形
                if (clip.CachedWaveform is { Length: > 1 })
                {
                    var waveform = clip.CachedWaveform;

                    // 根据 clip.Duration 和 clip.Offset 裁剪波形（单位: 样本）
                    int totalSamples = waveform.Length;
                    var durationRatio = (float)clip.Duration / clip.SourceDuration;
                    var offsetRatio = (float)clip.Offset / clip.SourceDuration;


                    int startSample = (int)(offsetRatio * totalSamples);
                    int endSample = (int)((offsetRatio + durationRatio) * totalSamples);
                    startSample = Math.Clamp(startSample, 0, totalSamples - 1);
                    endSample = Math.Clamp(endSample, 0, totalSamples);

                    waveform = waveform.Slice(startSample, endSample - startSample);


                    var centerY = i * TrackHeight + TrackHeight / 2;
                    var scaleY = (TrackHeight / 2.0) * 0.95;

                    var pixelWidth = (int)Math.Ceiling(width);
                    if (pixelWidth <= 1) return;

                    var samplesPerPixel = waveform.Length / pixelWidth;
                    samplesPerPixel = Math.Max(1, samplesPerPixel);

                    var geometry = new StreamGeometry();
                    using (var ctx = geometry.Open())
                    {
                        if (samplesPerPixel <= 50)
                        {
                            // 高分辨率精细一点，用折线图（此乃盗窃reaper之秘术
                            var waveformCount = waveform.Length;

                            var visibleStartX = Math.Max(0, -x); // 视口左边相对波形起点的偏移，单位像素
                            var visibleEndX = Math.Min(width, renderSize.Width - x);

                            var visibleStartSample = (int)(visibleStartX / width * waveformCount);
                            var visibleEndSample = (int)(visibleEndX / width * waveformCount);

                            visibleStartSample = Math.Clamp(visibleStartSample, 0, waveformCount - 1);
                            visibleEndSample = Math.Clamp(visibleEndSample, 0, waveformCount - 1);

                            var visibleSampleCount = visibleEndSample - visibleStartSample + 1;
                            if (visibleSampleCount <= 1) return;

                            var stepX = width / (waveformCount - 1);

                            ctx.BeginFigure(
                                new Point(x + visibleStartSample * stepX,
                                    centerY - waveform[visibleStartSample] * scaleY), false);

                            for (var s = visibleStartSample + 1; s <= visibleEndSample; s++)
                            {
                                var px = x + s * stepX;
                                var py = centerY - waveform[s] * scaleY;
                                ctx.LineTo(new Point(px, py));
                            }
                        }
                        else
                        {
                            // 低分辨率乃Min-Max 模式，压缩采样段（TODO: 有时候会一闪一闪的
                            var renderStartPx = Math.Max(0, (int)Math.Floor(-x));
                            var renderEndPx = Math.Min(pixelWidth, (int)Math.Ceiling(renderSize.Width - x));
                            if (renderStartPx >= renderEndPx) return;

                            for (var px = renderStartPx; px < renderEndPx; px++)
                            {
                                var start = px * samplesPerPixel;
                                var end = Math.Min(start + samplesPerPixel, waveform.Length);

                                float min = 0, max = 0;
                                for (var j = start; j < end; j++)
                                {
                                    var s = waveform[j];
                                    if (j == start || s > max) max = s;
                                    if (j == start || s < min) min = s;
                                }

                                var drawX = x + px;
                                var y1 = centerY - max * scaleY;
                                var y2 = centerY - min * scaleY;

                                ctx.BeginFigure(new Point(drawX, y1), false);
                                ctx.LineTo(new Point(drawX, y2));
                            }
                        }
                    }

                    context.DrawGeometry(null, new Pen(Brushes.Black, 0.8), geometry);
                }


                context.DrawText(
                    new FormattedText(clip.Name, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                        Typeface.Default, 10, Brushes.Black),
                    new Point(x, i * TrackHeight));
            }
        }

        //// 绘制Dropping的临时对象
        if (_isDropping)
        {
            var x = _mousePosition.X;
            x = double.Round(x*Subdivisions/_scaleFactor,0)/Subdivisions * ScaleFactor - OffsetX%
                (ScaleFactor / (double)Subdivisions);
            var y = Math.Floor(_mousePosition.Y / TrackHeight) * TrackHeight;
            var index = (int)y /  TrackHeight;
            if((DataStateService.Tracks.Count) >= index)
            {
                
                var rect = new Rect(x, y, 100, TrackHeight);
                var background = new LinearGradientBrush();
                background.StartPoint = new RelativePoint(0.0, 0.5, RelativeUnit.Relative);
                background.EndPoint = new RelativePoint(1.0, 0.5, RelativeUnit.Relative);
                if(DataStateService.Tracks.Count != index)
                {
                    background.GradientStops.Add(
                        new GradientStop(Color.Parse(DataStateService.Tracks[index].Color), 0.0));
                    background.GradientStops.Add(new GradientStop(Colors.Transparent, 1.0));
                }
                else
                {
                    background.GradientStops.Add(
                        new GradientStop(Colors.DimGray, 0.0));
                    background.GradientStops.Add(new GradientStop(Colors.Transparent, 1.0));
                }

                context.FillRectangle(background, rect);
            }
        }


        //// 绘制刻度网格
        var step = Math.Max(1, ScaleFactor);

        var subStep = (double)step / Subdivisions;

        for (double x = 0; x < renderSize.Width + OffsetX; x += subStep)
        {
            var drawX = Math.Round(x - OffsetX); // 防止抗锯齿导致线丢失
            var isMainLine = Math.Abs(x % step) < 0.1;

            if (!isMainLine && ScaleFactor < 33) continue;

            var pen = isMainLine ? penWhite : penGray;

            context.DrawLine(pen, new Point(drawX, 0), new Point(drawX, renderSize.Height));
        }


        //// 绘制播放线
        var playheadX = PlayHeadPosX * ScaleFactor - OffsetX;

        if (playheadX >= 0 && playheadX <= renderSize.Width)
        {
            var playheadPen = new Pen(Brushes.White);

            context.DrawLine(playheadPen,
                new Point(playheadX, 0),
                new Point(playheadX, renderSize.Height));

            var label = $"{(PlayHeadPosX * 4 + 1):0.0} ({(PlayHeadPosX + 1):0.0})";
            context.DrawText(
                new FormattedText(label, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                    Typeface.Default, 10, Brushes.White),
                new Point(playheadX + 4, 4));
        }

        base.Render(context);
    }

    private void UpdatePlayHeadFromPointer(Point point)
    {
        // pointer.X 是当前控件内部位置，+ OffsetX 得到全局位置
        var globalX = Math.Max(0, point.X + OffsetX);
        PlayHeadPosX = globalX / ScaleFactor;
        PlayHeadPosX = double.Round(PlayHeadPosX *Subdivisions,0) /Subdivisions;
        Console.WriteLine($"PlayHeadPosX: {PlayHeadPosX}");
        UiStateService.GlobalPlayHeadPosX = PlayHeadPosX;

        // TODO: 同步节拍
        var currentBeat = PlayHeadPosX / ScaleFactor;
        // UiStateService.CurrentBeatPosition = currentBeat;

        // InvalidateVisual(); // 重绘
    }


    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var point = e.GetPosition(this);
        var props = e.GetCurrentPoint(this).Properties;

        if (props.IsLeftButtonPressed)
        {
            UpdateTrackSelect();

            var state = GetClipInteractionMode(); // HACK: 此处设置了_activeClip
            if (state == ClipInteractionMode.None)
            {
                _isDraggingPlayhead = true;
                UpdatePlayHeadFromPointer(point);
            }
            else if (state == ClipInteractionMode.OnTopTitle)
            {
                if (_activeClip == null)
                    return;

                _isMovingClip = true;

                var globalX = Math.Max(0, _mousePosition.X + OffsetX);
                var pointerBeat = globalX / ScaleFactor;
                var clickBeat = pointerBeat - _activeClip.StartBeat;
                _lastClickedBeatOfClip = clickBeat;
            }
            else if (state == ClipInteractionMode.OnRight)
            {
                if (_activeClip == null)
                    return;

                _isResizingClipRight = true;
            }
            else if (state == ClipInteractionMode.OnLeft)
            {
                if (_activeClip == null)
                    return;

                _isResizingClipLeft = true;
            }

            e.Handled = true; // 避免冒泡
        }
    }

    private void UpdateTrackSelect()
    {
        var trackIndex = (int)Math.Floor(_mousePosition.Y / TrackHeight);

        if (trackIndex < 0 || trackIndex >= DataStateService.Tracks.Count)
        {
            return;
        }

        var track = DataStateService.Tracks[trackIndex];
        track.HandleTrackSelected();
    }

    /// <summary>
    /// 用于判断鼠标指针是否在clip的特殊区域上
    /// </summary>
    /// <returns>
    /// `ClipInteractionMode`: 鼠标所处状态
    /// </returns>
    private ClipInteractionMode GetClipInteractionMode()
    {
        if (_isMovingClip || _isDraggingPlayhead || _isResizingClipRight || _isResizingClipLeft)
            return ClipInteractionMode.None;

        var trackIndex = (int)Math.Floor(_mousePosition.Y / TrackHeight);
        var relativeMouseY = (_mousePosition.Y % TrackHeight) / TrackHeight; // 0~1

        if (trackIndex < 0 || trackIndex >= DataStateService.Tracks.Count)
        {
            Cursor = new Cursor(StandardCursorType.Ibeam);
            _activeClip = null;
            return ClipInteractionMode.None;
        }

        var track = DataStateService.Tracks[trackIndex];
        var globalX = Math.Max(0, _mousePosition.X + OffsetX);
        var pointerBeat = globalX / ScaleFactor;

        foreach (var clip in track.Clips)
        {
            var clipStart = clip.StartBeat;
            var clipEnd = clip.StartBeat + clip.Duration;

            if (pointerBeat >= clipStart && pointerBeat <= clipEnd)
            {
                _activeClip = clip;
                var relativeBeat = pointerBeat - clipStart;
                var scaledPosX = relativeBeat * ScaleFactor;

                // 顶部标题区域（如用于拖动、显示名等）
                if (relativeMouseY < 0.2)
                {
                    return ClipInteractionMode.OnTopTitle;
                }

                // 片段左端
                if (scaledPosX is > 0 and < 5)
                {
                    return ClipInteractionMode.OnLeft;
                }

                // 片段右侧
                if (scaledPosX > clip.Duration * ScaleFactor - 5 &&
                    scaledPosX < clip.Duration * ScaleFactor)
                {
                    return ClipInteractionMode.OnRight;
                }

                // 其他区域视为 clip body
                return ClipInteractionMode.InClipBody;
            }
        }

        // 没找到匹配的 clip
        _activeClip = null;
        return ClipInteractionMode.None;
    }


    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var point = e.GetPosition(this);
            if (_isDraggingPlayhead)
            {
                UpdatePlayHeadFromPointer(point);
                e.Handled = true;
            }
            else if (_isMovingClip)
            {
                MoveActiveClipTo(point);
            }
            else if (_isResizingClipRight)
            {
                ReDurationForActiveClip(point);
            }
            else if (_isResizingClipLeft)
            {
                ReOffsetForActiveClip(point);
            }
        }

        _mousePosition = e.GetPosition(this);

        CheckIfPointerInClipBounds(_mousePosition);
    }

    private void ReOffsetForActiveClip(Point point)
    {
        var globalX = Math.Max(0, point.X + OffsetX);
        var pointerBeat = globalX / ScaleFactor;

        if (_activeClip != null)
        {
            var newStart = pointerBeat;
            var oldStart = _activeClip.StartBeat;

            // 计算偏移的差值
            var delta = newStart - oldStart;

            if (delta != 0)
            {
                var newOffset = _activeClip.Proto.Offset + delta;
                var newDuration = _activeClip.Proto.Duration - delta;

                if (newOffset >= 0 && newDuration > 0.1)
                {
                    _activeClip.Proto.StartBeat = newStart;
                    _activeClip.Proto.Offset = newOffset;
                    _activeClip.Proto.Duration = newDuration;

                    // if (DataStateService.ActiveTrack?.Proto != null)
                        // TODO
                        // ReOffsetCommand.Execute(DataStateService.ActiveTrack.Proto, _activeClip.Proto);

                    InvalidateVisual();
                }
            }
        }
    }


    private void ReDurationForActiveClip(Point point)
    {
        var globalX = Math.Max(0, point.X + OffsetX);
        var pointerBeat = globalX / ScaleFactor;

        if (_activeClip != null && _activeClip.StartBeat < pointerBeat)
        {
            var newDuration = pointerBeat - _activeClip.StartBeat;
            if (newDuration < 0.1 || newDuration > _activeClip.SourceDuration)
                return;
            _activeClip.Proto.Duration = newDuration;
            // TODO
            // if (DataStateService.ActiveTrack?.Proto != null)
                // ReDurationCommand.Execute(DataStateService.ActiveTrack.Proto, _activeClip.Proto, _activeClip.Duration);
            InvalidateVisual();
        }
    }

    private void MoveActiveClipTo(Point point)
    {
        if (_activeClip == null)
            return;

        var globalX = Math.Max(0, point.X + OffsetX);
        var pointerBeat = globalX / ScaleFactor - _lastClickedBeatOfClip;
        pointerBeat = double.Round(pointerBeat*Subdivisions,0)/Subdivisions;
        if (pointerBeat > 0)
            _activeClip.Proto.StartBeat = pointerBeat;
        
        // TODO
        // if (DataStateService.ActiveTrack?.Proto != null)
            // MoveCommand.Execute(DataStateService.ActiveTrack.Proto, _activeClip.Proto);

        InvalidateVisual();
    }

    private void CheckIfPointerInClipBounds(Point point)
    {
        var state = GetClipInteractionMode();
        Cursor = state switch
        {
            ClipInteractionMode.None => new Cursor(StandardCursorType.Ibeam),
            ClipInteractionMode.OnTopTitle => new Cursor(StandardCursorType.Arrow),
            ClipInteractionMode.InClipBody => new Cursor(StandardCursorType.Cross),
            ClipInteractionMode.OnLeft => new Cursor(StandardCursorType.LeftSide),
            ClipInteractionMode.OnRight => new Cursor(StandardCursorType.RightSide),
            _ => Cursor
        };
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isDraggingPlayhead = false;
        _isMovingClip = false;
        _isResizingClipRight = false;
        _isResizingClipLeft = false;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            OffsetX -= e.Delta.Y * 44;
            OffsetX = Math.Max(0, OffsetX); // 不允许左滚超过0
            InvalidateVisual();
            e.Handled = true;

            UiStateService.GlobalTimelineScale = ScaleFactor;
            UiStateService.GlobalTimelineOffsetX = OffsetX;
            var parent = this.GetVisualAncestors().OfType<MainWindow>().FirstOrDefault();
            parent?.SyncTimeline(this);

            return;
        }

        var delta = e.Delta.Y;
        if (delta != 0)
        {
            var factor = delta > 0 ? 1.1 : 0.9; // 放大10%，缩小10%
            var pointerX = e.GetPosition(this).X;

            ZoomAt(pointerX, factor);

            UiStateService.GlobalTimelineScale = ScaleFactor;
            UiStateService.GlobalTimelineOffsetX = OffsetX;
            var parent = this.GetVisualAncestors().OfType<MainWindow>().FirstOrDefault();
            parent?.SyncTimeline(this);
        }
    }

    private void ZoomAt(double pointerX, double zoomFactor)
    {
        var oldScale = ScaleFactor;
        var newScale = (int)Math.Clamp(oldScale * zoomFactor, 10, 100000);

        if (newScale == oldScale)
            return;

        // 鼠标所对应的节拍位置
        var beatAtPointer = (pointerX + OffsetX) / oldScale;

        // 更新缩放
        ScaleFactor = newScale;

        // 调整 OffsetX，使 beatAtPointer 依然出现在原位置
        OffsetX = beatAtPointer * newScale - pointerX;
    }
}

internal enum ClipInteractionMode
{
    OnTopTitle,
    InClipBody,
    OnLeft,
    OnRight,
    None,
}