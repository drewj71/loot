using GimmeTheLoot.Shared.Services;

namespace GimmeTheLoot.Client.Services
{
    public class PreferencesFactor : IPreferencesFactor
    {
        public void Set(string key, string? value)
        {
            Preferences.Set(key, value);
        }
    }
}
