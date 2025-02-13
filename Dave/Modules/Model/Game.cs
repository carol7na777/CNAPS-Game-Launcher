using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamWebAPI2.Models.SteamStore;

namespace Dave.Modules.Model
{
    public class Game
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string ID { get; set; }
        public string ExecutablePath { get; set; }
        public double Playtime { get; set; }
        public string IconUrl { get; set; }
        public List<Friend> Friends { get; set; } = new();
        public List<Achievement> Achievements { get; set; } = new();
        //Expand for Icon, Achievements, etc.
    }

    public class Achievement
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool Unlocked { get; set; }
        public DateTime? UnlockDate { get; set; }
    }
    public class Friend
    {
        public string SteamId { get; set; }
        public string Username { get; set; }
        public string AvatarUrl { get; set; }
        public string ProfileUrl { get; set; }
    }
}
