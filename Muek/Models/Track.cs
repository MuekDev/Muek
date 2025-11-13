using System.Collections.Generic;

namespace Muek.Models;

public class Track
{
    public string Color;
    public string Id;
    public string Name;
    public uint Index;
    public List<Clip> Clips = new List<Clip>();
    public List<VstPlugin> Plugins = new List<VstPlugin>();
}