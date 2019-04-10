
using Microsoft.Owin;
using Owin;
using System.Threading;
using System.Web.Http;

[assembly: OwinStartup(typeof(Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Startup))]
namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp
{
    using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Controllers;
    using System.Collections.Generic;
    using System.Threading.Tasks;
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
            _tasksToShutdown = new List<Task>();

            DashboardController.Init();

            HttpConfiguration = new HttpConfiguration();

            ConfigureAuth(app);

            app.MapSignalR();

            ConfigureWebApi(app);   

            ConfigureJson(app);

            ConfigureTopology();

            _tasksToShutdown.Add(ConfigureIotHub(_shutdownTokenSource.Token));

            ConfigureRDX();

            ConfigureRDXTopologyWorker(Topology);

            ConfigureOpcUa();

            ConfigureTwinService();

            ConfigureRegistryService();

            _tasksToShutdown.AddRange(ConfigureUpdateSessions(_shutdownTokenSource.Token));
        }

        public static void End()
        {
            _shutdownTokenSource.Cancel();
            Task.WhenAll(_tasksToShutdown.ToArray());
            DashboardController.Deinit();
        }

        public static void Dispose()
        {
            SessionUpdate.Dispose();
        }

        private static CancellationTokenSource _shutdownTokenSource;
        private static List<Task> _tasksToShutdown;
    }
}