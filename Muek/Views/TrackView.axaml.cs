using System;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using Muek.Services;

namespace Muek.Views;

public partial class TrackView : UserControl
{
    public TrackView()
    {
        InitializeComponent();
    }

    public new IBrush? Background { get; set; } = Brushes.Transparent;

    private bool _isDraggingPlayhead = false;
    private int _scaleFactor = 100;
    private double _offsetX = 0;
    private double _playHeadPosX = 0;
    private double _timeRulerPosX = 0;

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
            var groupIndex = (beat / Subdivisions) % 2; // 0 or 1，看拍数是奇数还是偶数了

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

                var rect = new Rect(x, i*100, width, 100);
                var background = new SolidColorBrush(track.Color);

                context.FillRectangle(background, rect);
                context.DrawRectangle(new Pen(Brushes.Black, 1), rect);
                context.DrawText(
                    new FormattedText(clip.Name, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                        Typeface.Default, 10, Brushes.Black),
                    new Point(x,i* 100));
            }
        }


        //// 绘制刻度网格
        var step = Math.Max(1, ScaleFactor);

        var subStep = (double)step / Subdivisions;

        for (double x = 0; x < renderSize.Width + OffsetX; x += subStep)
        {
            var drawX = Math.Round(x - OffsetX); // 防止抗锯齿导致线丢失
            var isMainLine = Math.Abs(x % step) < 0.1;

            if (!isMainLine && ScaleFactor < 33)
            {
                continue;
            }

            var pen = isMainLine ? penWhite : penGray;

            context.DrawLine(pen, new Point(drawX, 0), new Point(drawX, renderSize.Height));
        }


        //// 绘制播放线
        var playheadX = PlayHeadPosX * ScaleFactor - OffsetX;

        if (playheadX >= 0 && playheadX <= renderSize.Width)
        {
            var playheadPen = new Pen(Brushes.White, 1);

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
        var globalX = Math.Max(0,point.X + OffsetX);
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