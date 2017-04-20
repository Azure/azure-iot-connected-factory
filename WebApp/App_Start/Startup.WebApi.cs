using System.Web.Http;
using Owin;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp
{
    public partial class Startup
    {
        public void ConfigureWebApi(IAppBuilder app)
        {
            app.UseWebApi(HttpConfiguration);
            HttpConfiguration.MapHttpAttributeRoutes();
        }
    }
}