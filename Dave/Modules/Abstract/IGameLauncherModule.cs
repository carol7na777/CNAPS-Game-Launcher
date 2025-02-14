using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dave.Modules.Model;

namespace Dave.Modules.Abstract
{
    public interface IGameLauncherModule
    {
        void Initialize();
        Task<List<Game>> GetGamesAsync();
        Task<List<Friend>> GetFriendsAsync();
        Task<List<Achievement>> GetAchievementsForGameAsync(Game game);
        Task<StoreDetails> GetGameStoreDetailsAsync(Game game);
        void LaunchGame(Game game);
    }
}
