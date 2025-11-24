using System;
using NAudio.Wave;
using RingBuffer;

namespace Muek.Services;

public class RingBufferSampleProvider : ISampleProvider
{
    private readonly RingBuffer<float> _buffer;
    private readonly WaveFormat _waveFormat;

    // 用于 RMS 计算
    public event Action<float>? OnRmsCalculated;

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

        return samplesRead;
    }

    private void CalculateRms(float[] buffer, int offset, int count)
    {
        if (count <= 0) return;
        double sumSquare = 0;
        for (int i = 0; i < count; i++)
        {
            float sample = buffer[offset + i];
            sumSquare += sample * sample;
        }
        double rms = Math.Sqrt(sumSquare / count);
        double db = 20 * Math.Log10(rms + 1e-9);
        if (db < -160) db = -160;
        
        OnRmsCalculated?.Invoke((float)db);
    }
}