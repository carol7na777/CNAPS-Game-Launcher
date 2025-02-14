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
        private readonly string m_ApiKey;

        public SteamModule()
        {
            Env.Load();
            m_ApiKey = Environment.GetEnvironmentVariable("STEAM_API_KEY");

            if (string.IsNullOrEmpty(m_ApiKey))
            {
                Console.WriteLine("STEAM_API_KEY is not set! Please set a Steam Web API key.");
                m_ApiKey = "Invalid"; // Oder eine Fehlermeldung werfen
            }

            Logger.Logger.Warning($"Steam API Key: {m_ApiKey}");
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
                    ID = steamGame.AppId,
                    Name = steamGame.Name,
                    ExecutablePath = steamGame.ExecutablePath,
                    Playtime = steamGame.Playtime,
                    IconUrl = steamGame.IconUrl
                };

                games.Add(game);
            }

            return games;
        }

        public async Task<StoreDetails> GetGameStoreDetailsAsync(Game game)
        {
            var details = await m_SteamService.FetchStoreInfo(game.ID);
            var appDetails = new StoreDetails
            {
                Id = details.Id,
                AboutTheGame = details.AboutTheGame,
                ShortDescription = details.ShortDescription,
                RequiredAge = details.RequiredAge,
                ControllerSupport = details.ControllerSupport,
                HeaderImage = details.HeaderImage,
                Website = details.Website,
                Developers = details.Developers,
                Publishers = details.Publishers,
                Background = details.Background,

            };

            return appDetails;
        }

        public async Task<List<Model.Achievement>> GetAchievementsForGameAsync(Game game)
        {
            // Fetch achievements for each game
            var achievements = await m_SteamService.FetchAchievementsAsync(game.ID);
            var achievementsList = achievements.Select(a => new Model.Achievement
            {
                Id = a.ApiName,
                Name = a.Name,
                Description = a.Description,
                Unlocked = a.IsAchieved,
                UnlockDate = a.UnlockTime
            }).ToList();

            return achievementsList;
        }

        public async Task<List<Friend>> GetFriendsAsync()
        {
            var steamFriends = await m_SteamService.FetchFriendsAsync();

            return steamFriends.Select(f => new Friend
            {
                SteamId = f.SteamId,
                Username = f.Username,
                AvatarUrl = f.AvatarUrl,
                ProfileUrl = f.ProfileUrl,
                UserStatus = f.UserStatus
            }).ToList();
        }

        public void LaunchGame(Game game)
        {
            m_SteamService.LaunchGame(game);
        }
    }
}
