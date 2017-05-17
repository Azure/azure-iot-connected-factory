using System;
using System.Diagnostics;
using System.Net.Mime;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Web.Mvc;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Helpers;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Models;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Security;
using GlobalResources;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Controllers
{
    public class AddOpcServerController : Controller
    {
        /// <summary>
        /// Default action of the controller.
        /// </summary>
        [HttpGet]
        [RequirePermission(Permission.DownloadCertificate, Permission.AddOpcServer)]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Downloads the web app's UA client application certificate for use in UA servers the user wants to connect to
        /// </summary>
        [HttpGet]
        [RequirePermission(Permission.DownloadCertificate, Permission.AddOpcServer)]
        public async Task<ActionResult> Download()
        {
            try
            {
                X509Certificate2 certificate = await OpcSessionHelper.Instance.GetApplicationCertificate();
                return File(certificate.GetRawCertData(), MediaTypeNames.Application.Octet, certificate.FriendlyName + ".der");
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
                OpcSessionModel sessionModel = new OpcSessionModel
                {
                    ErrorHeader = Strings.BrowserCertDownloadError,
                    EndpointUrl = string.Empty,
                    ErrorMessage = Strings.BrowserCertDownloadError
                };

                return View("Error", sessionModel);
            }
        }
    }
}