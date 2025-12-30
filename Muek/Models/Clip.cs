namespace Muek.Models;

public class Clip
{
    private double _duration;

    public double Duration
    {
        get => _duration;
        set
        {
            value = double.Max(value, 0.1);
            _duration = value;
        }
    }
    public double StartBeat;
    public double Offset;
    public string? Path;
    public string? Name;
    public required string Id;
    public float[]? CachedWaveform { get; set; }
}