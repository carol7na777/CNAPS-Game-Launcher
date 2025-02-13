using Avalonia;
using Dave.ViewModels;
using System;
using System.Runtime.InteropServices;
using static Dave.Logger.Logger;

namespace Dave;

class Program
{

    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll")]
    private static extern bool FreeConsole();

    private static void OpenConsole()
    {
        // Open a new console window
        if (AllocConsole())
        {
            Console.WriteLine("Console opened! You can write here.");
        }
    }

    private static void CloseConsole()
    {
        // Close the console window
        FreeConsole();
    }
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {

        // Keep the console window open.
        OpenConsole();
        InitLog();
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
