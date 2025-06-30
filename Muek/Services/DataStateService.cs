using System.Collections.Generic;
using Muek.Models;

namespace Muek.Services;

public static class DataStateService
{
    public static List<Track> Tracks { get; set; } = [
        new() { Clips = [new Clip { Duration = 10, StartBeat = 2 }] },
        new() { Clips = [new Clip { Duration = 2, StartBeat = 0 }] }
    ];
}