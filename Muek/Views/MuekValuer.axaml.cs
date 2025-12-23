using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;

namespace Muek.Views;

public partial class MuekValuer : UserControl
{
    [SupportedOSPlatform("windows")]
    [DllImport("user32.dll", EntryPoint = "SetCursorPos")]
    private static extern bool SetCursorPos(int x, int y);
    
    public static readonly StyledProperty<double> MinValueProperty = AvaloniaProperty.Register<MuekValuer, double>(
        nameof(MinValue));

    public double MinValue
    {
        get => GetValue(MinValueProperty);
        set => SetValue(MinValueProperty, value);
    }

    public static readonly StyledProperty<double> MaxValueProperty = AvaloniaProperty.Register<MuekValuer, double>(
        nameof(MaxValue));

    public double MaxValue
    {
        get => GetValue(MaxValueProperty);
        set => SetValue(MaxValueProperty, value);
    }

    public static readonly StyledProperty<IBrush> ValuerColorProperty = AvaloniaProperty.Register<MuekValuer, IBrush>(
        nameof(ValuerColor));

    public IBrush ValuerColor
    {
        get => GetValue(ValuerColorProperty);
        set
        {
            SetValue(ValuerColorProperty, value);
            InvalidateVisual();
        }
    }

    public static readonly StyledProperty<double> SizeProperty = AvaloniaProperty.Register<MuekValuer, double>(
        nameof(ValuerHeight));

    public double ValuerHeight
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public static readonly StyledProperty<double> ValuerWidthProperty = AvaloniaProperty.Register<MuekValuer, double>(
        nameof(ValuerWidth));

    public double ValuerWidth
    {
        get => GetValue(ValuerWidthProperty);
        set => SetValue(ValuerWidthProperty, value);
    }


    public static readonly StyledProperty<double> SpeedProperty = AvaloniaProperty.Register<MuekValuer, double>(
        nameof(Speed));

    public double Speed
    {
        get => GetValue(SpeedProperty);
        set => SetValue(SpeedProperty, value);
    }

    public static readonly StyledProperty<double> DefaultValueProperty = AvaloniaProperty.Register<MuekValuer, double>(
        nameof(DefaultValue));

    public double DefaultValue
    {
        get => GetValue(DefaultValueProperty);
        set => SetValue(DefaultValueProperty, value);
    }

    public enum LayoutEnum
    {
        Slider,
        Knob
    }

    public static readonly StyledProperty<LayoutEnum> LayoutProperty =
        AvaloniaProperty.Register<MuekValuer, LayoutEnum>(
            nameof(Layout));

    public LayoutEnum Layout
    {
        get => GetValue(LayoutProperty);
        set => SetValue(LayoutProperty, value);
    }

    //TODO 普普通通的Value值，等待修改
    public double Value;

    private bool _hover;
    private bool _pressed;

    private Pen _stroke = new Pen();

    public MuekValuer()
    {
        _hover = false;

        // DefaultValue = 50;
        // MaxValue = 100;
        // MinValue = 0;
        // Value = DefaultValue;
        // ValuerHeight = 200;
        // ValuerWidth = 20;
        Layout = LayoutEnum.Knob;
        Speed = .005;
        ValuerColor = Brushes.White;
        InitializeComponent();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Value = DefaultValue;
        ClipToBounds = false;

        StrokeThicknessDecrease(_stroke);
    }

    private readonly Brush _whiteBrush = new SolidColorBrush(Colors.White, .2);
    private readonly Brush _transparentBrush = new SolidColorBrush(Colors.Black, 0);
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        ValuerHeight = Bounds.Height;
        ValuerWidth = Bounds.Width;


        _stroke.Brush = ValuerColor;

