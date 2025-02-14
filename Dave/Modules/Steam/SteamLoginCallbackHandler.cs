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

        public SteamLoginCallbackHandler(string callbackUrl)
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
            m_HttpListener.Start();
            Logger.Logger.Debug("Listening for Steam callback on http://localhost:4040/callback/");

            while (true)
            {
                var context = await m_HttpListener.GetContextAsync();
                var request = context.Request;
                var queryParams = request.Url.Query;

                Logger.Logger.Warning($"Received query: {queryParams}");

                var response = context.Response;

                // Check for error and handle accordingly
                if (queryParams.Contains("openid.mode") && queryParams.Contains("openid.error"))
                {
                    var errorMessage = "Steam login error: " + ExtractError(queryParams);
                    Logger.Logger.Error(errorMessage);  // Log the error message
                    response.StatusCode = 400;
                    var buffer = Encoding.UTF8.GetBytes(errorMessage);
                    response.ContentLength64 = buffer.Length;
                    await response.OutputStream.WriteAsync(buffer);
                    response.OutputStream.Close();
                    StopListening();
                    break;
                }
                else
                {
                    if (queryParams.Contains("openid.claimed_id"))
                    {
                        m_SteamID = ExtractClaimedId(queryParams);
                        Logger.Logger.Info($"Steam login successful for user: {m_SteamID}");
                        response.StatusCode = 200;
                        var responseString = "Steam login successful! You can close this window.";
                        var buffer = Encoding.UTF8.GetBytes(responseString);
                        response.ContentLength64 = buffer.Length;
                        await response.OutputStream.WriteAsync(buffer);
                        response.OutputStream.Close();
                        StopListening();
                        break;
                    }
                    else
                    {
                        // Log if the OpenID response is missing the expected claim
                        Logger.Logger.Error("Error: Missing 'openid.claimed_id' in response.");
                        response.StatusCode = 400;
                        var responseString = "Invalid Steam OpenID response.";
                        var buffer = Encoding.UTF8.GetBytes(responseString);
                        response.ContentLength64 = buffer.Length;
                        await response.OutputStream.WriteAsync(buffer);
                        response.OutputStream.Close();
                        StopListening();
                        break;
                    }
                }

            }
        }

        private static string ExtractError(string queryParams)
        {
            var param = "openid.error=";
            var startIndex = queryParams.IndexOf(param, StringComparison.OrdinalIgnoreCase) + param.Length;
            var endIndex = queryParams.IndexOf('&', startIndex);
            if (endIndex == -1) endIndex = queryParams.Length;
            return queryParams.Substring(startIndex, endIndex - startIndex);
        }

        private static string ExtractClaimedId(string queryParams)
        {
            var param = "openid.claimed_id=";
            var startIndex = queryParams.IndexOf(param, StringComparison.OrdinalIgnoreCase) + param.Length;
            var endIndex = queryParams.IndexOf('&', startIndex);
            if (endIndex == -1) endIndex = queryParams.Length;

            var claimedId = queryParams.Substring(startIndex, endIndex - startIndex);

            // Now extract the user ID part after the base URL
            var userId = claimedId.Replace("https%3A%2F%2Fsteamcommunity.com%2Fopenid%2Fid%2F", "");

            return userId;
        }

        public void StopListening()
        {
            m_HttpListener.Stop();
        }
    }

}
