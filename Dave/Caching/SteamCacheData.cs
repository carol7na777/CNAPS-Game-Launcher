using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Dave.Caching;
public class SteamCacheData
{
    [JsonPropertyName("user")]
    public ulong User { get; set; }
}