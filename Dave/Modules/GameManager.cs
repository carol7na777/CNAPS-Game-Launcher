using Dave.Logger;
using Dave.Modules.Abstract;
using Dave.Modules.Model;
using Dave.Modules.Steam;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Dave.Modules
{
    public class GameManager
    {
        private readonly List<IGameLauncherModule> m_Modules;

        public GameManager(IEnumerable<IGameLauncherModule> modules)
        {
            m_Modules = modules.ToList();
            foreach (var module in m_Modules)
            {
                module.Initialize();
            }
        }

        public async Task<List<Game>> GetAllGamesAsync()
        {
            var allGames = await Task.WhenAll(m_Modules.Select(module => module.GetGamesAsync()));
            var listGames = allGames.SelectMany(games => games).ToList();
            Logger.Logger.Info("All Games finished loading");
            return listGames;
        }
        public async Task<List<Friend>> GetAllFriendsAsync()
        {
            var allFriends = await Task.WhenAll(m_Modules.Select(module => module.GetFriendsAsync()));
            return allFriends.SelectMany(friends => friends).ToList();
        }

        public async Task<List<Model.Achievement>> GetAchievementsForGame(Game game)
        {
            var achievements = await Task.WhenAll(m_Modules.Select(module => module.GetAchievementsForGameAsync(game)));
            var achievementsList = achievements.SelectMany(achievements => achievements).ToList();
            return achievementsList;
        }

        public async Task<StoreDetails> GetGameStoreDetailsAsync(Game game)
        {
            var storeDetails = await Task.WhenAll(m_Modules.Select(module => module.GetGameStoreDetailsAsync(game)));
            // TODO: Fix this being index 0 when we add GOG or Epic
            return storeDetails[0];
        }

        public void LaunchGame(Game game)
        {
            foreach (var module in m_Modules)
            {
                if (module is SteamModule)
                    module.LaunchGame(game);
            }
        }
    }
}
