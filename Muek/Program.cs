using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Avalonia;
using Muek.Engine;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;
using Projektanker.Icons.Avalonia.MaterialDesign;

namespace Muek;

public class MyClass
{
    public int Id;
    public float Value;
}

internal sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        List<MyClass> list = new()
        {
            new MyClass() { Id = 1, Value = 2.0f },
            new MyClass() { Id = 2, Value = 3.0f }
        };

        // 转换为 MyClassRepr[]
        // var reprArray = list
        //     .Select(c => new MyClassRepr { id = c.Id, value = c.Value })
        //     .ToArray();
        //
        // unsafe
        // {
        //     fixed (MyClassRepr* p = reprArray) 
        //     {
        //         MuekEngine.rust_receive_array(p, reprArray.Length);
        //     }
        // }

        Console.WriteLine("Initializing Muek Engine...");
        unsafe
        {
            var b = MuekEngine.alloc_u8_string();
            var str = Encoding.UTF8.GetString(b->AsSpan());
            Console.WriteLine(str);

            // const string msg = @"C:\VST\Valhalla\ValhallaDSP\ValhallaSpaceModulator_x64.dll";
            // fixed (char* p = msg)
            // {
            //     MuekEngine.run_vst_instance_by_path((ushort*)p, msg.Length);
            // }
        }

        MuekEngine.spawn_audio_thread();
        MuekEngine.init_vst_box();

        var a = MuekEngine.my_add(1, 113);
        Debug.Assert(a == 114);
        Console.WriteLine("Init MuekEngine success with code " + a);

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        IconProvider.Current
            .Register<FontAwesomeIconProvider>()
            .Register<MaterialDesignIconProvider>();

        return AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace()
            ;
    }
}