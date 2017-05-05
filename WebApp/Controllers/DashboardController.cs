using System;
using System.Web.Mvc;
using System.Web.WebPages;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Contoso;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Configuration;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Models;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Security;
using GlobalResources;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Controllers
{

    /// <summary>
    /// A Controller for Dashboard-related views.
    /// </summary>
    [RequirePermission(Permission.ViewTelemetry)]
    [OutputCache(CacheProfile = "NoCacheProfile")]
    public class DashboardController : Controller
    {
        /// <summary>
        /// The configuration provider for the web application.
        /// </summary>

        private readonly string _mapQueryKey;

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

            // Reset topNode if invalid.
            if (topNode == null || (topNode != null && topNode != "" && Startup.Topology[topNode] == null))
            {
                topNode = Startup.Topology.TopologyRoot.Key;
            }

            if (Startup.SessionList.ContainsKey(Session.SessionID))
            {
                // The session is known.
                dashboardModel = Startup.SessionList[Session.SessionID];

                // If the requested topNode is not equal the current session topNode, we need to update our TopNode and the model data.
                if (((topNode == null || topNode == Startup.Topology.TopologyRoot.Key) && dashboardModel.TopNode.Key != Startup.Topology.TopologyRoot.Key) ||
                    (topNode != null && topNode != Startup.Topology.TopologyRoot.Key))
                {
                    // Set the new TopNode.
                    dashboardModel.TopNode = (ContosoTopologyNode)Startup.Topology[topNode];

                    // Update type of children for the view.
                    Type topNodeType = dashboardModel.TopNode.GetType();
                    dashboardModel.ChildrenType = typeof(Factory).ToString();
                    if (topNodeType == typeof(Factory))
                    {
                        dashboardModel.ChildrenType = typeof(ProductionLine).ToString();
                    }
                    else if (topNodeType == typeof(ProductionLine))
                    {
                        dashboardModel.ChildrenType = typeof(Station).ToString();
                    }
                    else if (topNodeType == typeof(Station))
                    {
                        dashboardModel.ChildrenType = typeof(ContosoOpcUaNode).ToString();
                    }
                }
            }
            else
            {
                // Create a new model and add it to the session list.
                dashboardModel = new DashboardModel();
                dashboardModel.TopNode = (ContosoTopologyNode)Startup.Topology.TopologyRoot;
                dashboardModel.SessionId = Session.SessionID;

                Startup.SessionList.Add(Session.SessionID, dashboardModel);

                // Init model data.
                dashboardModel.MapApiQueryKey = _mapQueryKey;
                dashboardModel.ChildrenType = typeof(Factory).ToString();
            }

            // Add all alerts for the top level node.
            dashboardModel.Alerts = Startup.Topology.GetAlerts(topNode);

            // Update the children info.
            dashboardModel.Children = Startup.Topology.GetChildrenInfo(topNode);

            if (dashboardModel.ChildrenType == typeof(Factory).ToString())
            {
                dashboardModel.ChildrenContainerHeader = Strings.ChildrenFactoryListContainerHeaderPostfix;
                dashboardModel.ChildrenListHeaderDetails = Strings.ChildrenFactoryListListHeaderDetails;
                dashboardModel.ChildrenListHeaderLocation = Strings.ChildrenFactoryListListHeaderLocation;
                dashboardModel.ChildrenListHeaderStatus = Strings.ChildrenFactoryListListHeaderStatus;
            }
            if (dashboardModel.ChildrenType == typeof(ProductionLine).ToString())
            {
                dashboardModel.ChildrenContainerHeader = dashboardModel.TopNode.Name + " " + Strings.ChildrenProductionLineListContainerHeaderPostfix;
                dashboardModel.ChildrenListHeaderDetails = Strings.ChildrenProductionLineListListHeaderDetails;
                dashboardModel.ChildrenListHeaderLocation = Strings.ChildrenProductionLineListListHeaderLocation;
                dashboardModel.ChildrenListHeaderStatus = Strings.ChildrenProductionLineListListHeaderStatus;
            }
            if (dashboardModel.ChildrenType == typeof(Station).ToString())
            {
                dashboardModel.ChildrenContainerHeader = ((ContosoTopologyNode)Startup.Topology[dashboardModel.TopNode.Parent]).Name + " - " + dashboardModel.TopNode.Name + " " + Strings.ChildrenStationListContainerHeaderPostfix;
                dashboardModel.ChildrenListHeaderDetails = Strings.ChildrenStationListListHeaderDetails;
                dashboardModel.ChildrenListHeaderLocation = Strings.ChildrenStationListListHeaderLocation;
                dashboardModel.ChildrenListHeaderStatus = Strings.ChildrenStationListListHeaderStatus;
            }
            if (dashboardModel.ChildrenType == typeof(ContosoOpcUaNode).ToString())
            {
                dashboardModel.ChildrenContainerHeader = ((ContosoTopologyNode)Startup.Topology[Startup.Topology[dashboardModel.TopNode.Parent].Parent]).Name + " - " + ((ContosoTopologyNode)Startup.Topology[dashboardModel.TopNode.Parent]).Name + " - " + dashboardModel.TopNode.Name + " " + Strings.ChildrenOpcUaNodeListContainerHeaderPostfix;
                dashboardModel.ChildrenListHeaderDetails = Strings.ChildrenOpcUaNodeListListHeaderDetails;
                dashboardModel.ChildrenListHeaderLocation = Strings.ChildrenOpcUaNodeListListHeaderLocation;
                dashboardModel.ChildrenListHeaderStatus = Strings.ChildrenOpcUaNodeListListHeaderStatus;
            }
            return View(dashboardModel);
        }
    }
}