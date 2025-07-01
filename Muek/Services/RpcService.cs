using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Muek.Commands;

namespace Muek.Services;

public static class RpcService
{
    public const string Host = "http://localhost:50051";
    public static void Init()
    {
    }

    public static async Task SendCommand(IRpcCommand command)
    {
        try
        {
            await command.Execute();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}