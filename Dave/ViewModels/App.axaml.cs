using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Dave.Modules.Abstract;
using Dave.Modules.Model;
using Dave.Modules.Steam;
using Dave.Modules;
using System.Collections.Generic;
using System;
using static Dave.Logger.Logger;
using System.Linq;
using System.Threading.Tasks;

namespace Dave.ViewModels;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

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
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}