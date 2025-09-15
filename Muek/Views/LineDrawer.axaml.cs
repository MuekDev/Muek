using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Muek.Views;

public partial class LineDrawer : UserControl
{
    private double _lineY;
    public double LineY
    {
        get => _lineY;
        set
        {
            if (Math.Abs(_lineY - value) > 0.001)
            {
                _lineY = value;
                InvalidateVisual();
            }
        }
    }

    public IBrush LineBrush { get; set; } = Brushes.Red;

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var pen = new Pen(LineBrush, 2);
        var width = Bounds.Width;

        // 在 LineY 位置绘制横线
        context.DrawLine(pen, new Point(0, LineY), new Point(width, LineY));
        
        
    }
}