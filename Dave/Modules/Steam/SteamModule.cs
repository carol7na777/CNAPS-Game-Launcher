using Dave.Modules.Abstract;
using Dave.Modules.Model;
using static Dave.Logger.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetEnv;

namespace Dave.Modules.Steam
{
    public class SteamModule : IGameLauncherModule
    {
        private readonly SteamAPIService m_SteamService;
        private readonly string m_ApiKey = "hello";

        public SteamModule()
        {
            Env.Load();
            m_ApiKey = Environment.GetEnvironmentVariable("STEAM_API_KEY");

            if (string.IsNullOrEmpty(m_ApiKey))
            {
                Console.WriteLine("STEAM_API_KEY ist nicht gesetzt! Setze eine Standard-API.");
                m_ApiKey = "DEIN_DEFAULT_API_KEY"; // Oder eine Fehlermeldung werfen
            }

            Console.WriteLine($"Steam API Key: {m_ApiKey}");
            m_SteamService = new SteamAPIService(m_ApiKey, 76561199023121914);
        }


        public void Initialize()
        {
            if (!m_SteamService.Init())
            {
                Error("Steam initialization failed.");
            }
        }


        public async Task<List<Game>> GetGamesAsync()
        {
            var steamGames = await m_SteamService.FetchOwnedGamesAsync();
            // Map steam-specific data to your Game model
            return steamGames.Select(g => new Game
            {
                ID = g.AppId.ToString(),
                Name = g.Name,
                ExecutablePath = g.ExecutablePath,
                Playtime = g.Playtime
            }).ToList();
        }

        public void LaunchGame(Game game)
        {
            m_SteamService.LaunchGame(game);
        }
    }
}
