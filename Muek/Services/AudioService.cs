using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Muek.Helpers;
using Muek.ViewModels;
using NAudio.CoreAudioApi;
using NAudio.Utils;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Muek.Services;

// 作为小县城唯一一个看过人寿的人，我眼神中的寒芒足以吓退10公里内的狗
public static class AudioService
{
    private static WasapiOut? _wasapiOut;

    private static int MasterSampleRate = 44100; // 临时值

    public static void Play()
    {
        // 1) 获取默认输出设备与其 mix format（采样率）
        var device = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
        int deviceSampleRate = device.AudioClient.MixFormat.SampleRate;
        int channels = device.AudioClient.MixFormat.Channels;

        MasterSampleRate = deviceSampleRate; // 直接咣当一个精准定位

        // 2) 确保所有 clip 的 CachedWaveform 都是交错 stereo 且采样率 == MasterSampleRate
        // TODO: 改为后台静默处理，因为这个会拖慢冷启动时间
        EnsureAllClipsCachedAtSampleRate(MasterSampleRate, channels);

        // 3) 渲染混音（返回交错 stereo 的 List<float>，采样率 = MasterSampleRate）
        var samples = Render();

        // 4) 播放：使用与设备 mix format 一致的 WaveFormat
        var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(MasterSampleRate, channels);

        var waveProvider = new BufferedWaveProvider(waveFormat)
        {
            BufferLength = samples.Length * sizeof(float),
            DiscardOnBufferOverflow = true
        };

        // copy floats -> bytes
        var floatArr = samples;
        byte[] buffer = new byte[floatArr.Length * sizeof(float)];
        Buffer.BlockCopy(floatArr, 0, buffer, 0, buffer.Length);
        waveProvider.AddSamples(buffer, 0, buffer.Length);

        // stop previous if any
        _wasapiOut?.Stop();
        _wasapiOut?.Dispose();

        _wasapiOut = new WasapiOut(device, AudioClientShareMode.Shared, false, 100);
        _wasapiOut.Init(waveProvider);
        _wasapiOut.Play();

        Task.Run(() =>
        {
            int sampleRate = MasterSampleRate;
            int channels = 2;
            int bytesPerFrame = sizeof(float) * channels;
            double bpm = DataStateService.Bpm;

            while (_wasapiOut.PlaybackState == PlaybackState.Playing)
            {
                long bytesPlayed = _wasapiOut.GetPosition();
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
        _wasapiOut?.Stop();
    }

    private static float[] Render()
    {
        int sampleRate = MasterSampleRate; // 见上 Play()
        double bpm = DataStateService.Bpm; // BPM
        int beatsPerBar = 4;               // 固定 4/4 拍
        int channels = 2;                  // 目标输出通道数（交错 stereo）

        var tracks = DataStateService.Tracks;
        // TODO: 仍然无法避免拷贝问题
        var trackBuffers = new List<float[]>();
        int maxLength = 0;

        foreach (var track in tracks)
        {
            var trackArray = GetTrackBuffer(track, bpm, beatsPerBar, channels, sampleRate);
            maxLength = Math.Max(maxLength, trackArray.Length);
            trackBuffers.Add(trackArray);
        }

        // ===== 轨道混音 =====
        var finalBuffer = new float[maxLength];
        foreach (var track in trackBuffers)
        {
            int len = track.Length;
            for (int i = 0; i < len; i++)
            {
                finalBuffer[i] += track[i];
            }
        }

        return finalBuffer; // float[]
    }

    public static float[] GetTrackBuffer(TrackViewModel track, double bpm, int sampleRate, int beatsPerBar, int channels)
    {
        // TODO: 这里用 List<float> 临时存储长度不确定的轨道
        // 最终 ToArray()
        var currentTrack = new List<float>();

        foreach (var clip in track.Clips)
        {
            if (clip.CachedWaveform == null || clip.CachedWaveform.Length == 0)
                continue;

            var sss = clip.CachedWaveform; // 交错 stereo L R L R

            // ===== 计算 offset 和 duration（单声道样本长度） =====
            int durationSamplesMono = (int)Math.Round(((clip.Duration * 60.0) / bpm) * sampleRate) * beatsPerBar;
            int offsetSamplesMono = (int)Math.Round(((clip.Offset * 60.0) / bpm) * sampleRate);

            // 转换为交错数组索引
            long offsetIndex = (long)offsetSamplesMono * channels;
            long durationCount = (long)durationSamplesMono * channels;

            if (offsetIndex >= sss.Length)
                continue; // 完全脱出！

            long endIndex = Math.Min(offsetIndex + durationCount, sss.Length);
            int copyLen = (int)(endIndex - offsetIndex);
            if (copyLen <= 0) continue;

            // 切片（交错 stereo）
            var clipSamplesInterleaved = new float[copyLen];
            Array.Copy(sss, (int)offsetIndex, clipSamplesInterleaved, 0, copyLen);

            // ===== 计算 clip 在整轨中的起始位置 =====
            int startSample =
                (int)Math.Round(((clip.StartBeat * 60.0) / bpm) * sampleRate)
                * beatsPerBar * channels;

            // 补齐前导 0
            if (currentTrack.Count < startSample)
            {
                int padLen = startSample - currentTrack.Count;
                if (padLen > 0)
                    currentTrack.AddRange(new float[padLen]);
            }
            // 加入轨道
            currentTrack.AddRange(clipSamplesInterleaved);
        }
        return currentTrack.ToArray();
    }


    private static void EnsureAllClipsCachedAtSampleRate(int targetSampleRate, int targetChannels)
    {
        foreach (var track in DataStateService.Tracks)
        {
            foreach (var clip in track.Clips)
            {
                // 如果没有 CachedWaveform 或者你知道它不是 target rate，则重新解码/重采样
                // TODO: 为简单起见，总是（或首次加载时）用 DecodeFromFile(..., targetSampleRate) 获取并写入 CachedWaveform
                if (string.IsNullOrEmpty(clip.Path)) continue;
                // TODO: 可以添加判断 clip.AlreadyResampledToRate 来避免重复重采样
                clip.CachedWaveform = DecodeAndResample(clip.Path, targetSampleRate, targetChannels);
            }
        }
    }

    public static List<float> DecodeFromFile(string path, int targetSampleRate, int targetChannels)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("音频文件未找到", path);

        using var reader = new AudioFileReader(path); // returns float samples at reader.WaveFormat.SampleRate
        int srcRate = reader.WaveFormat.SampleRate;
        int srcChannels = reader.WaveFormat.Channels;

        // read into buffer
        var raw = new List<float>();
        float[] buf = new float[1024 * srcChannels];
        int r;
        while ((r = reader.Read(buf, 0, buf.Length)) > 0)
        {
            for (int i = 0; i < r; i++) raw.Add(buf[i]);
        }

        // if mono -> convert to stereo interleaved first
        List<float> interleaved;
        if (srcChannels == 1 && targetChannels == 2)
        {
            interleaved = new List<float>(raw.Count * 2);
            for (int i = 0; i < raw.Count; i++)
            {
                interleaved.Add(raw[i]);
                interleaved.Add(raw[i]);
            }
        }
        else if (srcChannels == 2)
        {
            interleaved = raw;
        }
        else
        {
            throw new NotSupportedException("只支持 mono 或 stereo 源");
        }

        // if sampling rates match, return interleaved as List<float>
        if (srcRate == targetSampleRate) return interleaved;

        // 重采样(暂定)：调用立体声重采样（分离左右 -> 重采样 -> 交错）
        var resampled = ResampleStereo(interleaved, srcRate, targetSampleRate);
        return resampled;
    }

    public static float[] DecodeAndResample(string path, int targetSampleRate, int targetChannels)
    {
        if (!System.IO.File.Exists(path))
            throw new System.IO.FileNotFoundException("音频文件未找到", path);

        if (targetChannels != 1 && targetChannels != 2)
            throw new ArgumentException("只支持 targetChannels = 1 或 2", nameof(targetChannels));

        using var reader = new AudioFileReader(path); // 自动解码为 float，返回 reader.WaveFormat
        ISampleProvider source = reader;              // 原始采样流（float）

        int srcChannels = reader.WaveFormat.Channels;
        int srcSampleRate = reader.WaveFormat.SampleRate;

        // 1) 通道转换（在重采样前或后都可；这里先做通道匹配更直观）
        if (srcChannels == 1 && targetChannels == 2)
        {
            source = new MonoToStereoSampleProvider(source); // 复制单声道到 L/R
        }
        else if (srcChannels == 2 && targetChannels == 1)
        {
            // Stereo -> mono: 将左右平均为一个通道（你可以自定义权重）
            source = new StereoToMonoSampleProvider(source) { LeftVolume = 0.5f, RightVolume = 0.5f };
        }
        else if (srcChannels != targetChannels)
        {
            // 支持更多通道的情况下可以扩展，这里简单抛错
            throw new NotSupportedException($"不支持源通道 {srcChannels} 到目标通道 {targetChannels} 的自动转换");
        }

        // 2) 采样率重采样（如果不同）
        if (srcSampleRate != targetSampleRate)
        {
            // WdlResamplingSampleProvider 提供高质量重采样（较快且质量不错）
            source = new WdlResamplingSampleProvider(source, targetSampleRate);
        }

        // 3) 读取全部样本到 List<float>
        // 预先估算容量：使用 reader.TotalTime 可以给出较好的估计
        double totalSeconds = reader.TotalTime.TotalSeconds;
        int estimatedSamples = (int)Math.Ceiling(totalSeconds * targetSampleRate * targetChannels) + 1024;
        var samples = new List<float>(Math.Max(estimatedSamples, 4096));

        // 读取缓冲区：长度为 8192 * channels 的帧数（可调整）
        float[] buffer = new float[8192 * targetChannels];
        int read;
        while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
        {
            // 把读到的样本追加到集合
            for (int i = 0; i < read; i++)
                samples.Add(buffer[i]);
        }

        return samples.ToArray();
    }

    // （可能是线性插值）
    public static List<float> ResampleStereo(List<float> samples, int fromRate, int toRate)
    {
        if (samples.Count % 2 != 0) throw new ArgumentException("立体声长度应为偶数");
        int frameCount = samples.Count / 2;
        var left = new List<float>(frameCount);
        var right = new List<float>(frameCount);
        for (int i = 0; i < frameCount; i++)
        {
            left.Add(samples[i * 2]);
            right.Add(samples[i * 2 + 1]);
        }

        var leftR = ResampleMono(left, fromRate, toRate);
        var rightR = ResampleMono(right, fromRate, toRate);
        int newFrames = leftR.Count;
        var inter = new List<float>(newFrames * 2);
        for (int i = 0; i < newFrames; i++)
        {
            inter.Add(leftR[i]);
            inter.Add(rightR[i]);
        }

        return inter;
    }

    public static List<float> ResampleMono(List<float> samples, int fromRate, int toRate)
    {
        if (fromRate == toRate) return new List<float>(samples);
        double ratio = (double)toRate / fromRate;
        int newLength = (int)Math.Round(samples.Count * ratio);
        var outList = new List<float>(newLength);
        for (int i = 0; i < newLength; i++)
        {
            double pos = i / ratio;
            int idx = (int)Math.Floor(pos);
            double frac = pos - idx;
            float s0 = idx < samples.Count ? samples[idx] : 0f;
            float s1 = (idx + 1) < samples.Count ? samples[idx + 1] : 0f;
            outList.Add((float)((1 - frac) * s0 + frac * s1));
        }

        return outList;
    }
}