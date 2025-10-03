using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using NAudio.CoreAudioApi;
using NAudio.Utils;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Muek.Services;

// 作为小县城唯一一个看过人寿的人，我眼神中的寒芒足以吓退10公里内的狗
public static class AudioService
{
    private static WasapiOut? _wasapiOut;

    // 不再硬编码 SampleRate；Play() 时根据设备确定
    private static int MasterSampleRate = 44100; // 临时值

    public static void Play()
    {
        // 1) 获取默认输出设备与其 mix format（采样率）
        var device = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
        int deviceSampleRate = device.AudioClient.MixFormat.SampleRate;
        int channels = device.AudioClient.MixFormat.Channels; 
        
        MasterSampleRate = deviceSampleRate;    // 直接咣当一个精准定位

        // 2) 确保所有 clip 的 CachedWaveform 都是交错 stereo 且采样率 == MasterSampleRate
        // TODO: 改为后台静默处理，因为这个会拖慢冷启动时间
        EnsureAllClipsCachedAtSampleRate(MasterSampleRate, channels);

        // 3) 渲染混音（返回交错 stereo 的 List<float>，采样率 = MasterSampleRate）
        var samples = Render();

        // 4) 播放：使用与设备 mix format 一致的 WaveFormat
        var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(MasterSampleRate, channels);

        var waveProvider = new BufferedWaveProvider(waveFormat)
        {
            BufferLength = samples.Count * sizeof(float),
            DiscardOnBufferOverflow = true
        };

        // copy floats -> bytes
        var floatArr = samples.ToArray();
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

    private static List<float> Render()
    {
        int sampleRate = MasterSampleRate; // 采样率（设备/全局 master）
        double bpm = DataStateService.Bpm; // BPM
        int beatsPerBar = 4;               // 固定 4/4 拍
        int channels = 2;                  // 目标输出通道数（交错 stereo）

        var tracks = DataStateService.Tracks;
        var trackBuffers = new List<List<float>>();
        int maxLength = 0;

        foreach (var track in tracks)
        {
            var currentTrack = new List<float>();

            foreach (var clip in track.Clips)
            {
                if (clip.CachedWaveform == null) continue;

                var sss = clip.CachedWaveform; // TODO: 假定为 L R L R (idk

                // ===== 计算 offset 和 duration（单声道样本长度） =====
                int durationSamplesMono = (int)Math.Round(((clip.Duration * 60.0) / bpm) * sampleRate) * beatsPerBar;
                int offsetSamplesMono = (int)Math.Round(((clip.Offset * 60.0) / bpm) * sampleRate);

                // 将 mono-sample 单位转换为 interleaved 数组索引（unit：sample elements）（可能是采样点吧
                long offsetIndex = (long)offsetSamplesMono * channels;
                long durationCount = (long)durationSamplesMono * channels;

                if (offsetIndex >= sss.Count)
                    continue; // 起点已经超出，321跳！

                long endIndex = Math.Min(offsetIndex + durationCount, sss.Count);

                int copyLen = (int)(endIndex - offsetIndex);
                if (copyLen <= 0) continue;

                // 把交错立体声片段切出来（交错格式）
                var clipSamplesInterleaved = sss.GetRange((int)offsetIndex, copyLen);

                // ===== 计算 clip 在整轨中的起始位置（旧的逻辑，懒得改） =====
                // 这里 startSample 本身已经包含 * beatsPerBar * channels（即 interleaved 单位）
                int startSample =
                    (int)Math.Round(((clip.StartBeat * 60.0) / bpm) * sampleRate)
                    * beatsPerBar * channels;

                // 补齐前导 0（以 interleaved 单位计数）
                if (currentTrack.Count < startSample)
                {
                    int padLen = startSample - currentTrack.Count;
                    currentTrack.AddRange(Enumerable.Repeat(0f, padLen));
                }

                // 直接把交错片段加入轨道（att：已是交错 stereo，不需要再扩展）
                currentTrack.AddRange(clipSamplesInterleaved);
            }

            maxLength = Math.Max(maxLength, currentTrack.Count);
            trackBuffers.Add(currentTrack);
        }

        // ===== 轨道混音（按交错样本逐元素相加） =====
        var finalBuffer = new float[maxLength];
        foreach (var track in trackBuffers)
        {
            for (int i = 0; i < track.Count; i++)
            {
                finalBuffer[i] += track[i];
            }
        }

        // 返回交错立体声样本（float 列表）
        return new List<float>(finalBuffer);
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
                clip.CachedWaveform = DecodeFromFile(clip.Path, targetSampleRate, targetChannels);
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