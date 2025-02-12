using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dave.Modules.Model;

namespace Dave.Modules.Abstract
{
    public interface IGameLauncherModule
    {
        void Initialize();
        Task<List<Game>> GetGamesAsync();
        void LaunchGame(Game game);
    }
}
