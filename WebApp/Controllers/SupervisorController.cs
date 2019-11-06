using GlobalResources;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Models;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Web.Mvc;


namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Controllers
{
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
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
            OpcSessionModel sessionModel = new OpcSessionModel();

            sessionModel.supervisorList = GetSupervisors();
            return View("Index", sessionModel);
        }

        /// <summary>
        /// Post form method to set scan option.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken(Order = 1)]
        [RequirePermission(Permission.ControlOpcServer)]
        public List<Supervisor> GetSupervisors()
        {
            List<Supervisor> supervisorList = new List<Supervisor>();
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

            return supervisorList;
        }

        /// <summary>
        /// Post form method to update session model.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken(Order = 1)]
        [RequirePermission(Permission.ControlOpcServer)]
        public ActionResult UpdateModel()
        {
            OpcSessionModel sessionModel = new OpcSessionModel();

            sessionModel.supervisorList = GetSupervisors();
            ModelState.Clear();
            return PartialView("_SupervisorList", sessionModel);
        }

        /// <summary>
        /// Post form method to set scan option.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken(Order = 1)]
        [RequirePermission(Permission.ControlOpcServer)]
        public void SetScanStatus(string supervisorId, string scanStatus, string ipMask, string portRange)
        {
            SupervisorUpdateApiModel model = new SupervisorUpdateApiModel();
            model.DiscoveryConfig = new DiscoveryConfigApiModel();
            model.DiscoveryConfig.AddressRangesToScan = "";
            model.DiscoveryConfig.PortRangesToScan = "";


            if ((ipMask != null) && (ipMask != string.Empty))
            {
                model.DiscoveryConfig.AddressRangesToScan = ipMask;
            }
            if ((portRange != null) && (portRange != string.Empty))
            {
                model.DiscoveryConfig.PortRangesToScan = portRange;
            }

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