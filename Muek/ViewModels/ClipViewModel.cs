using System;
using System.Collections.Generic;
using Avalonia.Platform;
using Muek.Engine;
using Muek.Models;
using Muek.Services;
using Muek.Views;
using NAudio.Midi;
using NAudio.Wave;

namespace Muek.ViewModels;

public class ClipViewModel
{
    public Clip Proto { get; }

    // 本地缓存字段，不参与 proto 序列化
    public float[]? CachedWaveform { get; set; }
    public int CachedSampleRate { get; set; }
    public int CachedChannels { get; set; }
    public bool IsRendering { get; set; }

    public double StartBeat => Proto.StartBeat;
    public double Duration => Proto.Duration;
    public double Offset => Proto.Offset;
    public string? Path => Proto.Path;
    public string? Name => Proto.Name;

    public List<PianoRoll.Note>? Notes { get; set; } = null;

    public double SourceDuration { get; set; }
    // public List<float>? LOD0 { get; private set; }
    // public List<float>? LOD1 { get; private set; }
    // public List<float>? LOD2 { get; private set; }
    // public List<float>? LOD3 { get; private set; }
    
    public PatternViewModel? LinkedPattern { get; set; } = null;

    public ClipViewModel(Clip proto)
    {
        Proto = proto;
        SourceDuration = proto.Duration;
    }

    [Obsolete("取绝对值不太对，弃用了吧")]
    // ReSharper disable once UnusedMember.Global
    public void GenerateWaveformPreviewAbs(int sampleCount = 10000)
    {
        if(Notes is not null) return;
        try
        {
            using var reader = new AudioFileReader(Path);
            var totalSamples = (int)(reader.Length / (reader.WaveFormat.BitsPerSample / 8));
            var buffer = new float[totalSamples];
            var read = reader.Read(buffer, 0, totalSamples);

            var step = Math.Max(1, read / sampleCount);
            int actualCount = (int)Math.Ceiling((double)read / step);

            var result = new float[actualCount];
            int idx = 0;

            for (var i = 0; i < read; i += step)
            {
                float max = 0;
                for (var j = i; j < i + step && j < read; j++)
                {
                    max = Math.Max(max, Math.Abs(buffer[j]));
                }

                result[idx++] = max;
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
        if (Notes is not null) return;
        try
        {
            using var reader = new AudioFileReader(Path);

            // 估算采样数（每通道）
            long totalSamples = reader.Length / (reader.WaveFormat.BitsPerSample / 8);
            long estimatedPerChannel = totalSamples / reader.WaveFormat.Channels;

            var result = new List<float>((int)estimatedPerChannel);

            var buffer = new float[4096];
            int read;
            int channels = reader.WaveFormat.Channels;

            while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (int i = 0; i < read; i ++)
                {
                    result.Add(buffer[i]); // 只取第一个通道
                }
            }

            var arr = result.ToArray();
            CachedWaveform = arr;

            var id = Proto.Id;

            unsafe
            {
                fixed (float* arrPtr = arr)
                {
                    fixed (char* str = id)
                    {
                        MuekEngine.cache_clip_data((ushort*)str, Proto.Id.Length, arrPtr, arr.Length);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"[Waveform] Failed to read: {e.Message}");
        }
    }

    public void UpdateFromPattern()
    {
        if(LinkedPattern is null) return;
        Notes = new List<PianoRoll.Note>();
        foreach (var noteList in LinkedPattern.Notes)
        {
            foreach (var note in noteList)
            {
                Notes.AddRange(note with
                {
                    StartTime = note.StartTime / DataStateService.Midi2TrackFactor,
                    EndTime = note.EndTime / DataStateService.Midi2TrackFactor,
                });
            }
        }

        double trackEnd = 0;
        foreach (var note in Notes)
        {
            trackEnd = double.Max(trackEnd, note.EndTime);
        }
        trackEnd /= DataStateService.Subdivisions;
        // trackEnd /= DataStateService.Midi2TrackFactor;
        SourceDuration = trackEnd;
    }

    public void UpdateFromClip()
    {
        //TODO
    }
}