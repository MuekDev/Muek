using System;
using System.Linq;
using System.Threading.Tasks;
using Audio;
using Greet;
using Grpc.Net.Client;
using Muek.Services;
using Muek.Views;

namespace Muek.Commands;

public class PlayCommand : IRpcCommand
{
    public Task Execute()
    {
        Console.WriteLine("[PlayCommand] Execute");
        var window = new ProcessingWindow();
        window.Show();

        using var channel = GrpcChannel.ForAddress(RpcService.Host);

        var client = new AudioProxyProto.AudioProxyProtoClient(channel);

        var request = new PlayRequest
        {
            Tracks = { DataStateService.Tracks.Select(t => t.Proto) }
        };

        var reply = client.Play(request);

        Console.WriteLine($"RESPONSE: {reply}");

        window.Close();

        DataStateService.IsPlaying = true;

        return Task.CompletedTask;
    }
}

public class SyncCommand : IRpcCommand
{
    public Task Execute()
    {
        using var channel = GrpcChannel.ForAddress(RpcService.Host);

        var client = new AudioProxyProto.AudioProxyProtoClient(channel);

        var request = new Empty();

        var reply = client.GetPlayheadPos(request);

        if (reply != null)
        {
            Console.WriteLine(reply.Time);
        }

        return Task.CompletedTask;
    }
}

public class StopCommand : IRpcCommand
{
    public Task Execute()
    {
        Console.WriteLine("[StopCommand] Execute");

        using var channel = GrpcChannel.ForAddress(RpcService.Host);

        var client = new AudioProxyProto.AudioProxyProtoClient(channel);

        var reply = client.Stop(new Empty());

        Console.WriteLine($"RESPONSE: {reply}");

        DataStateService.IsPlaying = false;

        return Task.CompletedTask;
    }
}

// public class UpdateTrackCommand : IRpcCommand
// {
//     public Task ExecuteX(Track track)
//     {
//         Console.WriteLine("[UpdateTrackCommand] Execute");
//         using var channel = GrpcChannel.ForAddress(RpcService.Host);
//         var client = new AudioProxyProto.AudioProxyProtoClient(channel);
//         var reply = client.UpdateTrack(track);
//         Console.WriteLine($"RESPONSE: {reply}");
//         DataStateService.IsPlaying = false;
//         return Task.CompletedTask;
//     }
//
//     public Task Execute()
//     {
//         throw new NotImplementedException();
//     }
// }

public static class HandleNewClipCommand
{
    public static void Execute(Track track, Clip clip)
    {
        Console.WriteLine("[HandleNewClip] Execute");
        using var channel = GrpcChannel.ForAddress(RpcService.Host);
        var client = new AudioProxyProto.AudioProxyProtoClient(channel);
        var reply = client.HandleNewAudioClip(new NewAudioClipRequest()
        {
            Clip = clip,
            Track = track
        });
        Console.WriteLine($"RESPONSE: {reply}");
        DataStateService.IsPlaying = false;
    }
}