using System;
using System.Threading;
using System.Threading.Tasks;
using Audio;
using Grpc.Net.Client;
using Muek.Views;

namespace Muek.Services;

public static class TimeSyncService
{
    private static CancellationTokenSource? _cts;

    public static void Start()
    {
        if (_cts != null)
            return; // 避免重复启动

        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        Task.Run(async () =>
        {
            using var channel = GrpcChannel.ForAddress(RpcService.Host);
            var client = new AudioProxyProto.AudioProxyProtoClient(channel);
            var request = new Empty();

            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (DataStateService.IsPlaying)
                    {
                        var reply = await client.GetPlayheadPosAsync(request);
                        Console.WriteLine($"[Sync] Time: {reply.Time}");

                        // UIService.UpdatePlayhead(reply.Time);
                        var beats = reply.Time / 60.0 * DataStateService.Bpm / TrackView.Subdivisions;
                        UiStateService.InvokeUpdatePlayheadPos(beats);
                        Console.WriteLine($"time (s): {reply.Time}, bpm: {DataStateService.Bpm}, beats: {beats}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Sync] Error: {ex.Message}");
                }

                await Task.Delay(33, token); // 30fps
            }
        }, token);
    }

    public static void Stop()
    {
        _cts?.Cancel();
        _cts = null;
    }
}