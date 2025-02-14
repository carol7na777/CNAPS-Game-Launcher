using System;
using System.Collections.Generic;
using Steam.Models.SteamCommunity;

namespace Dave.Modules.Model
{
    public class Game
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public uint ID { get; set; }
        public string ExecutablePath { get; set; }
        public double Playtime { get; set; }
        public string IconUrl { get; set; }
        public List<Friend> Friends { get; set; } = [];
        public List<Achievement> Achievements { get; set; } = [];
    }

    public struct Achievement
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool Unlocked { get; set; }
        public DateTime? UnlockDate { get; set; }
    }
    public class Friend
    {
        public uint SteamId { get; set; }
        public string Username { get; set; }
        public string AvatarUrl { get; set; }
        public string ProfileUrl { get; set; }
        public UserStatus UserStatus { get; set; }
    }

    public class StoreDetails
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
