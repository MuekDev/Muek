using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Muek.Models;
using Muek.Services;

namespace Muek.Views;

public partial class TrackView : UserControl
{
    private bool _isDraggingPlayhead;
    private double _offsetX;
    private double _playHeadPosX;
    private int _scaleFactor = 100;
    private double _timeRulerPosX;
    private bool _isDropping =false;
    private Point _mousePosition =new();
    public int TrackHeight = 100;

    public TrackView()
    {
        InitializeComponent();
        Focusable = true;
        AddHandler(DragDrop.DropEvent, OnDrop);
        AddHandler(DragDrop.DragEnterEvent, OnDropEnter);
        AddHandler(DragDrop.DragLeaveEvent, OnDropLeave);
        AddHandler(DragDrop.DragOverEvent,OnDragOver);
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
            if (_scaleFactor == value) return;
            if (value < 10)
            {
                _scaleFactor = 10;
                return;
            }

            _scaleFactor = value;
            InvalidateVisual(); // 重新调用 Render
        }
    }

    public int Subdivisions { get; set; } = 4;


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

            var trackIndex = (int)Math.Floor(y / TrackHeight);

            Console.WriteLine($"Dropped at beat: {beat}, track: {trackIndex}");

            foreach (var file in files)
            {
                if (!Path.Exists(file)) continue;

                var ext = Path.GetExtension(file).ToLower();
                if (ext != ".wav" && ext != ".mp3" && ext != ".ogg")
                    continue;

                var newClip = new Clip
                {
                    Name = Path.GetFileNameWithoutExtension(file),
                    StartBeat = beat,
                    Duration = 4
                };

                // 确保轨道存在
                if (trackIndex >= 0 && trackIndex < DataStateService.Tracks.Count)
                {
                    DataStateService.Tracks[trackIndex].Clips.Add(newClip);
                }
            }

            _isDropping = false;
            InvalidateVisual();
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
                var background = new SolidColorBrush(track.Color);

                context.FillRectangle(background, rect);
                context.DrawRectangle(new Pen(Brushes.Black), rect);
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
            var y = Math.Floor(_mousePosition.Y / TrackHeight) * TrackHeight;
            var rect = new Rect(x, y, 100, TrackHeight);
            var background = new LinearGradientBrush();
            background.StartPoint = new RelativePoint(0.0, 0.5, RelativeUnit.Relative);
            background.EndPoint = new RelativePoint(1.0, 0.5, RelativeUnit.Relative);
            background.GradientStops.Add(new GradientStop(Colors.YellowGreen, 0.0));
            background.GradientStops.Add(new GradientStop(Colors.Transparent, 1.0));

            context.FillRectangle(background, rect);
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

            var label = (PlayHeadPosX + 1).ToString("0.0");
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
            _isDraggingPlayhead = true;
            UpdatePlayHeadFromPointer(point);
            e.Handled = true; // 避免冒泡
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (_isDraggingPlayhead && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var point = e.GetPosition(this);
            UpdatePlayHeadFromPointer(point);
            e.Handled = true;
        }

        _mousePosition = e.GetPosition(this);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isDraggingPlayhead = false;
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
        var newScale = (int)Math.Clamp(oldScale * zoomFactor, 10, 1000);

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