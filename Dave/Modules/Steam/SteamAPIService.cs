using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SteamWebAPI2.Utilities;
using SteamWebAPI2.Interfaces;
using Dave.Modules.Model;
using Steam.Models.SteamCommunity;

namespace Dave.Modules.Steam
{
    public class SteamAPIService
    {
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
        /// Asynchronously fetches the list of owned games and their playtime.
        /// </summary>
        /// <returns>A task representing the asynchronous operation that returns a list of owned games with playtime.</returns>
        public async Task<List<SteamGameData>> FetchOwnedGamesAsync()
        {
            try
            {
                var steamUserInterface = m_SteamWebInterfaceFactory.CreateSteamWebInterface<PlayerService>();
                var ownedGamesResponse = await steamUserInterface.GetOwnedGamesAsync(m_SteamUserId, includeAppInfo: true, includeFreeGames: false);

                if (ownedGamesResponse?.Data == null)
                {
                    return [];
                }

                return ownedGamesResponse.Data.OwnedGames.Select(game => new SteamGameData
                {
                    AppId = (uint)game.AppId,
                    Name = game.Name,
                    ExecutablePath = $"steam://rungameid/{game.AppId}",
                    Playtime = game.PlaytimeForever.TotalHours,
                    IconUrl = game.ImgIconUrl
                }).ToList();
            }
            catch (Exception ex)
            {
                Logger.Logger.Error($"Failed to fetch owned games: {ex.Message}");
                return [];
            }
        }

        public async Task<SteamStoreDetails> FetchStoreInfo(uint appId)
        {
            try
            {
                var steamStoreInterface = m_SteamWebInterfaceFactory.CreateSteamStoreInterface();
                var appDetailsResponse = await steamStoreInterface.GetStoreAppDetailsAsync(appId, language: "english");
                var storeDetails = new SteamStoreDetails
                {
                    Id = appDetailsResponse.SteamAppId,
                    RequiredAge = appDetailsResponse.RequiredAge,
                    ControllerSupport = appDetailsResponse.ControllerSupport,
                    AboutTheGame = appDetailsResponse.AboutTheGame,
                    ShortDescription = appDetailsResponse.ShortDescription,
                    HeaderImage = appDetailsResponse.HeaderImage,
                    Website = appDetailsResponse.Website,
                    Developers = appDetailsResponse.Developers,
                    Publishers = appDetailsResponse.Publishers,
                    Background = appDetailsResponse.Background,
                    Banner = $"https://cdn.cloudflare.steamstatic.com/steam/apps/{appId}/library_hero.jpg"
                };

                return storeDetails;

            }
            catch (Exception)
            {
                return new SteamStoreDetails { };
            }
        }

        public async Task<List<SteamAchievementData>> FetchAchievementsAsync(uint appId)
        {
            try
            {
                var steamUserStatsInterface = m_SteamWebInterfaceFactory.CreateSteamWebInterface<SteamUserStats>();
                var achievementsResponse = await steamUserStatsInterface.GetPlayerAchievementsAsync(appId, m_SteamUserId);

                var achievements = achievementsResponse.Data.Achievements.Select(a => new SteamAchievementData
                {
                    ApiName = a.APIName,
                    Name = a.Name,
                    Description = a.Description,
                    IsAchieved = a.Achieved == 1, // uint to bool
                    UnlockTime = a.UnlockTime
                }).ToList();

                Logger.Logger.Info($"Successfully fetched {achievements.Count} achievements for game: {achievementsResponse.Data.GameName}");

                return achievements;
            }
            catch (Exception)
            {
                return [];
            }

        }

        public async Task<List<SteamFriendData>> FetchFriendsAsync()
        {
            try
            {
                // fetch friends list
                var steamFriendsInterface = m_SteamWebInterfaceFactory.CreateSteamWebInterface<SteamUser>();
                var friendsResponse = await steamFriendsInterface.GetFriendsListAsync(m_SteamUserId);

                if (friendsResponse?.Data == null)
                    return [];

                // extract friends steam ids
                var friendSteamIds = friendsResponse.Data.Select(f => f.SteamId).ToList();

                // fetch friends player summaries
                var steamPlayerInterface = m_SteamWebInterfaceFactory.CreateSteamWebInterface<SteamUser>();
                var playerSummariesResponse = await steamPlayerInterface.GetPlayerSummariesAsync(friendSteamIds);

                var friends = playerSummariesResponse.Data.Select(p => new SteamFriendData
                {
                    SteamId = (uint)p.SteamId,
                    Username = p.Nickname,
                    AvatarUrl = p.AvatarFullUrl,
                    ProfileUrl = p.ProfileUrl,
                    UserStatus = p.UserStatus
                }).ToList();

                Logger.Logger.Info($"Successfully fetched {friends.Count} friends for Player: {m_SteamUserId}");
                foreach (var friend in friends)
                    Logger.Logger.Info($"UserStatus {friend.Username}: {friend.UserStatus}");
                return friends;
            }
            catch (Exception ex)
            {
                Logger.Logger.Error($"Failed to fetch friends: {ex.Message}");
                return [];
            }
        }


        /// <summary>
        /// Launches a Steam game using the steam:// protocol.
        /// </summary>
        /// <param name="game">The game to launch. Its Id should contain the Steam AppId.</param>
        public void LaunchGame(Game game)
        {
            if (game.Equals(null) || game.ID == 0)
                throw new ArgumentException("Invalid game data.");

            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = $"steam://rungameid/{game.ID}",
                UseShellExecute = true // This is the key part
            };

            Logger.Logger.Info("Launching Game: '{0}' with ID: '{1}'", game.Name, game.ID.ToString());

            System.Diagnostics.Process.Start(processInfo);
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
        public string IconUrl { get; set; }
    }

    public class SteamAchievementData
    {
        public string ApiName { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsAchieved { get; set; }
        public DateTime? UnlockTime { get; set; }
    }
    public class SteamFriendData
    {
        public uint SteamId { get; set; }
        public string Username { get; set; }
        public string AvatarUrl { get; set; }
        public string ProfileUrl { get; set; }
        public UserStatus UserStatus { get; set; }
    }

    public class SteamStoreDetails
    {
        public uint Id { get; set; }
        public uint RequiredAge { get; set; }
        public string ControllerSupport { get; set; }
        public string AboutTheGame { get; set; }
        public string ShortDescription { get; set; }
        public string HeaderImage { get; set; }
        public string Website { get; set; }
        public string[] Developers { get; set; }
        public string[] Publishers { get; set; }
        public string Background { get; set; }
        public string Banner { get; set; }
    }
}

