using System;
using System.Threading.Tasks;
using Greet;
using Grpc.Net.Client;
using Muek.Services;

namespace Muek.Commands;

public class GreetCommand : IRpcCommand
{
    public async Task Execute()
    {
        using var channel = GrpcChannel.ForAddress(RpcService.Host);

        var client = new Greeter.GreeterClient(channel);

        var request = new HelloRequest { Name = "Muek" };

        var reply = await client.SayHelloAsync(request);

        Console.WriteLine($"RESPONSE: {reply.Message}");
    }
}