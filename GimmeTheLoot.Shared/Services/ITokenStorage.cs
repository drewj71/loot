namespace GimmeTheLoot.Shared.Services
{
    public interface ITokenStorage
    {
        Task SetTokenAsync(string token);
        Task<string?> GetTokenAsync();
        Task RemoveTokenAsync();
    }
}
