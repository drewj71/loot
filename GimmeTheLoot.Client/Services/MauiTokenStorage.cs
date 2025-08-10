using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using GimmeTheLoot.Shared.Services;

namespace GimmeTheLoot.Client.Services
{
    public class MauiTokenStorage : ITokenStorage
    {
        const string TokenKey = "authToken";

        public Task SetTokenAsync(string token)
            => SecureStorage.SetAsync(TokenKey, token);

        public async Task<string?> GetTokenAsync()
            => await SecureStorage.GetAsync(TokenKey);

        public Task RemoveTokenAsync()
        {
            SecureStorage.Remove(TokenKey);
            return Task.CompletedTask;
        }
    }
}
