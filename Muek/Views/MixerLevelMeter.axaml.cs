using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reactive.PlatformServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Muek.Services;
using Muek.ViewModels;
using SkiaSharp;

namespace Muek.Views;

public partial class MixerLevelMeter : UserControl
{
    private static readonly StyledProperty<TrackViewModel> TrackProperty =
        AvaloniaProperty.Register<MixerLevelMeter, TrackViewModel>(
            nameof(Track));

    public TrackViewModel Track
    {
        get => GetValue(TrackProperty);
        set => SetValue(TrackProperty, value);
    }

    private const float MinDb = -114.0f;
    private const float MaxDb = 6f;

    private const float CompressionFactor = 0.15f;

    private float[] CurrentRmsLevel => AudioService.CurrentRmsDb;
    private float[] CurrentPeakLevel => AudioService.CurrentPeakDb;

    public enum LevelMeterMode
    {
        PeakRms,
        Peak,
        Rms
    }

    private static readonly StyledProperty<LevelMeterMode> ModeProperty =
        AvaloniaProperty.Register<MixerLevelMeter, LevelMeterMode>(
            nameof(Mode));

    public LevelMeterMode Mode
    {
        get => GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    public MixerLevelMeter()
    {
        InitializeComponent();
        ClipToBounds = false;
        Mode = LevelMeterMode.PeakRms;
        AudioService.RmsDbChanged += AudioServiceOnRmsDbChanged;
        AudioService.PeakDbChanged += AudioServiceOnPeakDbChanged;
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
    }

    private void AudioServiceOnRmsDbChanged(object? sender, float[] f)
    {
        Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Render);
    }

