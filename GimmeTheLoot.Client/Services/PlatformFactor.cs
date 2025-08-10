using GimmeTheLoot.Shared.Services;

namespace GimmeTheLoot.Client.Services
{
    public class PlatformFactor : IPlatformFactor
    {
        public string GetDeviceIdom()
        {
            return DeviceInfo.Idiom.ToString();
        }

        public string GetPlatform()
        {
            return DeviceInfo.Platform.ToString() + " - " + DeviceInfo.VersionString;
        }
    }
}
