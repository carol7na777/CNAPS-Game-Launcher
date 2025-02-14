using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Dave.Utility
{
    class LoginManager
    {
        public static async void LoginToSteam()
        {
            string steamLoginUrl = "https://steamcommunity.com/openid/login";

            // Create the necessary query parameters
            var queryParams = new Dictionary<string, string>
            {
                { "openid.ns", "http://specs.openid.net/auth/2.0" },
                { "openid.mode", "checkid_setup" },
                { "openid.claimed_id", "http://specs.openid.net/auth/2.0/identifier_select" },
                { "openid.identity", "http://specs.openid.net/auth/2.0/identifier_select" },
                { "openid.return_to", "http://localhost:4040/callback" },  // URL for your callback
                { "openid.realm", "http://localhost:4040/" }  // Use a placeholder if no real domain
            };

            var builder = new UriBuilder(steamLoginUrl) { Query = await new FormUrlEncodedContent(queryParams).ReadAsStringAsync() };
            var loginUrl = builder.ToString();

            // Open the login URL in the default browser
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = loginUrl,
                UseShellExecute = true
            });
        }
    }
}
