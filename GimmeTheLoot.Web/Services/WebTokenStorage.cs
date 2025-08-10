using System.Threading.Tasks;
using GimmeTheLoot.Shared.Services;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace GimmeTheLoot.Web.Services
{
    public class WebTokenStorage : ITokenStorage
    {
        private readonly ProtectedLocalStorage _storage;
        const string TokenKey = "authToken";

        public WebTokenStorage(ProtectedLocalStorage storage)
        {
            _storage = storage;
        }

        public async Task SetTokenAsync(string token)
            => await _storage.SetAsync(TokenKey, token);

        public async Task<string?> GetTokenAsync()
        {
            var result = await _storage.GetAsync<string>(TokenKey);
            return result.Success ? result.Value : null;
        }

        public async Task RemoveTokenAsync()
            => await _storage.DeleteAsync(TokenKey);
    }
}
