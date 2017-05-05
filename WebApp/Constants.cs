using Microsoft.AspNet.SignalR;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp
{
    public static class Constants
    {
        public const string CultureCookieName = "_culture";
    }

    [Authorize]
    public class TelemetryHub : Hub
    {

    }
}