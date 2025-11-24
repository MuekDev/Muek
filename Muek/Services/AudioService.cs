using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using RingBuffer; 

namespace Muek.Services;

public static class AudioService
{
    private static WasapiOut? _wasapiOut;
    private static CancellationTokenSource? _playbackCts;
    private static int _cachedSampleRate = 0;

    private static RingBuffer<float>? _ringBuffer;
    private static long _currentMixSamplePosition = 0; // 当前混音到了第几个样本
    private static long _totalSongSamples = 0;         // 歌曲总长度

    private const int RingBufferCapacity = 176400;

    private static float _currentRmsDb = -160f;
    private static float _currentPeakDb = -160f;

    public static float CurrentRmsDb
    {
        get => _currentRmsDb;
        private set
        {
            _currentRmsDb = value;
            RmsDbChanged?.Invoke(null, value);
        }
    }

    public static float CurrentPeakDb
    {
        get => _currentPeakDb;
        private set
        {
            _currentPeakDb = value;
            PeakDbChanged?.Invoke(null, value);
        }
    }

    public static event EventHandler<float>? RmsDbChanged;
    public static event EventHandler<float>? PeakDbChanged;

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

        Stop(); 
        
        CalculateTotalDurationSamples();
        _currentMixSamplePosition = 0;

        _ringBuffer = new RingBuffer<float>(RingBufferCapacity);

        _playbackCts = new CancellationTokenSource();

        var producerThread = new Thread(ProducerLoop)
        {
            Priority = ThreadPriority.Highest, // 关键：设为最高优先级
            IsBackground = true,
            Name = "AudioMixerThread"
        };
        producerThread.Start(_playbackCts.Token);

        Console.WriteLine("正在预缓冲...");
        int retry = 0;
        while (_ringBuffer.Size < _ringBuffer.Capacity / 2 && retry < 100)
        {
            Thread.Sleep(10);
            retry++;
        }

        var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(MasterSampleRate, Channels);
        var provider = new RingBufferSampleProvider(_ringBuffer, waveFormat);
        provider.OnRmsCalculated += db => CurrentRmsDb = db;
        provider.OnPeakCalculated += db => CurrentPeakDb = db;

        _wasapiOut = new WasapiOut(device, AudioClientShareMode.Shared, false, 100); // 建议 latency 改为 100-200 比较稳
        _wasapiOut.Init(provider);
        _wasapiOut.Play();

        Task.Run(() => UpdatePlayheadLoop(_wasapiOut, MasterSampleRate, _playbackCts.Token));

        Console.WriteLine($"实时流播放中... Rate: {MasterSampleRate}");
    }

    public static void Stop()
    {
        _playbackCts?.Cancel();
        
        Thread.Sleep(50); 
        
        _wasapiOut?.Stop();
        _wasapiOut?.Dispose();
        _wasapiOut = null;
        
        _ringBuffer = null;
        CurrentRmsDb = -160f;
    }

    private static void ProducerLoop(object? obj)
    {
        var token = (CancellationToken)obj!;
        int processChunkSize = 1024; 
        float[] mixChunk = new float[processChunkSize];

        while (!token.IsCancellationRequested)
        {
            if (_ringBuffer == null) break;

            int availableSpace = _ringBuffer.Capacity - _ringBuffer.Size;

            if (availableSpace > 0)
            {
                if (_currentMixSamplePosition >= _totalSongSamples)
                {
                    try { _ringBuffer.Put(0); } catch { } 
                    Thread.Sleep(10);
                    continue;
                }

                MixNextBlock(mixChunk, _currentMixSamplePosition);
                
                foreach (var sample in mixChunk)
                {
                    try 
                    {
                        _ringBuffer.Put(sample); 
                    }
                    catch (InvalidOperationException) 
                    {
                        // 防止 buffer 突然满了抛出异常
                        break; 
                    }
                }

                _currentMixSamplePosition += processChunkSize;
                
                // 【关键】：这里不要 Sleep！
                // 循环会立刻回到开头检查 availableSpace。
                // 如果空间还很大，会继续填下一个块，直到把 Buffer 填满。
            }
            else
            {
                // 只有当 Buffer 确实满了（塞不进去了），才休息一小会儿
                // 1ms 足够了，让出 CPU 给消费者
                Thread.Sleep(1); 
            }
        }
    }

    private static void MixNextBlock(float[] chunkBuffer, long startSampleIndex)
    {
        Array.Clear(chunkBuffer, 0, chunkBuffer.Length);

        var tracks = DataStateService.Tracks;
        double bpm = DataStateService.Bpm;
        int sampleRate = MasterSampleRate;
        int chunkLen = chunkBuffer.Length;

        foreach (var track in tracks)
        {
            foreach (var clip in track.Clips)
            {
                if (clip.CachedWaveform is not { Length: > 0 }) continue;
                var sourceData = clip.CachedWaveform;

                double startSeconds = clip.StartBeat / bpm * 60.0;
                long clipGlobalStartIdx = (long)(startSeconds * sampleRate) * BeatsPerBar * Channels;
                
                clipGlobalStartIdx -= clipGlobalStartIdx % 2;

                double durationSeconds = clip.Duration / bpm * 60.0;
                long clipTotalLen = (long)(durationSeconds * sampleRate) * BeatsPerBar * Channels;
                clipTotalLen -= clipTotalLen % 2;

                long clipGlobalEndIdx = clipGlobalStartIdx + clipTotalLen;

                if (clipGlobalStartIdx >= startSampleIndex + chunkLen || clipGlobalEndIdx <= startSampleIndex)
                    continue;

                long writeStartInChunk = Math.Max(0, clipGlobalStartIdx - startSampleIndex);
                long writeEndInChunk = Math.Min(chunkLen, clipGlobalEndIdx - startSampleIndex);

                long sourceOffsetIdx = (long)(clip.Offset * sampleRate) * Channels;
                sourceOffsetIdx -= sourceOffsetIdx % 2;

                long samplesIntoClip = (startSampleIndex + writeStartInChunk) - clipGlobalStartIdx;
                
                // 真正的读取指针
                long readPtr = sourceOffsetIdx + samplesIntoClip;

                // 循环拷贝并累加
                for (long i = writeStartInChunk; i < writeEndInChunk; i++)
                {
                    if (readPtr < sourceData.Length)
                    {
                        chunkBuffer[i] += sourceData[readPtr];
                        readPtr++;
                    }
                }
            }
        }
    }

    private static void CalculateTotalDurationSamples()
    {
        double maxDurationBeats = 0;
        foreach (var track in DataStateService.Tracks)
        {
            foreach (var clip in track.Clips)
            {
                double endBeat = clip.StartBeat + clip.Duration;
                if (endBeat > maxDurationBeats) maxDurationBeats = endBeat;
            }
        }
        _totalSongSamples = (long)(maxDurationBeats / DataStateService.Bpm * 60.0 * MasterSampleRate) * BeatsPerBar * Channels;
        // 加一点缓冲尾部
        _totalSongSamples += 44100 * Channels; 
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
        CurrentRmsDb = -160f;
    }

    private static void ClearAllCache()
    {
        foreach (var track in DataStateService.Tracks)
        {
            foreach (var clip in track.Clips) clip.CachedWaveform = null;
        }
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
                    try { clip.CachedWaveform = LoadAndResample(clip.Path, targetRate); }
                    catch { /* log */ }
                }
            }
        }
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
}