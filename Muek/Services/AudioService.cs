using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using NAudio.Utils;
using NAudio.Wave;

namespace Muek.Services;

public static class AudioService
{
    private static readonly WaveOutEvent WaveOut = new();

    // 作为小县城唯一一个看过人寿的人，我眼神中的寒芒足以吓退10公里内的狗
    public static void Play()
    {
        var samples = Render();

        var waveProvider = new BufferedWaveProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2))
        {
            BufferLength = samples.Count * sizeof(float),
            DiscardOnBufferOverflow = true
        };

        byte[] buffer = new byte[samples.Count * sizeof(float)];
        Buffer.BlockCopy(samples.ToArray(), 0, buffer, 0, buffer.Length);
        waveProvider.AddSamples(buffer, 0, buffer.Length);

        var output = WaveOut;
        output.Init(waveProvider);
        output.Play();

        int channels = 2;
        int bytesPerFrame = sizeof(float) * channels;

        Task.Run(() =>
        {
            int sampleRate = 44100;
            int channels = 2;
            int bytesPerFrame = sizeof(float) * channels;
            double bpm = DataStateService.Bpm;

            while (output.PlaybackState == PlaybackState.Playing)
            {
                long bytesPlayed = output.GetPosition();
                long framesPlayed = bytesPlayed / bytesPerFrame;

                double currentSec = (double)framesPlayed / sampleRate / 4;
                double currentBeats = currentSec / (60.0 / bpm);

                UiStateService.InvokeUpdatePlayheadPos(currentBeats);

                Thread.Sleep(20);
            }

            // 播放结束，强制跳到最后
            // double totalSec = (double)(samples.Count / channels) / sampleRate;
            // double totalBeats = totalSec / (60.0 / bpm);
            // UiStateService.InvokeUpdatePlayheadPos(totalBeats);
        });


        Console.WriteLine("正在播放...");
    }

    public static void Stop()
    {
        WaveOut.Stop();
    }

    private static List<float> Render()
    {
        int sampleRate = 44100;            // 采样率
        double bpm = DataStateService.Bpm; // BPM
        int beatsPerBar = 4;               // 固定 4/4 拍
        int channels = 2;                  // 双声道

        var tracks = DataStateService.Tracks;
        var trackBuffers = new List<List<float>>();
        int maxLength = 0;

        foreach (var track in tracks)
        {
            var currentTrack = new List<float>();

            foreach (var clip in track.Clips)
            {
                if (clip.CachedWaveform == null) continue;

                var sss = clip.CachedWaveform;

                // ===== 计算 offset 和 duration（单声道样本长度） =====
                int durationSamples = (int)Math.Round(((clip.Duration * 60.0) / bpm) * sampleRate) * beatsPerBar;
                int offsetSamples = (int)Math.Round(((clip.Offset * 60.0) / bpm) * sampleRate);

                offsetSamples = Math.Min(offsetSamples, sss.Count);
                int endSample = Math.Min(offsetSamples + durationSamples, sss.Count);

                // 单声道裁剪
                var clipSamplesMono = sss.GetRange(offsetSamples, endSample - offsetSamples);

                // ===== 计算 clip 在整轨中的起始位置（保持原逻辑） =====
                int startSample =
                    (int)Math.Round(((clip.StartBeat * 60.0) / bpm) * sampleRate)
                    * beatsPerBar * channels;

                // 补齐前导 0
                if (currentTrack.Count < startSample)
                {
                    int padLen = startSample - currentTrack.Count;
                    currentTrack.AddRange(Enumerable.Repeat(0f, padLen));
                }

                // ===== 转换为双声道并加入轨道 =====
                foreach (var sample in clipSamplesMono)
                {
                    currentTrack.Add(sample); // 左声道
                    currentTrack.Add(sample); // 右声道
                }
            }

            maxLength = Math.Max(maxLength, currentTrack.Count);
            trackBuffers.Add(currentTrack);
        }

        // ===== 轨道混音 =====
        var finalBuffer = new float[maxLength];
        foreach (var track in trackBuffers)
        {
            for (int i = 0; i < maxLength; i++)
            {
                if (i < track.Count)
                    finalBuffer[i] += track[i];
            }
        }

        return new List<float>(finalBuffer);
    }

    /// <summary>
    /// 解码音频文件（支持 MP3 / WAV）为 float[]
    /// </summary>
    /// <param name="path">音频文件路径</param>
    /// <returns>浮点数组（-1.0 ~ 1.0 之间）</returns>
    public static float[] DecodeFromFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("音频文件未找到", path);

        using var reader = new AudioFileReader(path); // 自动支持 wav / mp3 / aac / wma
        var samples = new List<float>();

        Console.WriteLine($"sample rate: {reader.WaveFormat.SampleRate} channels: {reader.WaveFormat.Channels}");
        float[] buffer = new float[reader.WaveFormat.SampleRate * reader.WaveFormat.Channels];
        int read;
        while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
        {
            for (int i = 0; i < read; i++)
            {
                samples.Add(buffer[i]);
            }
        }

        return samples.ToArray();
    }
}