        //Slider渲染逻辑

        
        if (Layout == LayoutEnum.Slider)
        {
            if (_hover || _pressed)
            {
                StrokeThicknessIncrease(_stroke);
                // context.DrawRectangle(ValuerColor, null, new Rect(1, 1, ValuerWidth, ValuerHeight));
            }
            else
            {
                StrokeThicknessDecrease(_stroke);
            }

            context.DrawRectangle(_transparentBrush, _stroke, new Rect(0, 0, ValuerWidth, ValuerHeight));
            // context.DrawRectangle(Brush.Parse("#CC000000"), _stroke, new Rect(0, 0, ValuerWidth, ValuerHeight));
            var percentValue = (Value - MinValue) / (MaxValue - MinValue);
            // context.DrawRectangle(Brushes.Black,null, new Rect(0,(1-percentValue)*(ValuerHeight-6)+2, ValuerWidth, 4));
            
            var defaultPercentValue = (DefaultValue - MinValue) / (MaxValue - MinValue);
            
            context.DrawRectangle(ValuerColor, _stroke,
                new Rect(-ValuerWidth/2, (1 - percentValue) * (ValuerHeight-1), ValuerWidth * 2, 1));

            if(_hover || _pressed)
            {
                //默认值
                context.DrawEllipse(_whiteBrush, null,
                    new Point(-10, (1 - defaultPercentValue) * (ValuerHeight) + 1), 2, 2);
            }
        }

        //Knob渲染逻辑
        if (Layout == LayoutEnum.Knob)
        {
            var radius = (ValuerHeight < ValuerWidth ? ValuerHeight / 2 : ValuerWidth / 2)*1.5;
            var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
            if (_hover || _pressed)
            {
                StrokeThicknessIncrease(_stroke);
                // context.DrawEllipse(ValuerColor, null, new Point(ValuerHeight, ValuerHeight), ValuerHeight, ValuerHeight);
            }
            else
            {
                StrokeThicknessDecrease(_stroke);
            }
            context.DrawEllipse(_transparentBrush, _stroke, center, radius,
                radius);
            // context.DrawEllipse(Brush.Parse("#CC000000"), _stroke, center,
            //     radius-.05, radius-.05);

            //左右边缘的值
            var border = .16;
            //百分比
            var percentValue = (Value - MinValue) / (MaxValue - MinValue);
            percentValue = border +  percentValue * (1 - 2 * border);
            
            // context.DrawEllipse(Brushes.Black, null,
            //     new Point(ValuerHeight + ValuerHeight * .8 * -double.Sin(percentValue * Double.Pi * 2),
            //         ValuerHeight + ValuerHeight * .8 * double.Cos(percentValue * Double.Pi * 2)), ValuerHeight * .2, ValuerHeight * .2);

            var defaultPercentValue = (DefaultValue - MinValue) / (MaxValue - MinValue);
            defaultPercentValue = border + defaultPercentValue * (1 - 2 * border);
            
            if(_hover || _pressed)
            {
                //默认值
                context.DrawEllipse(_whiteBrush, null,
                    new Point(center.X + radius * 1.2 * -double.Sin(defaultPercentValue * Double.Pi * 2),
                        center.Y + radius * 1.2 * double.Cos(defaultPercentValue * Double.Pi * 2)), 2,
                    2);

                //边缘值
                context.DrawEllipse(_whiteBrush, null,
                    new Point(center.X + radius * 1.2 * -double.Sin(border * Double.Pi * 2),
                        center.Y + radius * 1.2 * double.Cos(border * Double.Pi * 2)), 2,
                    2);
                context.DrawEllipse(_whiteBrush, null,
                    new Point(center.X + radius * 1.2 * -double.Sin((1 - border) * Double.Pi * 2),
                        center.Y + radius * 1.2 * double.Cos((1 - border) * Double.Pi * 2)), 2,
                    2);
            }
            
            //通过三角函数渲染位置
            context.DrawLine(new Pen(ValuerColor, 2),
                new Point(
                    center.X + radius * .7 *
                    -double.Sin(percentValue * Double.Pi * 2),
                    center.Y + radius * .7 *
                    double.Cos(percentValue * Double.Pi * 2)),
                new Point(
                    center.X + radius *
                    -double.Sin(percentValue * Double.Pi * 2),
                    center.Y + radius *
                    double.Cos(percentValue * Double.Pi * 2)));
        }

