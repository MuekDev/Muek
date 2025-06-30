using System;
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
    private int _scaleFactor = 100;

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

            UiStateService.GlobalTimeLineScale = value;
            var parent = this.GetVisualAncestors().OfType<MainWindow>().FirstOrDefault();
            parent?.SyncTimeline(this);

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

        for (double x = 0; x < renderSize.Width; x += subStep)
        {
            var drawX = Math.Round(x);      // 防止抗锯齿导致线丢失
            var isMainLine = Math.Abs(drawX % step) < 0.1;

            if (!isMainLine && ScaleFactor < 33)
            {
                continue;
            }

            var pen = isMainLine ? penWhite : penGray;

            context.DrawLine(pen, new Point(drawX,0 ), new Point(drawX, renderSize.Height));
        }


        base.Render(context);
    }


    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        var delta = e.Delta.Y;
        if (delta != 0)
        {
            var factor = delta > 0 ? 1.1 : 0.9; // 放大10%，缩小10%
            ScaleFactor = (int)Math.Clamp(ScaleFactor * factor, 1, 1000);
        }
    }
}