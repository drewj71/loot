using GimmeTheLoot.Shared.Services;

namespace GimmeTheLoot.Web.Services
{
    public class PlatformFactor : IPlatformFactor
    {
        public string GetDeviceIdom()
        {
            return "Web";
        }

        public string GetPlatform()
        {
            return Environment.OSVersion.ToString();
        }
    }
}