        Console.WriteLine($"pressed: {_pressed}\nhover: {_hover}");
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        _hover = true;
        InvalidateVisual();
    }
    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _hover = false;
        InvalidateVisual();
    }

    private Point _tempPress;
    
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _pressed = true;
            _tempPress = e.GetPosition(this);
            // Console.WriteLine("Pressed");
            if (e.KeyModifiers == KeyModifiers.Alt)
            {
                Value = DefaultValue;
                InvalidateVisual();
            }
            else
            {
                if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    Cursor = new Cursor(StandardCursorType.None);
                switch (Layout)
                {
                    case LayoutEnum.Knob:
                        if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                            SetCursorPos(this.PointToScreen(new Point(Bounds.Width/2, Bounds.Height/2)).X,
                                this.PointToScreen(new Point(Bounds.Width/2, Bounds.Height/2)).Y);
                        _tempPress = new Point(Bounds.Width/2, Bounds.Height/2);
                        break;
                    case LayoutEnum.Slider:
                    {
                        var percentValue = (Value - MinValue) / (MaxValue - MinValue);
                        if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                            SetCursorPos(this.PointToScreen(new Point(0, (1 - percentValue) * (ValuerHeight - 1))).X,
                                this.PointToScreen(new Point(0, (1 - percentValue) * (ValuerHeight - 1))).Y);
                        break;
                    }
                }
            }
        }

        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            //TODO
            Console.WriteLine("Right MouseButton Clicked");
        }

        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _pressed = false;
        Cursor = Cursor.Default;
        e.Handled = true;
        if(e.KeyModifiers == KeyModifiers.Alt) return;
        switch (Layout)
        {
            case LayoutEnum.Knob:
                if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    SetCursorPos(this.PointToScreen(new Point(Bounds.Width/2, Bounds.Height/2)).X,
                        this.PointToScreen(new Point(Bounds.Width/2, Bounds.Height/2)).Y);
                break;
            case LayoutEnum.Slider:
            {
                var percentValue = (Value - MinValue) / (MaxValue - MinValue);
                if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    SetCursorPos(this.PointToScreen(new Point(0, (1 - percentValue) * (ValuerHeight - 1))).X,
                        this.PointToScreen(new Point(0, (1 - percentValue) * (ValuerHeight - 1))).Y);
                break;
            }
        }

        InvalidateVisual();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_pressed)
        {
            Cursor = new Cursor(StandardCursorType.None);
            var pos = e.GetPosition(this);

            switch (Layout)
            {
                case LayoutEnum.Knob:
                {
                    var deltaY = pos.Y - _tempPress.Y;
                    Value -= deltaY * Speed * (MaxValue - MinValue);
                    _tempPress = pos;
                    break;
                }
                case LayoutEnum.Slider:
                {
                    var ratio = 1.0 - (pos.Y / Bounds.Height);
                    Value = MinValue + ratio * (MaxValue - MinValue);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            Value = double.Clamp(Value, MinValue, MaxValue);
        }
        InvalidateVisual();
    }

    // private async Task StrokeThicknessIncrease(Pen stroke)
    // {
    //     while (stroke.Thickness < 1)
    //     {
    //         await Task.Delay(5);
    //         lock (stroke) stroke.Thickness += .1;
    //         // Console.WriteLine("StrokeThicknessIncrease");
    //     }
    // }

    private void StrokeThicknessIncrease(Pen stroke)
    {
        stroke.Thickness = 1.5;
    }

    // private async Task StrokeThicknessDecrease(Pen stroke)
    // {
    //     while (stroke.Thickness > .5)
    //     {
    //         await Task.Delay(5);
    //         lock (stroke) stroke.Thickness -= .1;
    //         // Console.WriteLine("StrokeThicknessDecrease");
    //     }
    // }

    private void StrokeThicknessDecrease(Pen stroke)
    {
        stroke.Thickness = 1;
    }
}