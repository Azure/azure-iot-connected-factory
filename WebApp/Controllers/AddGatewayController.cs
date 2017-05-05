
using System.Web.Mvc;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Security;

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
            return View();
        }
    }
}