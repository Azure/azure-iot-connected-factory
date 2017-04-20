using System.Web.Http;
using Owin;
using Microsoft.Owin;

[assembly: OwinStartup(typeof(Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Startup))]
namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp
{
    public partial class Startup
    {
        public static HttpConfiguration HttpConfiguration { get; private set; }

        /// <summary>
        /// Configures the web application.
        /// </summary>
        /// <param name="app">The app builder interface.</param>
        public void Configuration(IAppBuilder app)
        {
            HttpConfiguration = new HttpConfiguration();

            ConfigureAuth(app);

            app.MapSignalR();

            ConfigureWebApi(app);   

            ConfigureJson(app);

            ConfigureTopology();

            ConfigureRDX();

            ConfigureRDXTopologyWorker(Topology);

            ConfigureOpcUa();
        }
    }
}