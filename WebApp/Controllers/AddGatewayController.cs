
using System.Web.Mvc;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Configuration;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Security;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Models;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Controllers
{
    /// <summary>
    /// A Controller for AddGateway views.
    /// </summary>
    public class AddGatewayController : Controller
    {
        /// <summary>
        /// Default action of the controller.
        /// </summary>
        [HttpGet]
        [RequirePermission(Permission.AddOpcServer)]
        public ActionResult Index()
        {
            AddGatewayModel gatewayModel = new AddGatewayModel();

            gatewayModel.IotHubName = ConfigurationProvider.GetConfigurationSettingValue("IotHubEventHubName");
            return View(gatewayModel);
        }
    }
}