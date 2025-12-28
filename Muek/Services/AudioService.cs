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
    public static event EventHandler? AudioStarted;
    public static event EventHandler? AudioStopped;
    public static float CurrentDb { get; set; }
    #pragma warning disable CS0067
    public static event EventHandler<float>? DbChanged;
    public static event EventHandler<float[]>? RmsDbChanged;
    public static event EventHandler<float[]>? PeakDbChanged;
    #pragma warning restore CS0067
    public static float[] CurrentRmsDb { get; set; } = [];
    public static float[] CurrentPeakDb { get; set; } = [];
    public static float PlayPosition = 0;

    public static void TriggerAudioStarted()
    {
        AudioStarted?.Invoke(null, EventArgs.Empty);
    }

    public static void TriggerAudioStopped()
    {
        AudioStopped?.Invoke(null, EventArgs.Empty);
    }
}