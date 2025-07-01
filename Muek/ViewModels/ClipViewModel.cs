using System;
using System.Collections.Generic;
using Audio;
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
    public string Path => Proto.Path;
    public string Name => Proto.Name;

    public ClipViewModel(Clip proto)
    {
        Proto = proto;
    }
    
    public void GenerateWaveformPreview(int sampleCount = 10000)
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

}
