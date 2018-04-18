using Sitecore.XConnect.Client;
using Sitecore.XConnect.Client.Configuration;

namespace Sitecore.Foundation.Analytics.Helpers
{
    public static class XConnectHelper
    {
        public static XConnectClient GetXConnectClient()
            => SitecoreXConnectClientConfiguration.GetClient();
    }
}
