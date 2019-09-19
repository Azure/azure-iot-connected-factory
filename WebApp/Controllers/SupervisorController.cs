using GlobalResources;
using Microsoft.AspNet.SignalR;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Contoso;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Helpers;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Models;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Security;
using Newtonsoft.Json;
using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;


namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Controllers
{
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models;
    using static Startup;

    [OutputCache(CacheProfile = "NoCacheProfile")]

    public class SupervisorController : Controller
    {
        /// <summary>
        /// Default action of the controller.
        /// </summary>
        [HttpGet]
        [RequirePermission(Permission.BrowseOpcServer)]
        public ActionResult Index()
        {
            List<Supervisor> supervisorList = new List<Supervisor>();
            OpcSessionModel sessionModel = new OpcSessionModel();
            Session["EndpointId"] = null;

            try
            {
                IEnumerable<SupervisorApiModel> supervisors = RegistryService.ListSupervisors();
                IEnumerable<ApplicationInfoApiModel> applications = RegistryService.ListApplications();
                Session["Applications"] = applications;

                if (supervisors != null)
                {
                    foreach (var supervisor in supervisors)
                    {
                        Supervisor supervisorInfo = new Supervisor();
                        supervisorInfo.supervisorModel = supervisor;
                        supervisorInfo.HasApplication = false;
                        foreach (var application in applications)
                        {
                            if (application.SupervisorId == supervisor.Id)
                            {
                                supervisorInfo.HasApplication = true;
                            }
                        }
                        supervisorList.Add(supervisorInfo);
                    }
                }
            }
            catch (Exception e)
            {
                Trace.TraceWarning("Can not get supervisors list");
                string errorMessage = string.Format(Strings.BrowserOpcException, e.Message,
                    e.InnerException?.Message ?? "--", e?.StackTrace ?? "--");
                Trace.TraceWarning(errorMessage);
            }

            sessionModel.supervisorList = supervisorList;
            return View("Index", sessionModel);
        }

        /// <summary>
        /// Post form method to set scan option.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken(Order = 1)]
        [RequirePermission(Permission.ControlOpcServer)]
        public void SetScanStatus(string supervisorId, string scanStatus)
        {
            SupervisorUpdateApiModel model = new SupervisorUpdateApiModel();

            if (scanStatus == "true")
            {
                model.Discovery = DiscoveryMode.Fast;
            }
            else
            {
                model.Discovery = DiscoveryMode.Off;
            }

            try
            {
                RegistryService.UpdateSupervisor(supervisorId, model);
            }
            catch (Exception exception)
            {
                string errorMessageTrace = string.Format(Strings.BrowserConnectException, exception.Message,
                exception.InnerException?.Message ?? "--", exception?.StackTrace ?? "--");
                Trace.TraceError(errorMessageTrace);
            }
        }
    }
}