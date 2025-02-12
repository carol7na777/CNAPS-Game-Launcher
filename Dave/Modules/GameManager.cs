using Dave.Logger;
using Dave.Modules.Abstract;
using Dave.Modules.Model;
using Dave.Modules.Steam;
using System;
using System.Collections.Generic;
using System.Linq;
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
            return allGames.SelectMany(games => games).ToList();
        }
    }
}
