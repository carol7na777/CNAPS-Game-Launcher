using Avalonia;
using Dave.ViewModels;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Runtime.InteropServices;
using Dave.Modules.Abstract;
using Dave.Modules.Model;
using Dave.Modules.Steam;
using Dave.Modules;
using System.Collections.Generic;
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
        OpenConsole();  // Call this to open the console
        InitLog();
        // Create a list of game launcher modules. For now, only SteamModule is available.
        List<IGameLauncherModule> modules = new List<IGameLauncherModule>
            {
                new SteamModule()
                // Future modules like new GOGModule() can be added here.
            };

        // Instantiate the GameManager with the modules.
        GameManager gameManager = new GameManager(modules);

        Task.Run(async () =>
        {
            try
            {
                // Fetch the aggregated list of games from all modules.
                List<Game> allGames = await gameManager.GetAllGamesAsync();

                // Display the list of games.
                Console.WriteLine("Owned Games:");
                var sortedGames = allGames.OrderByDescending(g => g.Playtime).ToList();
                Console.WriteLine("{0,-10} {1,-30} {2,15}", "ID", "Name", "Playtime (h)");
                Console.WriteLine(new string('-', 60));

                foreach (var game in sortedGames)
                {
                    Console.WriteLine("{0,-10} {1,-30} {2,15:F1}", game.ID, game.Name, game.Playtime);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        });

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
