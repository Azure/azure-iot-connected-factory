using System.Web.Mvc;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Helpers;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Navigation;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Controllers
{
    public class NavigationController : Controller
    {
        public NavigationController() { }

        public ActionResult NavigationMenu()
        {
            var navigationMenu = new NavigationMenu();

            string action = ControllerContext.ParentActionViewContext.RouteData.Values["action"].ToString();
            string controller = ControllerContext.ParentActionViewContext.RouteData.Values["controller"].ToString();

            NavigationHelper.ApplySelection(navigationMenu.NavigationMenuItems, controller, action);

            return PartialView("_NavigationMenu", navigationMenu.NavigationMenuItems);
        }
    }
}