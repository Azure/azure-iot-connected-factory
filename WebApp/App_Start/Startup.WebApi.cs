using System.Net;
using System.Web.Http;
using Owin;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp
{
    public partial class Startup
    {
        public void ConfigureWebApi(IAppBuilder app)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            app.UseWebApi(HttpConfiguration);
            HttpConfiguration.MapHttpAttributeRoutes();
        }
    }
}