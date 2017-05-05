using System.Web;
using System.Web.Mvc;
using System.Diagnostics;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Controllers
{
    [OutputCache(CacheProfile = "NoCacheProfile")]
    public class AccountController : Controller
    {
        /// <summary>
        /// Check if user is signed in and forward to the Dashboard view or to authentication.
        /// </summary>
        [AllowAnonymous]
        public ActionResult SignIn()
        {
            Trace.TraceInformation("SignIn -IsAuthenticated: {0}, New session: {1}", User.Identity.IsAuthenticated, Session.IsNewSession);
            // For authenticated user, we forward to the Dashboard.
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Dashboard");
            }
            else
            {
                    return View();
            }
        }

        /// <summary>
        /// Sign out the current signed in user.
        /// </summary>
        public ActionResult SignOut()
        {
            Trace.TraceInformation("SignOut - User: {0}, AuthenticationType: {1}", User.Identity.Name, User.Identity.AuthenticationType);
            HttpContext.GetOwinContext().Authentication.SignOut();
            Session.Abandon();
            return RedirectToAction("Index", "Dashboard");
        }
        public AccountController() { }
    }
}