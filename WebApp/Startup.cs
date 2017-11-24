
using Microsoft.Owin;
using Owin;
using System.Threading;
using System.Web.Http;

[assembly: OwinStartup(typeof(Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Startup))]
namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp
{
    using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Controllers;
    using static Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.SessionUpdate;

    public partial class Startup
    {
        public static HttpConfiguration HttpConfiguration { get; private set; }

        /// <summary>
        /// Configures the web application.
        /// </summary>
        public void Configuration(IAppBuilder app)
        {
            _shutdownTokenSource = new CancellationTokenSource();
            DashboardController.Init();

            HttpConfiguration = new HttpConfiguration();

            ConfigureAuth(app);

            app.MapSignalR();

            ConfigureWebApi(app);   

            ConfigureJson(app);

            ConfigureTopology();

            ConfigureIotHub();

            ConfigureRDX();

            ConfigureRDXTopologyWorker(Topology);

            ConfigureOpcUa();

            ConfigureUpdateSessions(_shutdownTokenSource.Token);
        }

        public static void End()
        {
            _shutdownTokenSource.Cancel();
            DashboardController.Deinit();
        }

        public static void Dispose()
        {
            SessionUpdate.Dispose();
        }

        private static CancellationTokenSource _shutdownTokenSource;
    }
}