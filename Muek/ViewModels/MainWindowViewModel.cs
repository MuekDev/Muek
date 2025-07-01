using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Muek.Commands;
using Muek.Services;

namespace Muek.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public string Greeting { get; } = "Welcome to Avalonia!";

    [RelayCommand]
    public async Task OnPlayButtonClick()
    {
        Console.WriteLine("Omg it is playing...");
        await RpcService.SendCommand(new PlayCommand());
    }

    [RelayCommand]
    public async Task OnStopButtonClick()
    {
        Console.WriteLine("Omg it is stopping...");
        await RpcService.SendCommand(new StopCommand());
    }
}