using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Dave.Caching;
public class SteamCacheData
{
    [JsonPropertyName("user")]
    public ulong User { get; set; }

    [JsonPropertyName("games")]
    public List<SteamGameProfile> Games { get; set; }
}

public class SteamGameProfile
{
    [JsonPropertyName("id")]
    public uint GameId { get; set; }

    [JsonPropertyName("Name")]
    public string GameName { get; set; }

    [JsonPropertyName("status")]
    public string GameStatus { get; set; }

    [JsonPropertyName("ExecutablePath")]
    public string ExecutablePath { get; set; }

    [JsonPropertyName("Playtime")]
    public double PlaytimeMinutes { get; set; }

    [JsonPropertyName("Achievements")]
    public List<SteamAchievementProfile> Achievements { get; set; }
}

public class SteamAchievementProfile
{
    [JsonPropertyName("id")]
    public uint AchievementId { get; set; }

    [JsonPropertyName("name")]
    public string AchievementName { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("unlocked")]
    public bool IsUnlocked { get; set; }

    [JsonPropertyName("unlockdate")]
    public DateTime UnlockDate { get; set; }
}