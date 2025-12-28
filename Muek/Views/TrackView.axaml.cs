using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Input;
using Muek.Engine;
using Muek.Helpers;
using Muek.Models;
using Muek.Services;
using Muek.ViewModels;
using NAudio.Midi;
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

    public ClipViewModel? ActiveClip
    {
        get => _activeClip;
        set
        {
            _activeClip = value;
            InvalidateVisual();
        }
    }
    
    private MainWindow? _mainWindow;
    private double _lastClickedBeatOfClip = 0; // 最后一次点击clip title的位置（相对于clip，单位为beat）
    private bool _isResizingClipLeft;
    private bool _isSnapping = true;

    private double _offsetY;

    public double OffsetY
    {
        get => _offsetY;
        set
        {
            // 1. 计算限制
            var newOffset = Math.Clamp(value, 0, MaxOffsetY);

            if (Math.Abs(_offsetY - newOffset) < 0.01) return;

            _offsetY = newOffset;

            // 2. 同步左侧 (TrackHeadScrollViewer)
            _mainWindow ??= ViewHelper.GetMainWindow();

            if (_mainWindow != null)
            {
                var sv = _mainWindow.TrackHeadScrollViewer;
                sv.Offset = new Vector(sv.Offset.X, _offsetY);
            }

            InvalidateVisual();
        }
    }

    private double MaxOffsetY
    {
        get
        {
            var totalContentHeight = DataStateService.Tracks.Count * TrackHeight;

            // 预留 1 个轨道高度，方便拖拽创建新轨
            totalContentHeight += TrackHeight;

            // 视口高度
            var viewportHeight = Bounds.Height;

            return Math.Max(0, totalContentHeight - viewportHeight);
        }
    }

    private CancellationTokenSource? _trackerCts;
    
    public TrackView()
    {
        InitializeComponent();

        AddHandler(DragDrop.DropEvent, OnDrop);
        AddHandler(DragDrop.DragEnterEvent, OnDropEnter);
        AddHandler(DragDrop.DragLeaveEvent, OnDropLeave);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        
        UiStateService.GlobalPlayHeadPosXUpdated += UiStateServiceOnGlobalPlayHeadPosXUpdated;

        AudioService.AudioStarted += (_, _) => StartPlayheadTracking();
        AudioService.AudioStopped += (_, _) => StopPlayheadTracking();

        Focusable = true;
    }

    public void StartPlayheadTracking()
    {
        // 防止重复启动
        StopPlayheadTracking();

        _trackerCts = new CancellationTokenSource();
        var token = _trackerCts.Token;

        Task.Run(async () =>
        {
            Console.WriteLine("[TrackView] Playhead tracker started.");

            while (!token.IsCancellationRequested)
            {
                try
                {
                    float currentBeat = MuekEngine.get_current_position_beat();

                    if (Math.Abs(_playHeadPosX - currentBeat) > 0.001)
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            _playHeadPosX = currentBeat;

                            InvalidateVisual();
                        }, DispatcherPriority.Render);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Tracker Error] {ex.Message}");
                }

                await Task.Delay(16, token);
            }

            Console.WriteLine("[TrackView] Playhead tracker stopped.");
        }, token);
    }

    public void StopPlayheadTracking()
    {
        Console.WriteLine("[TrackView] Playhead tracker stopped.");
        if (_trackerCts != null)
        {
            _trackerCts.Cancel();
            _trackerCts.Dispose();
            _trackerCts = null;
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        StopPlayheadTracking();
    }

    private void UiStateServiceOnGlobalPlayHeadPosXUpdated(object? sender, double e)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() => { PlayHeadPosX = e; });
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        _mousePosition = e.GetPosition(this);
    
        if (e.Data.Contains(DataFormats.Files)) 
        {
            e.DragEffects = DragDropEffects.Copy;
            e.Handled = true; 
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }

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
    
    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
        {
            #pragma warning disable CS0618
            var files = e.Data.GetFileNames()?.ToList();
            #pragma warning restore CS0618
            if (files == null) return;
            Console.WriteLine($"[TrackView] Dropped: {files.Count}");

            var (x, y) = e.GetPosition(this);

            // var beat = (int)Math.Floor(x / ScaleFactor);
            x = e.GetPosition(this).X + OffsetX;

            var beat = x / ScaleFactor;
            beat = double.Round(beat * Subdivisions, 0) / Subdivisions;

            var absoluteY = y + OffsetY;
            var trackIndex = (int)Math.Floor(absoluteY / TrackHeight);

            //如果在最后一行，则创建新轨道
            if (trackIndex == DataStateService.Tracks.Count)
            {
                new MainWindowViewModel().AddTrack();
                // OffsetY = MaxOffsetY;
            }


            Console.WriteLine($"Dropped at beat: {beat}, track: {trackIndex}");

            foreach (var file in files)
            {
                if (!Path.Exists(file)) continue;
                var ext = Path.GetExtension(file).ToLower();
                var isMidi = false;
                if (ext != ".wav" && ext != ".mp3" && ext != ".ogg" && ext != ".mid")
                    continue;
                if(ext == ".mid")
                    isMidi = true;

                var durationSec = GetAudioDurationInSeconds(file);
                var durationBeats = (durationSec / 60f) * DataStateService.Bpm / Subdivisions;

                List<PianoRoll.Note>[]? notes = null;

                Clip newClip;
                if(!isMidi)
                    newClip = new Clip
                    {
                        Name = Path.GetFileNameWithoutExtension(file),
                        StartBeat = beat,
                        Duration = durationBeats,
                        Path = file,
                        Id = Guid.NewGuid().ToString(),
                        // CachedWaveform = AudioService.DecodeFromFile(file, 48000, 2).ToArray()
                    };
                else
                {
                    notes = [[],[],[],[],[],[],[],[],[],[],[],[],[],[],[],[]];
                    var midi = new MidiService();
                    midi.ImportMidi(file);
                    for (int i = 1; i < midi.Data.Tracks; i++)
                    {
                        for (int j = 0; j < midi.Data[i].Count; j++)
                        {
                            var note = midi.Data[i][j];
                            if (note.GetType() == typeof(NoteOnEvent))
                            {
                                try
                                {
                                    notes[i-1].Add(new PianoRoll.Note()
                                    {
                                        Name = ((NoteOnEvent)note).NoteNumber,
                                        // Color = NoteColor3,
                                        StartTime = ((NoteOnEvent)note).AbsoluteTime /
                                            (double)midi.Data.DeltaTicksPerQuarterNote * 4,
                                        EndTime = (((NoteOnEvent)note).AbsoluteTime + ((NoteOnEvent)note).NoteLength) /
                                            (double)midi.Data.DeltaTicksPerQuarterNote * 4,
                                        Velocity = ((NoteOnEvent)note).Velocity
                                    });
                                }
                                catch (Exception @exception)
                                {
                                    Console.Error.WriteLine(@exception.Message);
                                }
                            }
                        }
                    }
                    double trackEnd = 0;
                    foreach (var noteList in notes)
                    {
                        foreach (var note in noteList)
                            trackEnd = double.Max(trackEnd, note.EndTime);
                    }
                    trackEnd /= Subdivisions;
                    trackEnd /= DataStateService.Midi2TrackFactor;
                    newClip = new Clip
                    {
                        Name = Path.GetFileNameWithoutExtension(file),
                        StartBeat = beat,
                        Duration = trackEnd,
                        // Path = file,
                        Id = Guid.NewGuid().ToString(),
                    };
                    Console.WriteLine($"Add Midi Clip: Duration: {trackEnd}");
                }

                // 确保轨道存在
                if (trackIndex >= 0 && trackIndex < DataStateService.Tracks.Count)
                {
                    DataStateService.Tracks[trackIndex].AddClip(newClip,notes);
                }

                // var track = DataStateService.Tracks[trackIndex].Proto;
                // HandleNewClipCommand.Execute(track, newClip);
                // OffsetY = MaxOffsetY;
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
        var brushWhite = new SolidColorBrush(Colors.White,0.2);
        var brushGray = new SolidColorBrush(Colors.White, 0.15);
        var penWhite = new Pen(brushWhite,.4);
        var penGray = new Pen(brushGray,.2);
        var clipBorderThickness = 1.5;
        var highlightPen = new Pen(new SolidColorBrush(Colors.White,0.8),clipBorderThickness);
        var loopBrush = new SolidColorBrush(Colors.Black, 0.4);
        var loopPen = new Pen(loopBrush, 1.5, DashStyle.Dash);

        if (Background != null)
            context.FillRectangle(Background, new Rect(renderSize));

        //// 绘制交替色的背景
        var beatStart = (int)(OffsetX / ScaleFactor);
        var beatEnd = (int)((OffsetX + renderSize.Width) / ScaleFactor) + 1;

        for (var beat = beatStart; beat < beatEnd; beat++)
        {
            var groupIndex = beat / Subdivisions % 2; // 0 or 1，看拍数是奇数还是偶数了

            var color = groupIndex == 0 ? Color.Parse("#303030") : Color.Parse("#2C2C2C");
            var brush = new SolidColorBrush(color);

            var x1 = beat * ScaleFactor - OffsetX;
            var x2 = (beat + 1) * ScaleFactor - OffsetX;

            // 不绘制边界外的
            if (x2 < 0 || x1 > renderSize.Width)
                continue;

            context.FillRectangle(brush, new Rect(x1, 0, x2 - x1, renderSize.Height));
        }

        // 将画笔缓存到循环外，减少GC压力
        var waveformPen = new Pen(Brushes.Black, 0.8);
        var rectBorderPen = new Pen(Brushes.Black,0.2);

        //// 绘制片段
        for (var i = 0; i < DataStateService.Tracks.Count; i++)
        {
            var track = DataStateService.Tracks[i];

            // 每一轨的中间Y轴和高度
            var trackY = i * TrackHeight - OffsetY;
            if (trackY + TrackHeight < 0 || trackY > renderSize.Height)
                continue;

            // 绘制track的横向分割线
            var gapLineY = (i + 1) * TrackHeight - OffsetY;
            context.DrawLine(penGray, new Point(0, gapLineY), new Point(renderSize.Width, gapLineY));

            var centerY = trackY + TrackHeight / 2.0;
            var scaleY = TrackHeight / 2.0 * 0.95;

            _ = Color.TryParse(track.Color, out var color) ? color : DataStateService.MuekColor;
            var background = new SolidColorBrush(color);

            foreach (var clip in track.Clips.ToList())
            {
                var x = clip.StartBeat * ScaleFactor - OffsetX;
                var width = clip.Duration * ScaleFactor;

                // 跳过可视区域外的片段（横向）
                if (x + width < 0 || x > renderSize.Width)
                    continue;

                // 片段背景框
                var rect = new Rect(x, trackY, width, TrackHeight);
                var borderRect = new Rect(x + clipBorderThickness, trackY + clipBorderThickness,
                    width - clipBorderThickness * 2, TrackHeight - clipBorderThickness * 2);
                // context.FillRectangle(background, rect);
                context.DrawRectangle(background,rectBorderPen, rect);
                if(clip == _activeClip)
                    context.DrawRectangle(background,highlightPen, borderRect);
                
                if (clip.Notes != null)
                {
                    try
                    {
                        var notes = clip.Notes;
                        double noteHeight;
                        double noteWidth;
                        int noteMax = notes[0].Name;
                        int noteMin = noteMax;

                        foreach (var note in notes)
                        {
                            var position = note.Name;
                            noteMin = Math.Min(position, noteMin);
                            noteMax = Math.Max(position, noteMax);
                        }

                        noteHeight = (double)TrackHeight / (noteMax + 1 - noteMin);
                        noteWidth = _scaleFactor * 4;

                        var clipNotes = new List<PianoRoll.Note>();
                        var clipStart = x + OffsetX;
                        var loops = width / _scaleFactor / clip.SourceDuration;
                        Console.WriteLine($"Loops: {loops}");
                        for(int loop = 0; loop < loops; loop++)
                        {
                            foreach (var note in notes)
                            {
                                var noteStart = loop * clip.SourceDuration * _scaleFactor +
                                                (clip.StartBeat - clip.Offset) * _scaleFactor +
                                                note.StartTime * noteWidth / DataStateService.Midi2TrackFactor;
                                var noteLength = noteWidth * (note.EndTime - note.StartTime) / DataStateService.Midi2TrackFactor;
                                if (noteStart < clipStart && noteStart + noteLength > clipStart)
                                {
                                    noteLength = noteStart + noteLength - clipStart;
                                    noteStart = clipStart;
                                }

                                if (noteStart + noteLength > clipStart + width && noteStart < clipStart + width)
                                    noteLength = clipStart + width - noteStart;
                                if (noteStart >= clipStart && noteStart + noteLength <= clipStart + width)
                                    clipNotes.Add(note with
                                    {
                                        StartTime = noteStart, EndTime = noteStart + noteLength
                                    });
                            }
                            if(loop!=0)
                            {
                                context.DrawLine(loopPen,
                                    new Point(loop * clip.SourceDuration * _scaleFactor + x+1,
                                        TrackHeight * i + OffsetY),
                                    new Point(loop * clip.SourceDuration * _scaleFactor + x+1,
                                        TrackHeight * (i + 1) + OffsetY));
                            }
                        }

                        foreach (var note in clipNotes)
                        {
                            context.FillRectangle(Brushes.White,
                                new Rect(
                                    note.StartTime - OffsetX,
                                    TrackHeight * .6 - (note.Name - noteMin + 1) * noteHeight * .6 + TrackHeight * .2 +
                                    TrackHeight * i - OffsetY,
                                    note.EndTime - note.StartTime,
                                    noteHeight * .55));
                        }
                    }
                    catch (Exception ex)
                    {
                        track.Clips.Remove(clip);
                        new DialogWindow().ShowError(ex.Message);
                        continue;
                    }
                }

                // 渲染波形
                else if (clip.CachedWaveform is { Length: > 1 })
                {
                    var waveform = clip.CachedWaveform;
                    var totalSamples = waveform.Length;

                    // 1. 计算【密度】：每个像素对应多少个采样点
                    // 逻辑：源文件总采样数 / (源文件总时长(拍) * 缩放比例)
                    // 这样计算出来的密度是恒定的，不会因为 Clip 被裁剪而变化
                    double totalProjectedPixelWidth = clip.SourceDuration * ScaleFactor;

                    // 防止除零
                    if (totalProjectedPixelWidth <= 0.1) totalProjectedPixelWidth = 1;

                    var samplesPerPixel = (double)totalSamples / totalProjectedPixelWidth;

                    // 2. 计算【偏移】：由于 clip.Offset (头部裁剪)，我们需要跳过多少采样点
                    var offsetRatio = (double)clip.Offset / clip.SourceDuration;
                    var startOffsetSample = (long)(offsetRatio * totalSamples);

                    var pixelWidthInt = (int)Math.Ceiling(width);
                    if (pixelWidthInt <= 0) continue;

                    var geometry = new StreamGeometry();
                    using (var ctx = geometry.Open())
                    {
                        // 精细折线
                        if (samplesPerPixel <= 50)
                        {
                            // 高分辨率精细一点，用折线图（此乃盗窃reaper之秘术
                            var waveformCount = waveform.Length;

                            var visibleStartX = Math.Max(0, -x); // 视口左边相对波形起点的偏移，单位像素
                            var visibleEndX = Math.Min(width, renderSize.Width - x);

                            var visibleStartSample = startOffsetSample + (long)(visibleStartX * samplesPerPixel);
                            var visibleEndSample = startOffsetSample + (long)(visibleEndX * samplesPerPixel);

                            // 边界保护
                            visibleStartSample = Math.Clamp(visibleStartSample, 0, totalSamples - 1);
                            visibleEndSample = Math.Clamp(visibleEndSample, 0, totalSamples - 1);

                            var visibleSampleCount = visibleEndSample - visibleStartSample + 1;

                            if (visibleSampleCount > 1)
                            {
                                ctx.BeginFigure(
                                    new Point(x + visibleStartX, // 起始点 X 近似对齐
                                        centerY - waveform[visibleStartSample] * scaleY), false);

                                for (var s = visibleStartSample + 1; s <= visibleEndSample; s++)
                                {
                                    var relativePixelX = (s - startOffsetSample) / samplesPerPixel;
                                    var px = x + relativePixelX;
                                    var py = centerY - waveform[s] * scaleY;
                                    ctx.LineTo(new Point(px, py));
                                }
                            }
                        }
                        else
                        {
                            // Min-Max 大几把修剪树

                            // 计算视口裁剪
                            var renderStartPx = Math.Max(0, (int)Math.Floor(-x));
                            var renderEndPx = Math.Min(pixelWidthInt, (int)Math.Ceiling(renderSize.Width - x));

                            if (renderStartPx < renderEndPx)
                            {
                                for (var px = renderStartPx; px < renderEndPx; px++)
                                {
                                    var sStart = (long)(px * samplesPerPixel) + startOffsetSample;
                                    var sEnd = (long)((px + 1) * samplesPerPixel) + startOffsetSample;

                                    // 边界保护
                                    sStart = Math.Clamp(sStart, 0, totalSamples - 1);
                                    sEnd = Math.Clamp(sEnd, sStart + 1, totalSamples);

                                    // 寻找 Min/Max
                                    float min = 0, max = 0;
                                    bool firstSample = true;

                                    // 循环寻找极值
                                    for (var j = sStart; j < sEnd; j++)
                                    {
                                        var val = waveform[(int)j];
                                        if (firstSample)
                                        {
                                            min = val;
                                            max = val;
                                            firstSample = false;
                                        }
                                        else
                                        {
                                            if (val > max) max = val;
                                            else if (val < min) min = val;
                                        }
                                    }

                                    if (firstSample && sStart < totalSamples)
                                    {
                                        min = max = waveform[(int)sStart];
                                    }

                                    var drawX = x + px;
                                    var y1 = centerY - max * scaleY;
                                    var y2 = centerY - min * scaleY;

                                    if (Math.Abs(y1 - y2) < 0.1) y2 += 0.1;

                                    ctx.BeginFigure(new Point(drawX, y1), false);
                                    ctx.LineTo(new Point(drawX, y2));
                                }
                            }
                        }
                    }

                    // 绘制最终几何图形
                    // TODO：freeze geometry 或缓存它
                    context.DrawGeometry(null, waveformPen, geometry);
                }

                // 绘制文字
                context.DrawText(
                    new FormattedText(clip.Name ?? "New Clip", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                        Typeface.Default, 10, Brushes.Black),
                    new Point(x, trackY));
            }
        }

        //// 绘制Dropping的临时对象
        if (_isDropping)
        {
            var x = _mousePosition.X;
            x = double.Round(x * Subdivisions / _scaleFactor, 0) / Subdivisions * ScaleFactor -
                OffsetX % (ScaleFactor / (double)Subdivisions);

            var documentY = _mousePosition.Y + OffsetY;
            var index = (int)Math.Floor(documentY / TrackHeight);
            var drawY = index * TrackHeight - OffsetY;

            if ((DataStateService.Tracks.Count) >= index)
            {
                var rect = new Rect(x, drawY, 100, TrackHeight);
                var background = new LinearGradientBrush();
                background.StartPoint = new RelativePoint(0.0, 0.5, RelativeUnit.Relative);
                background.EndPoint = new RelativePoint(1.0, 0.5, RelativeUnit.Relative);
                if (DataStateService.Tracks.Count != index)
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
        PlayHeadPosX = double.Round(PlayHeadPosX * Subdivisions, 0) / Subdivisions;
        Console.WriteLine($"PlayHeadPosX: {PlayHeadPosX}");
        UiStateService.GlobalPlayHeadPosX = PlayHeadPosX;

        // TODO: 同步节拍
        var currentBeat = PlayHeadPosX / ScaleFactor;
        AudioService.PlayPosition = (float)currentBeat * _scaleFactor;
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
            UpdateTrackSelect(point);

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

            if (e.ClickCount == 2 && _activeClip is { LinkedPattern: not null })
            {
                var pattern = _activeClip.LinkedPattern;
                pattern.SelectPattern();
                DataStateService.PianoRollWindow.Show();
            }

            e.Handled = true; // 避免冒泡
        }
        else if (props.IsRightButtonPressed)
        {
            if(_activeClip is null)
                return;
            var state = GetClipInteractionMode();
            if (state == ClipInteractionMode.OnTopTitle)
            {
                var menu = new MenuFlyout()
                {
                    Items =
                    {
                        new MenuItem()
                        {
                            Header = "Rename",
                            //TODO
                        },
                        new MenuItem()
                        {
                            Header = "Remove",
                            Command = RemoveClipCommand
                        }
                    }
                };
                menu.ShowAt(this,true);
            }
            else
            {
                RemoveClip();
            }
        }
    }
    
    [RelayCommand]
    private void RemoveClip()
    {
        foreach (var track in DataStateService.Tracks)
        {
            foreach (var clip in track.Clips.ToList())
            {
                if (clip != _activeClip) continue;
                track.Clips.Remove(clip);
                ActiveClip = null;
                InvalidateVisual();
            }
        }
    }

    private void UpdateTrackSelect(Point point)
    {
        var trackIndex = (int)Math.Floor((point.Y + OffsetY) / TrackHeight);

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

        var trackIndex = (int)Math.Floor((_mousePosition.Y + OffsetY) / TrackHeight);
        var relativeMouseY = ((_mousePosition.Y + OffsetY) % TrackHeight) / TrackHeight; // 0~1

        if (trackIndex < 0 || trackIndex >= DataStateService.Tracks.Count)
        {
            Cursor = new Cursor(StandardCursorType.Ibeam);
            ActiveClip = null;
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
                ActiveClip = clip;
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
        ActiveClip = null;
        return ClipInteractionMode.None;
    }


    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        _isSnapping = e.KeyModifiers != KeyModifiers.Alt;
        
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
            if(_isSnapping)
                newStart = double.Round(newStart * Subdivisions, 0) / Subdivisions;
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
            if (newDuration < 0.1 || newDuration > _activeClip.SourceDuration && _activeClip.Notes == null)
                return;
            _activeClip.Proto.Duration = newDuration;
            
            if (_isSnapping)
                _activeClip.Proto.Duration = double.Round(newDuration * Subdivisions, 0) / Subdivisions;
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
        if (_isSnapping)
            pointerBeat = double.Round(pointerBeat * Subdivisions, 0) / Subdivisions;
        pointerBeat = pointerBeat < 0 ? 0 : pointerBeat;
        Console.WriteLine(pointerBeat);
        if (pointerBeat >= 0)
            _activeClip.Proto.StartBeat = pointerBeat;

        InvalidateVisual();
    }

    private void CheckIfPointerInClipBounds(Point point)
    {
        var state = GetClipInteractionMode();
        Cursor = state switch
        {
            ClipInteractionMode.None => new Cursor(StandardCursorType.Ibeam),
            ClipInteractionMode.OnTopTitle => new Cursor(StandardCursorType.SizeAll),
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

        var parent = this.GetVisualAncestors().OfType<MainWindow>().FirstOrDefault();

        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            OffsetX -= e.Delta.Y * 44;
            OffsetX = Math.Max(0, OffsetX);
            InvalidateVisual();
            e.Handled = true;

            UiStateService.GlobalTimelineScale = ScaleFactor;
            UiStateService.GlobalTimelineOffsetX = OffsetX;
            parent?.SyncTimeline(this);
            return;
        }

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            var delta = e.Delta.Y;
            if (delta != 0)
            {
                var factor = delta > 0 ? 1.1 : 0.9;
                var pointerX = e.GetPosition(this).X;
                ZoomAt(pointerX, factor);

                UiStateService.GlobalTimelineScale = ScaleFactor;
                UiStateService.GlobalTimelineOffsetX = OffsetX;
                parent?.SyncTimeline(this);
            }

            return;
        }

        OffsetY -= e.Delta.Y * 44;
        e.Handled = true;
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);

        // 触发setter
        var a = OffsetY;
        OffsetY = 0;
        OffsetY = a;
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