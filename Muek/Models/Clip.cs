using System.Collections.Generic;

namespace Muek.Models;

public class Clip
{
    public double Duration;
    public double StartBeat;
    public double Offset;
    public string? Path;
    public string? Name;
    public required string Id;
    public float[]? CachedWaveform { get; set; }
}