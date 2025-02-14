using Dave.Modules.Model;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Dave.Utility
{
    public class ResourceLoader
    {
        public static async Task<string> DownloadFriendIconAsync(Friend friend)
        {
            string cacheFolder = Path.Combine(AppContext.BaseDirectory, "FriendIcons");
            Directory.CreateDirectory(cacheFolder); // Ensure the folder exists

            string localIconPath = Path.Combine(cacheFolder, $"{friend.SteamId}.jpg");

            if (!File.Exists(localIconPath))
            {
                using var httpClient = new HttpClient();
                try
                {
                    byte[] imageData = await httpClient.GetByteArrayAsync(friend.AvatarUrl);
                    await File.WriteAllBytesAsync(localIconPath, imageData);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to download icon for {friend.Username}: {ex.Message}");
                    return "assets/default_icon.png"; // Fallback icon
                }
            }

            return localIconPath;
        }

        public static async Task<string> DownloadGameBannerAsync(StoreDetails details)
        {
            // Cache directory for banners
            string cacheFolder = Path.Combine(AppContext.BaseDirectory, "GameBanners");
            Directory.CreateDirectory(cacheFolder); // Ensure the folder exists

            // Path to save the banner image
            string localBannerPath = Path.Combine(cacheFolder, $"{details.Id}_banner.jpg");
            string downloadUrl = details.Banner;

            // If the banner hasn't been downloaded yet
            if (File.Exists(localBannerPath))
            {
                // Return the local path of the downloaded banner
                return localBannerPath;
            }

            using var httpClient = new HttpClient();
            try
            {
                Logger.Logger.Warning("Downloading banner image {0}", details.Banner);
                // Download the banner image bytes
                byte[] imageData = await httpClient.GetByteArrayAsync(downloadUrl);

                // Save the banner image bytes to the local file system
                await File.WriteAllBytesAsync(localBannerPath, imageData);
            }
            catch (Exception)
            {
                return "assets/default_banner.png"; // Fallback banner image
            }

            // Return the local path of the downloaded banner
            return localBannerPath;
        }
        public static async Task<string> DownloadGameIconAsync(Game game)
        {
            string cacheFolder = Path.Combine(AppContext.BaseDirectory, "GameIcons");
            Directory.CreateDirectory(cacheFolder); // Ensure the folder exists

            string localIconPath = Path.Combine(cacheFolder, $"{game.ID}.jpg");

            string downloadUrl = $"http://media.steampowered.com/steamcommunity/public/images/apps/{game.ID}/{game.IconUrl}.jpg";

            if (!System.IO.File.Exists(localIconPath))
            {
                using var httpClient = new HttpClient();
                try
                {
                    byte[] imageData = await httpClient.GetByteArrayAsync(downloadUrl);
                    await System.IO.File.WriteAllBytesAsync(localIconPath, imageData);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to download icon for {game.Name}: {ex.Message}");
                    return "assets/default_icon.png"; // Fallback icon
                }
            }

            return localIconPath;
        }
    }
}
