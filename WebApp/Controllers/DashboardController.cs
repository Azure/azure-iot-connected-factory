
using GlobalResources;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Configuration;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Contoso;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Models;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Web.Mvc;
using System.Web.WebPages;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Controllers
{

    /// <summary>
    /// A Controller for Dashboard-related views.
    /// </summary>
    [RequirePermission(Permission.ViewTelemetry)]
    [OutputCache(CacheProfile = "NoCacheProfile")]
    public class DashboardController : Controller
    {
        public static void RemoveSessionFromSessionsViewingStations(string sessionId)
        {
            _sessionListSemaphore.Wait();
            try
            {
                _sessionsViewingStations.Remove(sessionId);
            }
            finally
            {
                _sessionListSemaphore.Release();
            }
        }

        public static int SessionsViewingStationsCount()
        {
            int count = 0;
            _sessionListSemaphore.Wait();
            try
            {
                count = _sessionsViewingStations.Count;
            }
            finally
            {
                _sessionListSemaphore.Release();
            }
            return count;
        }

        public static void Init()
        {
            if (_sessionListSemaphore == null)
            {
                _sessionListSemaphore = new SemaphoreSlim(1);
            }
            if (_sessionsViewingStations == null)
            {
                _sessionsViewingStations = new HashSet<string>();
            }
        }

        public static void Deinit()
        {
            if (_sessionListSemaphore != null)
            {
                _sessionListSemaphore.Dispose();
                _sessionListSemaphore = null;
            }
            if (_sessionsViewingStations != null)
            {
                _sessionsViewingStations = null;
            }
        }

        /// <summary>
        /// Initializes a new instance of the DashboardController class.
        /// </summary>
        public DashboardController()
        {
            // Set bing maps license key
            _mapQueryKey = ConfigurationProvider.GetConfigurationSettingValue("MapApiQueryKey");
            _mapQueryKey = (_mapQueryKey.IsEmpty() || _mapQueryKey.Equals("0")) ? string.Empty : _mapQueryKey;
        }

        [HttpGet]
        [RequirePermission(Permission.ViewTelemetry)]
        public ActionResult Index(string topNode)
        {
            DashboardModel dashboardModel;

            _sessionListSemaphore.Wait();
            try
            {
                // Reset topNode if invalid.
                if (string.IsNullOrEmpty(topNode) || Startup.Topology[topNode] == null)
                {
                    topNode = Startup.Topology.TopologyRoot.Key;
                }

                if (Session != null && Session.SessionID != null && Startup.SessionList.ContainsKey(Session.SessionID))
                {
                    // The session is known.
                    Trace.TraceInformation($"Session '{Session.SessionID}' is known");
                    dashboardModel = Startup.SessionList[Session.SessionID];

                    // Set the new dashboard TopNode.
                    dashboardModel.TopNode = (ContosoTopologyNode)Startup.Topology[topNode];

                    // Update type of children for the view and track sessions viewing at stations
                    Type topNodeType = dashboardModel.TopNode.GetType();
                    if (topNodeType == typeof(Factory))
                    {
                        _sessionsViewingStations.Remove(Session.SessionID);
                        dashboardModel.ChildrenType = typeof(ProductionLine);
                    }
                    else if (topNodeType == typeof(ProductionLine))
                    {
                        _sessionsViewingStations.Remove(Session.SessionID);
                        dashboardModel.ChildrenType = typeof(Station);
                    }
                    else if (topNodeType == typeof(Station))
                    {
                        _sessionsViewingStations.Add(Session.SessionID);
                        dashboardModel.ChildrenType = typeof(ContosoOpcUaNode);
                    }
                    else
                    {
                        // We must be at root
                        _sessionsViewingStations.Remove(Session.SessionID);
                        dashboardModel.TopNode = (ContosoTopologyNode)Startup.Topology.TopologyRoot;
                        dashboardModel.ChildrenType = typeof(Factory);
                    }
                    Trace.TraceInformation($"{_sessionsViewingStations.Count} session(s) viewing at Station nodes");
                }
                else
                {
                    // Create a new model and add it to the session list.
                    dashboardModel = new DashboardModel();
                    dashboardModel.TopNode = (ContosoTopologyNode)Startup.Topology.TopologyRoot;
                    dashboardModel.SessionId = Session.SessionID;
                    dashboardModel.ChildrenType = typeof(Factory);
                    dashboardModel.MapApiQueryKey = _mapQueryKey;

                    Trace.TraceInformation($"Add new session '{Session.SessionID}' to session list");
                    Startup.SessionList.Add(Session.SessionID, dashboardModel);

                }
            }
            catch (Exception e)
            {
                Trace.TraceInformation($"Exception in DashboardController ({e.Message})");
                dashboardModel = Startup.SessionList[Session.SessionID];
                dashboardModel.TopNode = (ContosoTopologyNode)Startup.Topology.TopologyRoot;
                dashboardModel.ChildrenType = typeof(Factory);
                dashboardModel.SessionId = Session.SessionID;
                dashboardModel.MapApiQueryKey = _mapQueryKey;
                _sessionsViewingStations.Remove(Session.SessionID);
            }
            finally
            {
                _sessionListSemaphore.Release();
            }

            // Add all alerts for the top level node.
            dashboardModel.Alerts = Startup.Topology.GetAlerts(topNode);

            // Update the children info.
            Trace.TraceInformation($"Show dashboard view for ({dashboardModel.TopNode.Key})");
            dashboardModel.Children = Startup.Topology.GetChildrenInfo(dashboardModel.TopNode.Key);
            if (dashboardModel.ChildrenType == typeof(Factory))
            {
                dashboardModel.ChildrenContainerHeader = Strings.ChildrenFactoryListContainerHeaderPostfix;
                dashboardModel.ChildrenListHeaderDetails = Strings.ChildrenFactoryListListHeaderDetails;
                dashboardModel.ChildrenListHeaderLocation = Strings.ChildrenFactoryListListHeaderLocation;
                dashboardModel.ChildrenListHeaderStatus = Strings.ChildrenFactoryListListHeaderStatus;
            }
            if (dashboardModel.ChildrenType == typeof(ProductionLine))
            {
                dashboardModel.ChildrenContainerHeader = dashboardModel.TopNode.Name + " " + Strings.ChildrenProductionLineListContainerHeaderPostfix;
                dashboardModel.ChildrenListHeaderDetails = Strings.ChildrenProductionLineListListHeaderDetails;
                dashboardModel.ChildrenListHeaderLocation = Strings.ChildrenProductionLineListListHeaderLocation;
                dashboardModel.ChildrenListHeaderStatus = Strings.ChildrenProductionLineListListHeaderStatus;
            }
            if (dashboardModel.ChildrenType == typeof(Station))
            {
                dashboardModel.ChildrenContainerHeader = ((ContosoTopologyNode)Startup.Topology[dashboardModel.TopNode.Parent]).Name + " - " + dashboardModel.TopNode.Name + " " + Strings.ChildrenStationListContainerHeaderPostfix;
                dashboardModel.ChildrenListHeaderDetails = Strings.ChildrenStationListListHeaderDetails;
                dashboardModel.ChildrenListHeaderLocation = Strings.ChildrenStationListListHeaderLocation;
                dashboardModel.ChildrenListHeaderStatus = Strings.ChildrenStationListListHeaderStatus;
            }
            if (dashboardModel.ChildrenType == typeof(ContosoOpcUaNode))
            {
                dashboardModel.ChildrenContainerHeader = ((ContosoTopologyNode)Startup.Topology[dashboardModel.TopNode.Key]).Name;
                dashboardModel.ChildrenContainerHeader = ((ContosoTopologyNode)Startup.Topology[dashboardModel.TopNode.Parent]).Name;
                dashboardModel.ChildrenContainerHeader = ((ContosoTopologyNode)Startup.Topology[Startup.Topology[dashboardModel.TopNode.Parent].Parent]).Name;
                dashboardModel.ChildrenContainerHeader = ((ContosoTopologyNode)Startup.Topology[Startup.Topology[dashboardModel.TopNode.Parent].Parent]).Name + " - " + ((ContosoTopologyNode)Startup.Topology[dashboardModel.TopNode.Parent]).Name + " - " + dashboardModel.TopNode.Name + " " + Strings.ChildrenOpcUaNodeListContainerHeaderPostfix;
                dashboardModel.ChildrenContainerHeader = ((ContosoTopologyNode)Startup.Topology[Startup.Topology[dashboardModel.TopNode.Parent].Parent]).Name + " - " + ((ContosoTopologyNode)Startup.Topology[dashboardModel.TopNode.Parent]).Name + " - " + dashboardModel.TopNode.Name + " " + Strings.ChildrenOpcUaNodeListContainerHeaderPostfix;
                dashboardModel.ChildrenListHeaderDetails = Strings.ChildrenOpcUaNodeListListHeaderDetails;
                dashboardModel.ChildrenListHeaderLocation = Strings.ChildrenOpcUaNodeListListHeaderLocation;
                dashboardModel.ChildrenListHeaderStatus = Strings.ChildrenOpcUaNodeListListHeaderStatus;
            }
            Trace.TraceInformation($"Show dashboard view for ({dashboardModel.TopNode.Key})");
            return View(dashboardModel);
        }

        private readonly string _mapQueryKey;
        private static SemaphoreSlim _sessionListSemaphore = null;
        private static HashSet<string>_sessionsViewingStations;
    }
}