    private void AudioServiceOnPeakDbChanged(object? sender, float[] f)
    {
        Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Render);
    }

    // private readonly Pen _orangePen = new Pen(new ValurColorBrush(Colors.Orange, .5));
    // private readonly Pen _redPen = new Pen(new ValurColorBrush(Colors.Red, .5));
    
    
    // private readonly FormattedText _warningText1 = new FormattedText("-3dB",
    //     CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 10,
    //     new ValurColorBrush(Colors.Orange, .5));
    //
    // private readonly FormattedText _warningText2 = new FormattedText("0dB",
    //     CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 10,
    //     new ValurColorBrush(Colors.Red, .5));

    private Rect _backgroundRect;

    private readonly Pen _whitePen = new Pen(new SolidColorBrush(Colors.White, .5));

    private List<Point[]> _gridList = new();

    private IBrush ValurColorBrush => Brush.Parse(Track.Color);
    private static readonly Color OrangeColor = Color.FromRgb(255,160,0);
    private static readonly Color RedColor = Color.FromRgb(255,50,0);

    private IBrush _orangeBrush = new SolidColorBrush(OrangeColor, .8);
    private IBrush _redBrush = new SolidColorBrush(RedColor, .8);
    private double _colorBrushOpacity = .5;
    private IBrush ColorBrush => new SolidColorBrush(Color.Parse(Track.Color), _colorBrushOpacity);

    private readonly IBrush _backgroundOrangeBrush = new SolidColorBrush(OrangeColor, 0.15);

    private readonly IBrush _backgroundRedBrush = new SolidColorBrush(RedColor, 0.15);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ModeProperty || change.Property == TrackProperty)
        {
            if (Mode == LevelMeterMode.PeakRms)
            {
                _orangeBrush = new SolidColorBrush(OrangeColor, .5);
                _redBrush = new SolidColorBrush(RedColor, .5);
                _colorBrushOpacity = .5;
            }
            else
            {
                _orangeBrush = new SolidColorBrush(OrangeColor, .75);
                _redBrush = new SolidColorBrush(RedColor, .75);
                _colorBrushOpacity = .75;
            }
            // if (change.Property == TrackProperty) _solidColorBrush = new ValurColorBrush(Color.Parse(Track.Color));
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        // _orangePoint1 = new Point(0, (1 - NormalizeDb(-3)) * Bounds.Height);
        // _orangePoint2 = new Point(Bounds.Width, (1 - NormalizeDb(-3)) * Bounds.Height);
        // _redPoint1 = new Point(0, (1 - NormalizeDb(0)) * Bounds.Height);
        // _redPoint2 = new Point(Bounds.Width, (1 - NormalizeDb(0)) * Bounds.Height);
        var waringLevelOrange = (1 - NormalizeDb(-3)) * Bounds.Height;
        var waringLevelRed = (1 - NormalizeDb(0)) * Bounds.Height;

        _backgroundRect = new Rect(0, 0, Bounds.Width, Bounds.Height);

        _gridList = new();
        for (int i = -6; i > MinDb; i -= 3)
        {
            _gridList.Add([
                new Point(Bounds.Width * .8, (1 - NormalizeDb(i)) * Bounds.Height),
                new Point(Bounds.Width, (1 - NormalizeDb(i)) * Bounds.Height)
            ]);
        }

        context.PushClip(_backgroundRect);

        context.DrawRectangle((new SolidColorBrush(Colors.Black,0.2)), null, _backgroundRect);

        
        //警告线
        {
            var r = 1.5;
            //-3dB Warning 警告电平
            // context.DrawLine(_orangePen,
            //     _orangePoint1,
            //     _orangePoint2);
            context.DrawEllipse(_backgroundOrangeBrush,null,
                new Point(Bounds.Width,waringLevelOrange),
                r,r);
            context.DrawEllipse(_backgroundOrangeBrush,null,
                new Point(0,waringLevelOrange),
                r,r);
            
            // //0dB Warning 警告电平
            // context.DrawLine(_redPen,
            //     _redPoint1,
            //     _redPoint2);
            context.DrawEllipse(_backgroundRedBrush,null,
                new Point(Bounds.Width,waringLevelRed),
                r,r);
            context.DrawEllipse(_backgroundRedBrush,null,
                new Point(0,waringLevelRed),
                r,r);
        }
        //Text
        // context.DrawText(_warningText1,
        //     _warningPoint1);
        // context.DrawText(_warningText2,
        //     _warningPoint2);


        // context.DrawRectangle(null, new Pen(ValurColorBrush), _backgroundRect);

        //PEAK
        if (Mode is LevelMeterMode.PeakRms or LevelMeterMode.Peak)
        {
            if (CurrentPeakLevel.Length < 3)
                return;
            
            //Left Channel
            {
                context.DrawRectangle(ColorBrush, null, new Rect(
                    -0.1, (1 - NormalizeDb(CurrentPeakLevel[0])) * Bounds.Height, Bounds.Width / 2,
                    Bounds.Height * NormalizeDb(CurrentPeakLevel[0])));
                if (CurrentPeakLevel[0] > -3)
                {
                    context.DrawRectangle(_orangeBrush, null, new Rect(
                        -0.1, (1 - NormalizeDb(CurrentPeakLevel[0])) * Bounds.Height, Bounds.Width / 2, Bounds.Height *
                        (NormalizeDb(CurrentPeakLevel[0]) - NormalizeDb(-3))));
                }

                if (CurrentPeakLevel[0] > 0)
                {
                    context.DrawRectangle(_redBrush, null, new Rect(
                        -0.1, (1 - NormalizeDb(CurrentPeakLevel[0])) * Bounds.Height, Bounds.Width / 2, Bounds.Height *
                        (NormalizeDb(CurrentPeakLevel[0]) - NormalizeDb(0))));
                }
            }
            //Right Channel
            {
                context.DrawRectangle(ColorBrush, null, new Rect(
                    0.1 + Bounds.Width / 2, (1 - NormalizeDb(CurrentPeakLevel[1])) * Bounds.Height, Bounds.Width / 2,
                    Bounds.Height * NormalizeDb(CurrentPeakLevel[1])));
                if (CurrentPeakLevel[1] > -3)
                {
                    context.DrawRectangle(_orangeBrush, null, new Rect(
                        0.1 + Bounds.Width / 2, (1 - NormalizeDb(CurrentPeakLevel[1])) * Bounds.Height,
                        Bounds.Width / 2, Bounds.Height *
                                          (NormalizeDb(CurrentPeakLevel[1]) - NormalizeDb(-3))));
                }

                if (CurrentPeakLevel[1] > 0)
                {
                    context.DrawRectangle(_redBrush, null, new Rect(
                        0.1 + Bounds.Width / 2, (1 - NormalizeDb(CurrentPeakLevel[1])) * Bounds.Height,
                        Bounds.Width / 2, Bounds.Height *
                                          (NormalizeDb(CurrentPeakLevel[1]) - NormalizeDb(0))));
                }
            }
        }

        //RMS
        if (Mode is LevelMeterMode.Rms or LevelMeterMode.PeakRms)
        {
            if (CurrentRmsLevel.Length < 3)
                return;
            
            //Left Channel
            {
                context.DrawRectangle(ColorBrush, null, new Rect(
                    -0.1, (1 - NormalizeDb(CurrentRmsLevel[0])) * Bounds.Height, Bounds.Width / 2,
                    Bounds.Height * NormalizeDb(CurrentRmsLevel[0])));
                if (CurrentRmsLevel[0] > -3)
                {
                    context.DrawRectangle(_orangeBrush, null, new Rect(
                        -0.1, (1 - NormalizeDb(CurrentRmsLevel[0])) * Bounds.Height, Bounds.Width / 2, Bounds.Height *
                        (NormalizeDb(CurrentRmsLevel[0]) - NormalizeDb(-3))));
                }

                if (CurrentRmsLevel[0] > 0)
                {
                    context.DrawRectangle(_redBrush, null, new Rect(
                        -0.1, (1 - NormalizeDb(CurrentRmsLevel[0])) * Bounds.Height, Bounds.Width / 2, Bounds.Height *
                        (NormalizeDb(CurrentRmsLevel[0]) - NormalizeDb(0))));
                }
            }
            //Right Channel
            {
                context.DrawRectangle(ColorBrush, null, new Rect(
                    0.1 + Bounds.Width / 2, (1 - NormalizeDb(CurrentRmsLevel[1])) * Bounds.Height, Bounds.Width / 2,
                    Bounds.Height * NormalizeDb(CurrentRmsLevel[1])));
                if (CurrentRmsLevel[1] > -3)
                {
                    context.DrawRectangle(_orangeBrush, null, new Rect(
                        0.1 + Bounds.Width / 2, (1 - NormalizeDb(CurrentRmsLevel[1])) * Bounds.Height, Bounds.Width / 2,
                        Bounds.Height *
                        (NormalizeDb(CurrentRmsLevel[1]) - NormalizeDb(-3))));
                }

                if (CurrentRmsLevel[1] > 0)
                {
                    context.DrawRectangle(_redBrush, null, new Rect(
                        0.1 + Bounds.Width / 2, (1 - NormalizeDb(CurrentRmsLevel[1])) * Bounds.Height, Bounds.Width / 2,
                        Bounds.Height *
                        (NormalizeDb(CurrentRmsLevel[1]) - NormalizeDb(0))));
                }
            }
        }


        //-3 0以外的参考线
        // for (int i = -6; i > MinDb; i -= 3)
        // {
        //     context.DrawLine(_whitePen,
        //         new Point(Bounds.Width*.8, (1 - NormalizeDb(i))*Bounds.Height),
        //         new Point(Bounds.Width, (1 - NormalizeDb(i))*Bounds.Height));
        // }
        foreach (var item in _gridList)
        {
            context.DrawLine(_whitePen,
                item[0], item[1]);
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        // InvalidateVisual();
    }

    //唉又是AI编程
    public float CalculateLevelDb(float[] buffer)
    {
        if (buffer == null)
            throw new ArgumentNullException(nameof(buffer));

        if (buffer.Length == 0)
            return MinDb;

        // 1. 找到样本中的峰值（绝对值最大的值）
        float peak = 0.0f;
        foreach (float sample in buffer)
        {
            float absoluteValue = Math.Abs(sample);
            if (absoluteValue > peak)
            {
                peak = absoluteValue;
            }
        }

        // 2. 防止 log10(0) 导致的无穷大
        if (peak < 1e-6) // 一个非常小的阈值，避免处理零值
        {
            return MinDb;
        }

        // 3. 转换为峰值分贝
        double peakDb = 20 * Math.Log10(peak);

        // 4. 确保结果在设定的范围内
        return (float)Math.Max(peakDb, MinDb);
    }

    public float NormalizeDb(float dbValue)
    {
        // 安全检查：范围和压缩因子有效性
        if (MaxDb <= MinDb)
            throw new InvalidOperationException("MaxDb 必须大于 MinDb。");
        if (CompressionFactor <= 0)
            throw new InvalidOperationException("压缩因子必须大于0。");

        // 1. 钳位分贝值到 [MinDb, MaxDb]
        float clampedDb = Math.Max(dbValue, MinDb);
        clampedDb = Math.Min(clampedDb, MaxDb);

        // 2. 分贝 → 线性幅度 → 压缩幅度
        double amplitude = Math.Pow(10.0, clampedDb / 20.0);
        double compressedAmplitude = Math.Pow(amplitude, CompressionFactor); // 核心：加入压缩

        // 3. 计算 MinDb/MaxDb 对应的压缩后幅度
        double minAmplitude = Math.Pow(10.0, MinDb / 20.0);
        double maxCompressedAmplitude = Math.Pow(Math.Pow(10.0, MaxDb / 20.0), CompressionFactor);
        double minCompressedAmplitude = Math.Pow(minAmplitude, CompressionFactor);

        // 4. 归一化到 [0, 1]
        float normalizedValue = (float)((compressedAmplitude - minCompressedAmplitude) /
                                        (maxCompressedAmplitude - minCompressedAmplitude));

        return normalizedValue;
    }
}