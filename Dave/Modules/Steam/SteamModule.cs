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
using Dave.Utility;
using Dave.Caching;

namespace Dave.Modules.Steam
{
    public class SteamModule : IGameLauncherModule
    {
        private readonly SteamAPIService m_SteamService;
        private readonly string m_ApiKey;
        private readonly CacheManager m_CacheManager = new();

        public SteamModule(ulong steamId)
        {
            Env.Load();
            m_ApiKey = Environment.GetEnvironmentVariable("STEAM_API_KEY");

            if (string.IsNullOrEmpty(m_ApiKey))
            {
                Console.WriteLine("STEAM_API_KEY is not set! Please set a Steam Web API key.");
                m_ApiKey = "Invalid"; // Oder eine Fehlermeldung werfen
            }

            Warning($"Steam API Key: {m_ApiKey}");
            m_SteamService = new SteamAPIService(m_ApiKey, steamId);
        }

        public void Initialize() { }

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
            // Try to get game details from the cache first
            var cachedDetails = m_CacheManager.GetGameStoreDetails(game.ID);
            if (cachedDetails != null)
            {
                Info($"[Cache] Returning cached details for game {game.ID}");
                return cachedDetails; // Return cached result if valid
            }

            // If not cached, fetch from the API
            Info($"[Cache] Fetching details for game {game.ID} from Steam...");
            var details = await m_SteamService.FetchStoreInfo(game.ID);

            // Prepare the details object to return
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
                Banner = details.Banner,
            };

            // Cache the result for future use (e.g., 24 hours)
            m_CacheManager.AddOrUpdateGameStoreDetails(game.ID, appDetails, TimeSpan.FromHours(24));

            return appDetails;
        }
        public async Task<List<Model.Achievement>> GetAchievementsForGameAsync(Game game)
        {
            // Try to get achievements from the cache first
            var cachedAchievements = m_CacheManager.GetAchievements(game.ID);
            if (cachedAchievements != null)
            {
                Logger.Logger.Info($"[Cache] Returning cached achievements for game {game.ID}");
                return cachedAchievements; // Return cached result if valid
            }

            // If not cached, fetch from the API
            Logger.Logger.Info($"[Cache] Fetching achievements for game {game.ID} from Steam...");
            var achievements = await m_SteamService.FetchAchievementsAsync(game.ID);

            // Convert achievements to the desired model format
            var achievementsList = achievements.Select(a => new Model.Achievement
            {
                Id = a.ApiName,
                Name = a.Name,
                Description = a.Description,
                Unlocked = a.IsAchieved,
                UnlockDate = a.UnlockTime
            }).ToList();

            // Cache the achievements for future use (e.g., 24 hours)
            m_CacheManager.AddOrUpdateAchievements(game.ID, achievementsList, TimeSpan.FromHours(24));

            return achievementsList;
        }


        public async Task<List<Friend>> GetFriendsAsync()
        {
            var steamFriends = await m_SteamService.FetchFriendsAsync();
            var friendList = steamFriends.Select(f => new Friend
            {
                SteamId = f.SteamId,
                Username = f.Username,
                AvatarUrl = f.AvatarUrl,
                ProfileUrl = f.ProfileUrl,
                UserStatus = f.UserStatus
            }).ToList();

            return friendList;
        }

        public void LaunchGame(Game game)
        {
            m_SteamService.LaunchGame(game);
        }
    }
}
