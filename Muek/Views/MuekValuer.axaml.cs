using System;
using System.Globalization;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Muek.Models;

namespace Muek.Views;

public partial class MuekValuer : UserControl
{
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
        // Layout = LayoutEnum.Slider;
        Speed = .005;
        ValuerColor = Brushes.White;
        InitializeComponent();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Value = DefaultValue;
        ClipToBounds = false;

        _ = StrokeThicknessDecrease(_stroke);
    }

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
                _ = StrokeThicknessIncrease(_stroke);
                // context.DrawRectangle(ValuerColor, null, new Rect(1, 1, ValuerWidth, ValuerHeight));
            }
            else
            {
                _ = StrokeThicknessDecrease(_stroke);
            }

            context.DrawRectangle(ValuerColor, _stroke, new Rect(1.5, 1.5, ValuerWidth - 1, ValuerHeight - 1));
            context.DrawRectangle(Brush.Parse("#CC000000"), _stroke, new Rect(2, 2, ValuerWidth - 2, ValuerHeight - 2));
            var percentValue = (Value - MinValue) / (MaxValue - MinValue);
            // context.DrawRectangle(Brushes.Black,null, new Rect(0,(1-percentValue)*(ValuerHeight-6)+2, ValuerWidth, 4));
            context.DrawRectangle(ValuerColor, _stroke,
                new Rect(1.4, (1 - percentValue) * (ValuerHeight - 6) + 2, ValuerWidth-1, 4));
        }

        //Knob渲染逻辑
        if (Layout == LayoutEnum.Knob)
        {
            ValuerHeight /= 2;
            ValuerWidth /= 2;
            if (_hover || _pressed)
            {
                _ = StrokeThicknessIncrease(_stroke);
                // context.DrawEllipse(ValuerColor, null, new Point(ValuerHeight, ValuerHeight), ValuerHeight, ValuerHeight);
            }
            else
            {
                _ = StrokeThicknessDecrease(_stroke);
            }

            context.DrawEllipse(ValuerColor, _stroke, new Point(ValuerHeight, ValuerHeight), ValuerHeight * .95,
                ValuerHeight * .95);
            context.DrawEllipse(Brush.Parse("#CC000000"), _stroke, new Point(ValuerHeight, ValuerHeight),
                ValuerHeight * .9, ValuerHeight * .9);
            var percentValue = (Value - MinValue) / (MaxValue - MinValue);
            // context.DrawEllipse(Brushes.Black, null,
            //     new Point(ValuerHeight + ValuerHeight * .8 * -double.Sin(percentValue * Double.Pi * 2),
            //         ValuerHeight + ValuerHeight * .8 * double.Cos(percentValue * Double.Pi * 2)), ValuerHeight * .2, ValuerHeight * .2);

            //通过三角函数渲染圆形控件
            context.DrawEllipse(ValuerColor, null,
                new Point(ValuerHeight + ValuerHeight * .9 * -double.Sin(percentValue * Double.Pi * 2),
                    ValuerHeight + ValuerHeight * .9 * double.Cos(percentValue * Double.Pi * 2)), ValuerHeight * .2,
                ValuerHeight * .2);
        }

        Console.WriteLine($"pressed: {_pressed}\nhover: {_hover}");
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        _hover = true;
        e.Handled = true;
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _hover = false;
        e.Handled = true;
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
            if (e.ClickCount == 2)
            {
                Value = DefaultValue;
                InvalidateVisual();
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
        e.Handled = true;
        InvalidateVisual();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (!_pressed)
            return;

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
        InvalidateVisual();
    }

    private async Task StrokeThicknessIncrease(Pen stroke)
    {
        while (stroke.Thickness < 1)
        {
            await Task.Delay(5);
            lock (stroke) stroke.Thickness += .1;
            // Console.WriteLine("StrokeThicknessIncrease");
        }
    }

    private async Task StrokeThicknessDecrease(Pen stroke)
    {
        while (stroke.Thickness > .5)
        {
            await Task.Delay(5);
            lock (stroke) stroke.Thickness -= .1;
            // Console.WriteLine("StrokeThicknessDecrease");
        }
    }
}