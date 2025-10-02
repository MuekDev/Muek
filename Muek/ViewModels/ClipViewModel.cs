using System;
using System.Collections.Generic;
using Muek.Models;
using NAudio.Wave;

namespace Muek.ViewModels;

public class ClipViewModel
{
    public Clip Proto { get; }

    // 本地缓存字段，不参与 proto 序列化
    public List<float>? CachedWaveform { get; set; }
    public bool IsRendering { get; set; }

    public double StartBeat => Proto.StartBeat;
    public double Duration => Proto.Duration;
    public double Offset => Proto.Offset;
    public string Path => Proto.Path;
    public string Name => Proto.Name;
    public double SourceDuration { get; set; }
    // public List<float>? LOD0 { get; private set; }
    // public List<float>? LOD1 { get; private set; }
    // public List<float>? LOD2 { get; private set; }
    // public List<float>? LOD3 { get; private set; }

    public ClipViewModel(Clip proto)
    {
        Proto = proto;
        SourceDuration = proto.Duration;
    }

    [Obsolete("取绝对值不太对，弃用了吧")]
    // ReSharper disable once UnusedMember.Global
    public void GenerateWaveformPreviewAbs(int sampleCount = 10000)
    {
        try
        {
            using var reader = new AudioFileReader(Path);
            var totalSamples = (int)(reader.Length / (reader.WaveFormat.BitsPerSample / 8));
            var buffer = new float[totalSamples];
            var read = reader.Read(buffer, 0, totalSamples);

            var step = Math.Max(1, read / sampleCount);
            var result = new List<float>(sampleCount);

            for (var i = 0; i < read; i += step)
            {
                float max = 0;
                for (var j = i; j < i + step && j < read; j++)
                {
                    max = Math.Max(max, Math.Abs(buffer[j]));
                }

                result.Add(max);
            }

            CachedWaveform = result;
        }
        catch (Exception e)
        {
            Console.WriteLine($"[Waveform] Failed to read: {e.Message}");
        }
    }

    public void GenerateWaveformPreviewPure()
    {
        try
        {
            using var reader = new AudioFileReader(Path);
            var buffer = new float[4096];
            var result = new List<float>();
            int read;

            // 只取第一个通道（或混合通道）
            var channels = reader.WaveFormat.Channels;

            while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (int i = 0; i < read; i += channels)
                {
                    result.Add(buffer[i]); // 使用第一个通道
                }
            }

            CachedWaveform = result;
        }
        catch (Exception e)
        {
            Console.WriteLine($"[Waveform] Failed to read: {e.Message}");
        }
    }

    [Obsolete("<UNK>")]
    private List<float> GenerateLODLevel(int factor)
    {
        var samples = CachedWaveform!;
        int newLength = samples.Count / factor;
        var lod = new List<float>(newLength);

        for (int i = 0; i < newLength; i++)
        {
            int start = i * factor;
            int end = Math.Min(start + factor, samples.Count);
            float max = float.MinValue;
            float min = float.MaxValue;
            for (int j = start; j < end; j++)
            {
                var s = samples[j];
                if (s > max) max = s;
                if (s < min) min = s;
            }
            lod.Add((max + min) * 0.5f);
        }

        return lod;
    }

    // public void GenerateLodWaveform()
    // {
    //     LOD0 = GenerateLODLevel(0);
    //     LOD1 = GenerateLODLevel(2);
    //     LOD2 = GenerateLODLevel(4);
    //     LOD3 = GenerateLODLevel(8);
    // }
}