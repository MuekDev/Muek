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
using Muek.Services;
using Muek.ViewModels;

namespace Muek.Views;

public partial class MixerLevelMeter : UserControl
{
    public TrackViewModel Track;
    private const float MinDb = -114.0f;
    private const float MaxDb = 6f;

    private const float CompressionFactor = 0.15f;
    
    public float CurrentLevel => AudioService.CurrentDb;

    public MixerLevelMeter()
    {
        InitializeComponent();
        ClipToBounds = false;
        AudioService.DbChanged += AudioServiceOnDbChanged;
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        
    }

    private void AudioServiceOnDbChanged(object? sender, float f)
    {
        Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Render);
    }

    private readonly Pen _orangePen = new Pen(new SolidColorBrush(Colors.Orange,.5));
    private readonly Pen _redPen = new Pen(new SolidColorBrush(Colors.Red,.5));
    private Point _orangePoint1;
    private Point _orangePoint2;
    private Point _redPoint1;
    private Point _redPoint2;

    private readonly FormattedText _warningText1 = new FormattedText("-3dB",
        CultureInfo.CurrentCulture, FlowDirection.LeftToRight,Typeface.Default, 10,new SolidColorBrush(Colors.Orange,.5));
    private readonly FormattedText _warningText2 = new FormattedText("0dB",
        CultureInfo.CurrentCulture, FlowDirection.LeftToRight,Typeface.Default, 10,new SolidColorBrush(Colors.Red,.5));

    private Point _warningPoint1;
    private Point _warningPoint2;

    private Rect _backgroundRect;

    private readonly Pen _whitePen = new Pen(new SolidColorBrush(Colors.White, .2));
    
    private List<Point[]> _gridList = new();
    
    
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        
        _orangePoint1 = new Point(Bounds.Width * .7, (1 - NormalizeDb(-3)) * Bounds.Height);
        _orangePoint2 = new Point(Bounds.Width, (1 - NormalizeDb(-3)) * Bounds.Height);
        _redPoint1 = new Point(Bounds.Width * .6, (1 - NormalizeDb(0)) * Bounds.Height);
        _redPoint2 = new Point(Bounds.Width, (1 - NormalizeDb(0)) * Bounds.Height);
        
        _warningPoint1 = new Point(Bounds.Width+5, (1 - NormalizeDb(-3))*Bounds.Height-8);
        _warningPoint2 = new Point(Bounds.Width+5, (1 - NormalizeDb(0))*Bounds.Height-8);
        
        _backgroundRect = new Rect(0,0,Bounds.Width,Bounds.Height);

        _gridList = new();
        for (int i = -6; i > MinDb; i -= 3)
        {
            _gridList.Add([
                new Point(Bounds.Width*.8, (1 - NormalizeDb(i))*Bounds.Height),
                new Point(Bounds.Width, (1 - NormalizeDb(i))*Bounds.Height)]);
        }
        
        
        context.DrawRectangle(Brush.Parse("#232323"),null ,_backgroundRect);

        
        //警告线
        {
            //-3dB Warning 警告电平
            context.DrawLine(_orangePen,
                _orangePoint1,
                _orangePoint2);

            //0dB Warning 警告电平
            context.DrawLine(_redPen,
                _redPoint1,
                _redPoint2);
        }
        //Text
        context.DrawText(_warningText1,
            _warningPoint1);
        context.DrawText(_warningText2,
            _warningPoint2);
        
        context.DrawRectangle(null,new Pen(Brush.Parse(Track.Color)),_backgroundRect);
        context.DrawRectangle(Brush.Parse(Track.Color), null,new Rect(
            0, (1 - NormalizeDb(CurrentLevel)) * Bounds.Height,Bounds.Width,Bounds.Height * NormalizeDb(CurrentLevel)));

        
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
                item[0],item[1]);
        }
        
        if (CurrentLevel > -3)
        {
            context.DrawRectangle(Brushes.Orange, null,new Rect(
                0, (1 - NormalizeDb(CurrentLevel)) * Bounds.Height,Bounds.Width,Bounds.Height *
                (NormalizeDb(CurrentLevel) - NormalizeDb(-3))));
        }

        if (CurrentLevel > 0)
        {
            context.DrawRectangle(Brushes.Red, null,new Rect(
                0, (1 - NormalizeDb(CurrentLevel)) * Bounds.Height,Bounds.Width,Bounds.Height *
                (NormalizeDb(CurrentLevel) - NormalizeDb(0))));
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