using Dave.Modules.Abstract;
using Dave.Modules.Model;
using static Dave.Logger.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetEnv;
using SteamWebAPI2.Models.SteamStore;

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
            var games = new List<Game>();
            // Map steam-specific data to your Game model
            foreach (var steamGame in steamGames)
            {
                var game = new Game
                {
                    ID = steamGame.AppId.ToString(),
                    Name = steamGame.Name,
                    ExecutablePath = steamGame.ExecutablePath,
                    Playtime = steamGame.Playtime,
                    IconUrl = steamGame.IconUrl
                };

                // Fetch achievements for each game
                var achievements = await m_SteamService.FetchAchievementsAsync(steamGame.AppId);
                game.Achievements = achievements.Select(a => new Model.Achievement
                {
                    Id = a.ApiName,
                    Name = a.Name,
                    Description = a.Description,
                    Unlocked = a.IsAchieved,
                    UnlockDate = a.UnlockTime
                }).ToList();

                games.Add(game);
            }

            return games;
        }
        public async Task<List<Friend>> GetFriendsAsync()
        {
            var steamFriends = await m_SteamService.FetchFriendsAsync();
            
            return steamFriends.Select(f => new Friend
            {
                SteamId = f.SteamId.ToString(),
                Username = f.Username,
                AvatarUrl = f.AvatarUrl,
                ProfileUrl = f.ProfileUrl
            }).ToList();
        }

        public void LaunchGame(Game game)
        {
            m_SteamService.LaunchGame(game);
        }
    }
}
