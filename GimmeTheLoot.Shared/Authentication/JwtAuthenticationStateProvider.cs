using GimmeTheLoot.Shared.Services;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace GimmeTheLoot.Shared.Authentication
{
    public class JwtAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly ITokenStorage _tokenStorage;

        public JwtAuthenticationStateProvider(ITokenStorage tokenStorage)
        {
            _tokenStorage = tokenStorage;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var token = await _tokenStorage.GetTokenAsync();
            if (string.IsNullOrEmpty(token))
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

            var claims = ParseClaimsFromJwt(token);
            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);

            return new AuthenticationState(user);
        }

        public void NotifyUserAuthentication(string token)
        {
            var claims = ParseClaimsFromJwt(token);
            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        }

        public void NotifyUserLogout()
        {
            var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anonymous)));
        }

        private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            var claims = new List<Claim>();

            // JWT format: header.payload.signature
            var payload = jwt.Split('.')[1];

            // Pad the base64 string if needed (Base64URL)
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            var bytes = Convert.FromBase64String(payload);
            var jsonPayload = Encoding.UTF8.GetString(bytes);

            // Deserialize JSON to dictionary
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonPayload);

            if (keyValuePairs == null)
                return claims;

            foreach (var kvp in keyValuePairs)
            {
                // Special handling for "role" claim because it can be a single string or array
                if (kvp.Key == "role" || kvp.Key == ClaimTypes.Role)
                {
                    if (kvp.Value is JsonElement element)
                    {
                        if (element.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var role in element.EnumerateArray())
                            {
                                claims.Add(new Claim(ClaimTypes.Role, role.GetString() ?? ""));
                            }
                        }
                        else
                        {
                            claims.Add(new Claim(ClaimTypes.Role, element.GetString() ?? ""));
                        }
                    }
                    else if (kvp.Value is string roleString)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, roleString));
                    }
                }
                else
                {
                    claims.Add(new Claim(kvp.Key, kvp.Value.ToString() ?? ""));
                }
            }

            return claims;
        }

        public async Task LogoutAsync()
        {
            await _tokenStorage.RemoveTokenAsync();
            NotifyUserLogout();
        }

        public async Task<string> GetTokenAsync()
        {
            var authState = await GetAuthenticationStateAsync();
            var user = authState.User;

            if (!user.Identity?.IsAuthenticated ?? true)
                throw new Exception("User is not authenticated");

            // Your token storage contains the raw JWT token
            // Either fetch from tokenStorage directly or get claims from user and reconstruct token
            // Assuming ITokenStorage exposes a method GetTokenAsync():
            var token = await _tokenStorage.GetTokenAsync();
            return token ?? throw new Exception("JWT token not found");
        }
    }
}
