using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Muek.Helpers;

public class VstHelper
{
    public static void ScanDir(string path)
    {
        var dir = new DirectoryInfo(path);

        foreach (var file in dir.GetFiles("*.dll", SearchOption.AllDirectories))
        {
            Console.WriteLine(file.FullName);
        }
    }
    
    public static async IAsyncEnumerable<FileInfo> ScanDirAsync(string folder)
    {
        await Task.Yield();
        foreach (var path in Directory.EnumerateFiles(folder, "*.dll", SearchOption.AllDirectories)
                     // .Union(Directory.EnumerateFiles(folder, "*.vst3", SearchOption.AllDirectories))
                 )
        {
            yield return new FileInfo(path);
        }
    }
}