using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Dave.Caching
{
    public class CacheManager
    {
        private readonly ConcurrentDictionary<string, CacheItem<SteamCacheData>> m_Cache = new();
        private const string CacheKey = "Steam"; // Matches the JSON structure root key

        // Adds or updates the cached Steam user data.
        public void AddOrUpdate(ulong steamId, TimeSpan? expiration = null)
        {
            var steamData = new SteamCacheData
            {
                User = steamId,
            };

            var expiryTime = expiration.HasValue ? DateTime.UtcNow.Add(expiration.Value) : DateTime.MaxValue;
            m_Cache[CacheKey] = new CacheItem<SteamCacheData>(steamData, expiryTime);
        }

        // Retrieves the cached Steam user data if it's not expired.
        public SteamCacheData GetSteamUserProfile()
        {
            if (m_Cache.TryGetValue(CacheKey, out var item))
            {
                if (DateTime.UtcNow < item.Expiration)
                {
                    return item.Value;
                }
                else
                {
                    m_Cache.TryRemove(CacheKey, out _);
                }
            }
            return null;
        }

        // Clears the cached Steam user data.
        public void RemoveSteamUserProfile()
        {
            m_Cache.TryRemove(CacheKey, out _);
        }

        // Saves the current cache to disk, formatted exactly like your JSON layout.
        public void SaveCacheToDisk(string filePath)
        {
            if (!m_Cache.TryGetValue(CacheKey, out var item))
            {
                return; // Nothing to save
            }

            string cacheFolder = Path.Combine(AppContext.BaseDirectory, "cache");
            Directory.CreateDirectory(cacheFolder);

            // Format the data into your required structure
            var data = new Dictionary<string, SteamCacheData> { { "Steam", item.Value } };

            var options = new JsonSerializerOptions { WriteIndented = true };
            var serializedCache = JsonSerializer.Serialize(data, options);

            File.WriteAllText(filePath, serializedCache);
        }

        // Loads the cache from disk
        public void LoadCacheFromDisk(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            var fileContent = File.ReadAllText(filePath);
            var data = JsonSerializer.Deserialize<Dictionary<string, SteamCacheData>>(fileContent);

            m_Cache.Clear();

            if (data != null && data.TryGetValue("Steam", out var steamData))
            {
                m_Cache[CacheKey] = new CacheItem<SteamCacheData>(steamData, DateTime.MaxValue);
            }
        }
    }

    internal class CacheItem<T>(T value, DateTime expiration)
    {
        public T Value { get; } = value;
        public DateTime Expiration { get; } = expiration;
    }
}
