using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Dave.Modules.Steam
{
    public class SteamLoginCallbackHandler
    {
        private readonly HttpListener m_HttpListener;
        private string m_SteamID;
        private bool m_IsListening = false;

        public SteamLoginCallbackHandler()
        {
            m_HttpListener = new HttpListener();
            m_HttpListener.Prefixes.Add($"http://localhost:4040/callback/");  // Listen for this URL
        }

        public string GetSteamID()
        {
            return m_SteamID;
        }

        public async Task StartListening()
        {
            if (m_IsListening)
            {
                Logger.Logger.Warning("HTTP listener is already running.");
                return;
            }

            m_IsListening = true;
            m_HttpListener.Start();

            Logger.Logger.Debug("Listening for Steam callback on http://localhost:4040/callback/");

            while (m_IsListening)
            {
                var context = await m_HttpListener.GetContextAsync();
                var request = context.Request;
                var queryParams = request.Url.Query;

                Logger.Logger.Warning($"Received query: {queryParams}");

                var response = context.Response;

                if (queryParams.Contains("openid.mode") && queryParams.Contains("openid.error"))
                {
                    var errorMessage = "Steam login error: " + ExtractError(queryParams);
                    Logger.Logger.Error(errorMessage);
                    response.StatusCode = 400;
                    await SendResponseAsync(response, errorMessage);
                    StopListening();
                    break;
                }
                else if (queryParams.Contains("openid.claimed_id"))
                {
                    m_SteamID = ExtractClaimedId(queryParams);
                    Logger.Logger.Info($"Steam login successful for user: {m_SteamID}");
                    await SendResponseAsync(response, "Steam login successful! You can close this window.");
                    StopListening();
                    break;
                }
                else
                {
                    Logger.Logger.Error("Error: Missing 'openid.claimed_id' in response.");
                    response.StatusCode = 400;
                    await SendResponseAsync(response, "Invalid Steam OpenID response.");
                    StopListening();
                    break;
                }
            }
        }

        private static async Task SendResponseAsync(HttpListenerResponse response, string message)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer);
            response.OutputStream.Close();
        }

        public void StopListening()
        {
            if (!m_IsListening) return;

            Logger.Logger.Debug("Stopping HTTP listener.");
            m_IsListening = false;
            m_HttpListener.Stop();
        }

        private static string ExtractError(string queryParams)
        {
            var param = "openid.error=";
            var startIndex = queryParams.IndexOf(param, StringComparison.OrdinalIgnoreCase) + param.Length;
            var endIndex = queryParams.IndexOf('&', startIndex);
            if (endIndex == -1) endIndex = queryParams.Length;
            return queryParams[startIndex..endIndex];
        }

        private static string ExtractClaimedId(string queryParams)
        {
            var param = "openid.claimed_id=";
            var startIndex = queryParams.IndexOf(param, StringComparison.OrdinalIgnoreCase) + param.Length;
            var endIndex = queryParams.IndexOf('&', startIndex);
            if (endIndex == -1) endIndex = queryParams.Length;

            var claimedId = queryParams[startIndex..endIndex];

            // Now extract the user ID part after the base URL
            var userId = claimedId.Replace("https%3A%2F%2Fsteamcommunity.com%2Fopenid%2Fid%2F", "");

            return userId;
        }
    }

}
