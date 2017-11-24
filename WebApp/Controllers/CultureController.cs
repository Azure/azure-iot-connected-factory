
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Models;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Security;
using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Controllers
{
    /// <summary>
    /// A Controller for culture operations.
    /// </summary>
    [RequirePermission(Permission.ViewTelemetry)]
    public class CultureController : Controller
    {
        public CultureController() { }

        [HttpGet]
        [RequirePermission(Permission.ViewTelemetry)]
        [Route("culture/{cultureName}")]
        public ActionResult SetCulture(string cultureName)
        {
            // Save culture in a cookie
            HttpCookie cookie = this.Request.Cookies[Constants.CultureCookieName];

            if (cookie != null)
            {
                cookie.Value = cultureName; // update cookie value
            }
            else
            {
                cookie = new HttpCookie(Constants.CultureCookieName);
                cookie.Value = cultureName;
                cookie.Expires = DateTime.Now.AddYears(1);
            }

            Response.Cookies.Add(cookie);

            string topNodeKey = null;
            if (Session != null && Session.SessionID != null && Startup.SessionList.ContainsKey(Session.SessionID))
            {
                DashboardModel dashboardModel = null;
                dashboardModel = Startup.SessionList[Session.SessionID];
                topNodeKey = dashboardModel?.TopNode.Key;
            }
            RouteValueDictionary valueDictionary = new RouteValueDictionary { { "topNode", topNodeKey } };
            return RedirectToAction("Index", "Dashboard", valueDictionary);
        }
    }
}