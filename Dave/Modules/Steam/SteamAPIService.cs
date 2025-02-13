using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SteamWebAPI2.Utilities;
using SteamWebAPI2.Interfaces;
using Steam.Models.SteamCommunity;
using Steam.Models.GameServers;
using Dave.Modules.Model;

namespace Dave.Modules.Steam
{
    public class SteamAPIService
    {
        private bool m_IsInitialized = false;
        private readonly SteamWebInterfaceFactory m_SteamWebInterfaceFactory;
        private readonly string m_SteamApiKey = "hello";
        private readonly ulong m_SteamUserId; // Your Steam64 ID

        /// <summary>
        /// Constructor for initializing the Steamworks API.
        /// </summary>
        public SteamAPIService(string steamApiKey, ulong steamUserId)
        {
            m_SteamApiKey = steamApiKey ?? throw new ArgumentNullException(nameof(steamApiKey));
            m_SteamUserId = steamUserId;

            m_SteamWebInterfaceFactory = new SteamWebInterfaceFactory(m_SteamApiKey);
        }

        /// <summary>
        /// Initializes the Steam API.
        /// </summary>
        /// <returns>True if initialization succeeds; otherwise, false.</returns>
        public bool Init()
        {
            m_IsInitialized = true;
            return true;
        }

        /// <summary>
        /// Asynchronously fetches the list of owned games and their playtime.
        /// </summary>
        /// <returns>A task representing the asynchronous operation that returns a list of owned games with playtime.</returns>
        public async Task<List<SteamGameData>> FetchOwnedGamesAsync()
        {
            if (!m_IsInitialized)
                throw new InvalidOperationException("Steam API is not initialized.");

            try
            {
                var steamUserInterface = m_SteamWebInterfaceFactory.CreateSteamWebInterface<PlayerService>();
                var ownedGamesResponse = await steamUserInterface.GetOwnedGamesAsync(m_SteamUserId, includeAppInfo: true, includeFreeGames: false);

                if (ownedGamesResponse?.Data == null)
                {
                    return new List<SteamGameData>();
                }

                return ownedGamesResponse.Data.OwnedGames.Select(game => new SteamGameData
                {
                    AppId = (uint)game.AppId,
                    Name = game.Name,
                    ExecutablePath = $"steam://rungameid/{game.AppId}",
                    Playtime = game.PlaytimeForever.TotalHours
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to fetch owned games: {ex.Message}");
                return new List<SteamGameData>();
            }
        }
        public async Task<List<SteamAchievementData>> FetchAchievementsAsync(uint appId)
        {
            if (!m_IsInitialized)
                throw new InvalidOperationException("Steam API is not initialized.");

            try
            {
                var steamUserStatsInterface = m_SteamWebInterfaceFactory.CreateSteamWebInterface<SteamUserStats>();
                // Correct parameter order: steamId first, appId second
                var achievementsResponse = await steamUserStatsInterface.GetPlayerAchievementsAsync(appId, m_SteamUserId);

                return achievementsResponse.Data.Achievements.Select(a => new SteamAchievementData
                {
                    ApiName = a.APIName,
                    Name = a.Name,
                    Description = a.Description,
                    IsAchieved = a.Achieved == 1, // uint to bool
                    UnlockTime = a.UnlockTime
                }).ToList() ?? new List<SteamAchievementData>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to fetch achievements for app {appId}: {ex}");
                return new List<SteamAchievementData>();
            }
        }

        /// <summary>
        /// Launches a Steam game using the steam:// protocol.
        /// </summary>
        /// <param name="game">The game to launch. Its Id should contain the Steam AppId.</param>
        public void LaunchGame(Game game)
        {
            if (game.Equals(null) || game.ID == "0")
                throw new ArgumentException("Invalid game data.");

            System.Diagnostics.Process.Start($"steam://rungameid/{game.ID}");
        }
    }

    /// <summary>
    /// Represents the data for a Steam game.
    /// </summary>
    public class SteamGameData
    {
        public uint AppId { get; set; }
        public string Name { get; set; }
        public string ExecutablePath { get; set; }
        public double Playtime { get; set; } // Playtime in minutes.
    }

    public class SteamAchievementData
    {
        public string ApiName { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsAchieved { get; set; }
        public DateTime? UnlockTime { get; set; }
    }
}
