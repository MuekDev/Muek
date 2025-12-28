using System.Collections.Generic;

namespace Muek.Models;

public class Track
{
    public required string Color;
    public required string Id;
    public required string Name;
    public uint Index;
    public List<Clip> Clips = new List<Clip>();
    public List<VstPlugin> Plugins = new List<VstPlugin>();
}