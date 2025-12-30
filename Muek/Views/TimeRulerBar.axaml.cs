using System;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Muek.Models;
using Muek.Services;

namespace Muek.Views;

public partial class TimeRulerBar : UserControl
{
    private double _offsetX;
    private int _scaleFactor = 100;
    private bool _isDragging = false;

    public TimeRulerBar()
    {
        InitializeComponent();
        Focusable = true;
    }

    public new IBrush? Background { get; set; } = Brushes.Transparent;

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
        var brushWhite = new SolidColorBrush(Colors.White);
        var brushGray = new SolidColorBrush(Colors.Gray);
        var penWhite = new Pen(brushWhite);
        var penGray = new Pen(brushGray);

        if (Background != null)
            context.FillRectangle(Background, new Rect(renderSize));

        var step = Math.Max(1, ScaleFactor);

        var subStep = (double)step / Subdivisions;

        var beats = 1;

        // TODO: `renderSize.Width + OffsetX` 可能会导致当OffsetX过大时溢出
        for (double x = 0; x < renderSize.Width + OffsetX; x += subStep)
        {
            var drawX = Math.Round(x - OffsetX); // 防止抗锯齿导致线丢失
            var isMainLine = Math.Abs(x % step) < 0.1;

            if (!isMainLine && ScaleFactor < 33) continue;

            var pen = isMainLine ? penWhite : penGray;
            double height = isMainLine ? 15 : 20;

            context.DrawLine(pen, new Point(drawX, height), new Point(drawX, 30));

            // 标签这一块
            if (isMainLine && ScaleFactor > 22)
            {
                context.DrawText(
                    // HACK: 粗略计算小节
                    new FormattedText(((beats - 1) * 4 + 1).ToString(), CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        Typeface.Default, 10, brushWhite)
                    , new Point(drawX + 4, height));
                beats++;
            }
        }

        base.Render(context);
    }


    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            OffsetX -= e.Delta.Y * 20;
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
            ScaleFactor = (int)Math.Clamp(ScaleFactor * factor, 1, 1000);
            UiStateService.GlobalTimelineScale = ScaleFactor;
            UiStateService.GlobalTimelineOffsetX = OffsetX;
            var parent = this.GetVisualAncestors().OfType<MainWindow>().FirstOrDefault();
            parent?.SyncTimeline(this);
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (e.GetPosition(this).Y > 0 && e.GetPosition(this).Y <= Bounds.Height)
            Cursor = new Cursor(StandardCursorType.Ibeam);
        if (_isDragging)
            ViewHelper.GetMainWindow().TrackViewControl.UpdatePlayHeadFromPointer(e.GetPosition(this));
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.GetPosition(this).Y > 0 && e.GetPosition(this).Y <= Bounds.Height)
            _isDragging = true;
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isDragging = false;
    }
}