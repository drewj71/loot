using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace TheLoot.Services
{
    public class PlaidService
    {
        private readonly HttpClient _httpClient;
        private readonly string _clientId;
        private readonly string _secret;
        private readonly string _environment;
        private readonly string _baseUrl;

        public PlaidService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _clientId = config["Plaid:ClientId"];
            _secret = config["Plaid:Secret"];
            _environment = config["Plaid:Environment"] ?? "sandbox";

            _baseUrl = _environment switch
            {
                "production" => "https://production.plaid.com",
                "development" => "https://development.plaid.com",
                _ => "https://sandbox.plaid.com"
            };
        }

        // 1. Create Link Token
        public async Task<string> CreateLinkTokenAsync(string userId)
        {
            var request = new
            {
                client_id = _clientId,
                secret = _secret,
                client_name = "Loot",
                user = new { client_user_id = userId },
                products = new[] { "auth", "transactions" },
                country_codes = new[] { "US" },
                language = "en"
            };

            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/link/token/create", request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<LinkTokenResponse>();
            return result?.link_token ?? throw new System.Exception("Failed to get link_token");
        }

        // 2. Exchange Public Token for Access Token
        public async Task<string> ExchangePublicTokenAsync(string publicToken)
        {
            var request = new
            {
                client_id = _clientId,
                secret = _secret,
                public_token = publicToken
            };

            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/item/public_token/exchange", request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ExchangeTokenResponse>();
            return result?.access_token ?? throw new System.Exception("Failed to exchange public_token");
        }

        // Response DTOs
        private class LinkTokenResponse
        {
            public string link_token { get; set; }
            public string expiration { get; set; }
        }

        private class ExchangeTokenResponse
        {
            public string access_token { get; set; }
            public string item_id { get; set; }
            public string request_id { get; set; }
        }
    }
}
