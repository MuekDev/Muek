using System;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

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
    
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsVisibleProperty && IsVisible)
        {
            SetValue(OpacityProperty,0);
            Console.WriteLine($"IsVisibleChanged:{Opacity}");
            new Animation
            {
                Duration = TimeSpan.FromMilliseconds(500),
                FillMode = FillMode.Forward,
                Easing = Easing.Parse("CubicEaseOut"),
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(1),
                        Setters =
                        {
                            new Setter
                            {
                                Property = OpacityProperty,
                                Value = 1.0
                            }
                        }
                    }
                }
            }.RunAsync(this);
            
        }
    }
}