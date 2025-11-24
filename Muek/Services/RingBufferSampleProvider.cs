using System;
using NAudio.Wave;
using RingBuffer;

namespace Muek.Services;

public class RingBufferSampleProvider : ISampleProvider
{
    private readonly RingBuffer<float> _buffer;
    private readonly WaveFormat _waveFormat;

    // 用于 RMS 计算
    public event Action<float[]>? OnRmsCalculated;
    public event Action<float[]>? OnPeakCalculated;

    public RingBufferSampleProvider(RingBuffer<float> buffer, WaveFormat format)
    {
        _buffer = buffer;
        _waveFormat = format;
    }

    public WaveFormat WaveFormat => _waveFormat;

    public int Read(float[] buffer, int offset, int count)
    {
        // 1. 从 RingBuffer 读取数据
        int samplesRead = 0;
        
        // RingBuffer.NET 的 Get() 是单个取的，虽然性能略低但为了符合你的库用法
        // 实际高性能场景建议给 RingBuffer 库加一个 Get(Span<T>) 方法
        while (samplesRead < count && _buffer.Size > 0)
        {
            buffer[offset + samplesRead] = _buffer.Get();
            samplesRead++;
        }

        // 2. 如果 RingBuffer 空了（例如计算跟不上），补静音防止爆音
        if (samplesRead < count)
        {
            Array.Clear(buffer, offset + samplesRead, count - samplesRead);
            // 这里返回 count 表示即使没数据也输出了静音，保持流不断
            samplesRead = count; 
        }

        // 3. 计算这一帧的 RMS (供电平表使用)
        CalculateRms(buffer, offset, count);
        CalculatePeak(buffer, offset, count);

        return samplesRead;
    }

    // private void CalculateRms(float[] buffer, int offset, int count)
    // {
    //     if (count <= 0) return;
    //     double sumSquare = 0;
    //     for (int i = 0; i < count; i++)
    //     {
    //         float sample = buffer[offset + i];
    //         sumSquare += sample * sample;
    //     }
    //     double rms = Math.Sqrt(sumSquare / count);
    //     double db = 20 * Math.Log10(rms + 1e-9);
    //     if (db < -160) db = -160;
    //     
    //     OnRmsCalculated?.Invoke((float)db);
    // }
    //
    // private void CalculatePeak(float[] buffer, int offset, int count)
    // {
    //     // 1. 参数校验
    //     if (count <= 0) return;
    //
    //     // 2. 找到指定范围内的峰值（绝对值最大的值）
    //     float peak = 0.0f;
    //     for (int i = 0; i < count; i++)
    //     {
    //         // 从 offset 开始索引
    //         float absoluteValue = Math.Abs(buffer[offset + i]);
    //         if (absoluteValue > peak)
    //         {
    //             peak = absoluteValue;
    //         }
    //     }
    //
    //     // 3. 防止 log10(0) 导致的无穷大
    //     // if (peak < 1e-9) // 使用与RMS函数中类似的小值，保持一致性
    //     // {
    //     //     OnPeakCalculated?.Invoke(MinDb);
    //     //     return;
    //     // }
    //
    //     // 4. 转换为峰值分贝
    //     float peakDb = 20 * float.Log10(peak);
    //
    //     // 5. 确保结果在设定的范围内
    //     // float resultDb = (float)Math.Max(peakDb, MinDb);
    //
    //     // 6. 通过事件回调结果
    //     OnPeakCalculated?.Invoke(peakDb);
    // }
    private void CalculateRms(float[] buffer, int offset, int count)
    {
        if (count <= 0) return;

        // 为左右声道分别初始化平方和
        double leftSumSquare = 0;
        double rightSumSquare = 0;

        // 遍历缓冲区，步长为2（处理L和R）
        // 注意：这里假设count是偶数，且缓冲区足够大
        for (int i = 0; i < count; i += 2)
        {
            float leftSample = buffer[offset + i];
            float rightSample = buffer[offset + i + 1];

            leftSumSquare += leftSample * leftSample;
            rightSumSquare += rightSample * rightSample;
        }

        // 计算帧数（总样本数 / 2）
        int frameCount = count / 2;

        // 分别计算左右声道的RMS和dB
        double leftRms = Math.Sqrt(leftSumSquare / frameCount);
        double rightRms = Math.Sqrt(rightSumSquare / frameCount);

        double leftDb = 20 * Math.Log10(leftRms + 1e-9);
        double rightDb = 20 * Math.Log10(rightRms + 1e-9);

        if (leftDb < -160) leftDb = -160;
        if (rightDb < -160) rightDb = -160;

        // 回调包含左右声道结果的数组
        OnRmsCalculated?.Invoke(new float[] { (float)leftDb, (float)rightDb });
    }
    
    private void CalculatePeak(float[] buffer, int offset, int count)
    {
        if (count <= 0) return;

        // 为左右声道分别找到峰值
        float leftPeak = 0.0f;
        float rightPeak = 0.0f;

        // 遍历缓冲区，步长为2
        for (int i = 0; i < count; i += 2)
        {
            float leftAbsoluteValue = Math.Abs(buffer[offset + i]);
            if (leftAbsoluteValue > leftPeak)
            {
                leftPeak = leftAbsoluteValue;
            }

            float rightAbsoluteValue = Math.Abs(buffer[offset + i + 1]);
            if (rightAbsoluteValue > rightPeak)
            {
                rightPeak = rightAbsoluteValue;
            }
        }

        // 分别转换为峰值分贝
        float leftPeakDb = 20 * (float)Math.Log10(leftPeak + 1e-9);
        float rightPeakDb = 20 * (float)Math.Log10(rightPeak + 1e-9);

        // 回调结果数组
        OnPeakCalculated?.Invoke(new float[] { leftPeakDb, rightPeakDb });
    }
}