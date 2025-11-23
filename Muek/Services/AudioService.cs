using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Muek.Helpers;
using Muek.ViewModels;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Muek.Services;

public static class AudioService
{
    private static WasapiOut? _wasapiOut;
    private static CancellationTokenSource? _playbackCts;
    private static int _cachedSampleRate = 0;

    public static int MasterSampleRate { get; private set; } = 44100;
    private const int Channels = 2;
    private static int BeatsPerBar => DataStateService.Subdivisions;

    public static void Play()
    {
        using var enumerator = new MMDeviceEnumerator();
        using var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
        MasterSampleRate = device.AudioClient.MixFormat.SampleRate;

        if (_cachedSampleRate != MasterSampleRate)
        {
            ClearAllCache();
            _cachedSampleRate = MasterSampleRate;
        }

        EnsureAllClipsCached(MasterSampleRate);

        var mixedSamples = RenderMix(MasterSampleRate);
        if (mixedSamples.Length == 0) return;

        var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(MasterSampleRate, Channels);
        var waveProvider = new BufferedWaveProvider(waveFormat)
        {
            BufferLength = mixedSamples.Length * sizeof(float),
            DiscardOnBufferOverflow = true
        };

        var byteBuffer = new byte[mixedSamples.Length * sizeof(float)];
        Buffer.BlockCopy(mixedSamples, 0, byteBuffer, 0, byteBuffer.Length);
        waveProvider.AddSamples(byteBuffer, 0, byteBuffer.Length);

        Stop();
        _wasapiOut = new WasapiOut(device, AudioClientShareMode.Shared, false, 100);
        _wasapiOut.Init(waveProvider);
        _wasapiOut.Play();

        _playbackCts = new CancellationTokenSource();
        Task.Run(() => UpdatePlayheadLoop(_wasapiOut, MasterSampleRate, _playbackCts.Token));

        Console.WriteLine($"播放中... Rate: {MasterSampleRate}");
    }

    public static void Stop()
    {
        _playbackCts?.Cancel();
        _wasapiOut?.Stop();
        _wasapiOut?.Dispose();
        _wasapiOut = null;
    }

    private static void ClearAllCache()
    {
        foreach (var track in DataStateService.Tracks)
        {
            foreach (var clip in track.Clips) clip.CachedWaveform = null;
        }
    }

    private static float[] RenderMix(int sampleRate)
    {
        var tracks = DataStateService.Tracks;
        double bpm = DataStateService.Bpm;

        double maxDurationBeats = 0;
        foreach (var track in tracks)
        {
            foreach (var clip in track.Clips)
            {
                double endBeat = clip.StartBeat + clip.Duration;
                if (endBeat > maxDurationBeats) maxDurationBeats = endBeat;
            }
        }

        long totalOutputSamples = (long)(maxDurationBeats / bpm * 60.0 * sampleRate) * BeatsPerBar * Channels;

        if (totalOutputSamples <= 0) return Array.Empty<float>();

        var mixBuffer = new float[totalOutputSamples];

        foreach (var track in tracks)
        {
            foreach (var clip in track.Clips)
            {
                if (clip.CachedWaveform is not { Length: > 0 }) continue;
                var sourceData = clip.CachedWaveform;

                double startSeconds = clip.StartBeat / bpm * 60.0;
                long globalStartIndex = (long)(startSeconds * sampleRate) * BeatsPerBar * Channels;

                double durationSeconds = clip.Duration / bpm * 60.0;
                long needSamples = (long)(durationSeconds * sampleRate) * BeatsPerBar * Channels;

                long sourceOffsetIdx = (long)(clip.Offset * sampleRate) * Channels;

                // 对齐偶数
                globalStartIndex -= globalStartIndex % 2;
                sourceOffsetIdx -= sourceOffsetIdx % 2;
                needSamples -= needSamples % 2;

                if (sourceOffsetIdx >= sourceData.Length) continue;

                long copyLength = Math.Min(needSamples, sourceData.Length - sourceOffsetIdx);

                if (globalStartIndex + copyLength > mixBuffer.Length)
                    copyLength = mixBuffer.Length - globalStartIndex;

                if (copyLength <= 0) continue;

                for (int i = 0; i < copyLength; i++)
                {
                    mixBuffer[globalStartIndex + i] += sourceData[sourceOffsetIdx + i];
                }
            }
        }

        return mixBuffer;
    }

    private static float[] LoadAndResample(string path, int targetRate)
    {
        if (!File.Exists(path)) throw new FileNotFoundException(path);
        using var reader = new AudioFileReader(path);
        ISampleProvider provider = reader;

        if (reader.WaveFormat.Channels == 1)
            provider = new MonoToStereoSampleProvider(provider);

        if (provider.WaveFormat.SampleRate != targetRate)
            provider = new WdlResamplingSampleProvider(provider, targetRate);

        long estimatedSamples = (long)(reader.TotalTime.TotalSeconds * targetRate * Channels);
        var data = new List<float>((int)estimatedSamples + 8192);

        var buffer = new float[16384];
        int read;
        while ((read = provider.Read(buffer, 0, buffer.Length)) > 0)
        {
            for (int i = 0; i < read; i++) data.Add(buffer[i]);
        }

        return data.ToArray();
    }

    private static void EnsureAllClipsCached(int targetRate)
    {
        foreach (var track in DataStateService.Tracks)
        {
            foreach (var clip in track.Clips)
            {
                if (string.IsNullOrEmpty(clip.Path)) continue;
                if (clip.CachedWaveform == null || clip.CachedWaveform.Length == 0)
                {
                    try
                    {
                        clip.CachedWaveform = LoadAndResample(clip.Path, targetRate);
                    }
                    catch
                    {
                        Console.WriteLine("[AudioService] cached wave panic!");
                    }
                }
            }
        }
    }

    private static void UpdatePlayheadLoop(WasapiOut output, int sampleRate, CancellationToken token)
    {
        double bpm = DataStateService.Bpm;
        int bytesPerFrame = sizeof(float) * Channels;

        while (output != null && output.PlaybackState == PlaybackState.Playing && !token.IsCancellationRequested)
        {
            long bytesPlayed = output.GetPosition();
            long framesPlayed = bytesPlayed / bytesPerFrame;

            double currentSec = (double)framesPlayed / sampleRate / BeatsPerBar;

            double currentBeats = currentSec / (60.0 / bpm);

            UiStateService.InvokeUpdatePlayheadPos(currentBeats);
            Thread.Sleep(16);
        }
    }
}