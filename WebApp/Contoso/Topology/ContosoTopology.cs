using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Topology;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.OpcUa;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Security;
using Newtonsoft.Json;
using GlobalResources;
using System.Globalization;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Contoso
{
    /// <summary>
    /// Class to parse description common for all topology nodes (global, factory, production line, station)
    /// </summary>
    public class ContosoTopologyDescriptionCommon
    {
        [JsonProperty]
        public string Name;

        [JsonProperty]
        public string Description;

        [JsonProperty]
        public string Image;

        [JsonProperty]
        public ContosoPerformanceDescription OeeOverall;

        [JsonProperty]
        public ContosoPerformanceDescription OeePerformance;

        [JsonProperty]
        public ContosoPerformanceDescription OeeAvailability;

        [JsonProperty]
        public ContosoPerformanceDescription OeeQuality;

        [JsonProperty]
        public ContosoPerformanceDescription Kpi1;

        [JsonProperty]
        public ContosoPerformanceDescription Kpi2;

        /// <summary>
        /// Ctor for the information common to all topology node descriptions.
        /// </summary>
        public ContosoTopologyDescriptionCommon()
        {
            Name = "";
            Description = "";
            Image = "";
            OeeOverall = new ContosoPerformanceDescription();
            OeePerformance = new ContosoPerformanceDescription();
            OeeAvailability = new ContosoPerformanceDescription();
            OeeQuality = new ContosoPerformanceDescription();
            Kpi1 = new ContosoPerformanceDescription();
            Kpi2 = new ContosoPerformanceDescription();
        }
    }

    /// <summary>
    /// Class for the top level (global) topology description.
    /// </summary>
    public class TopologyDescription : ContosoTopologyDescriptionCommon
    {
        [JsonProperty]
        public List<FactoryDescription> Factories;
    }

    /// <summary>
    /// Class to define information of child nodes in the topology
    /// </summary>
    public class ContosoChildInfo
    {
        // Key to address the node in the toplogy. For OPC UA servers (Station), this it the Application URI of the OPC UA application.
        public string Key { get; set; }

        // For OPC UA Nodes this containes the OPC UA nodeId.
        public string SubKey { get; set; }

        // Status of the topology node, shown in the UX.
        public string Status { get; set; }

        // Name of the topology node. shown in the UX.
        public string Name { get; set; }

        // Description of the toplogy node, shown in the UX.
        public string Description { get; set; }

        // City the topology node resides, shown in the UX.
        public string City { get; set; }

        // Last value               
        public string Last { get; set; }

        //Unit of node value
        public string Unit { get; set; }


        // Geo location the toplogy node resides. Could be used in the UX.
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool Visible { get; set; }

        /// <summary>
        /// Ctor for child node information of topology nodes (non OPC UA nodes) using values.
        /// </summary>
        public ContosoChildInfo(string key, string status, string name, string description, string city, double latitude, double longitude, bool visible)
        {
            Key = key;
            SubKey = "null";
            Status = status;
            Name = name;
            Description = description;
            City = city;
            Latitude = latitude;
            Longitude = longitude;
            Visible = visible;
        }

        /// <summary>
        /// Ctor for child node information of OPC UA nodes.
        /// </summary>

        public ContosoChildInfo(string key, string subKey, string status, string name, string description, string city, double latitude, double longitude, bool visible, string last, string unit)
        {
            Key = key;
            SubKey = subKey;
            Status = status;
            Name = name;
            Description = description;
            City = city;
            Latitude = latitude;
            Longitude = longitude;
            Visible = visible;
            Last = last;
            Unit = unit != null ? unit : "";
        }
    }

    /// <summary>
    /// For Contoso the topology is organized as a tree. Below the TopologyRoot (which is the global company view) there are "Factory" children,
    /// those have "ProductionLine" children, which have "Station" children. These "Station"s are 
    /// OPC UA server systems, which contain OPC UA Nodes.
    /// </summary>
    public class ContosoTopology : TopologyTree
    {

        /// <summary>
        /// Ctor for the Contoso topology.
        /// </summary>
        /// <param name="topologyDescriptionFilename"></param>
        public ContosoTopology(string topologyDescriptionFilename)
        {
            TopologyDescription topologyDescription;

            // Read the JSON equipment topology description.
            using (StreamReader topologyDescriptionStream = File.OpenText(topologyDescriptionFilename))
            {
                JsonSerializer serializer = new JsonSerializer();
                topologyDescription = (TopologyDescription)serializer.Deserialize(topologyDescriptionStream, typeof(TopologyDescription));
            }

            // Build the topology tree, start with the global level.
            ContosoTopologyNode rootNode = new ContosoTopologyNode("TopologyRoot", "Global", "Contoso", topologyDescription);
            TopologyRoot = rootNode;

            // There must be at least one factory.
            if (topologyDescription.Factories.Count == 0)
            {
                IndexOutOfRangeException indexOutOfRange = new IndexOutOfRangeException("There must be at least one factory defined.");
                throw indexOutOfRange;
            }

            // Iterate through the whole description level by level.
            foreach (var factoryDescription in topologyDescription.Factories)
            {
                // Add it to the tree.
                Factory factory = new Factory(factoryDescription);
                AddChild(TopologyRoot.Key, factory);

                // There must be at least one production line.
                if (factoryDescription.ProductionLines.Count == 0)
                {
                    string message = String.Format("There must be at least one production line defined for factory '{0}'.", factory.Name);
                    IndexOutOfRangeException indexOutOfRange = new IndexOutOfRangeException(message);
                    throw indexOutOfRange;
                }

                // Handle all production lines.
                foreach (var productionLineDescription in factoryDescription.ProductionLines)
                {
                    // Add it to the tree.
                    ProductionLine productionLine = new ProductionLine(productionLineDescription);
                    productionLine.Location = factory.Location;
                    AddChild(factory.Key, productionLine);

                    // There must be at least one station.
                    if (productionLineDescription.Stations.Count == 0)
                    {
                        string message = String.Format("There must be at least one station defined for production line '{0}' in factory '{1}'.", productionLine.Name, factory.Name);
                        IndexOutOfRangeException indexOutOfRange = new IndexOutOfRangeException(message);
                        throw indexOutOfRange;
                    }

                    // Handle all stations (a station is running an OPC UA server).
                    foreach (var stationDescription in productionLineDescription.Stations)
                    {
                        // Add it to the tree.
                        Station station = new Station(stationDescription);
                        station.Location = productionLine.Location;
                        AddChild(productionLine.Key, station);
                    }
                }
            }

            // configuration only contains settings for stations, calculate the remaining topology
            UpdateAllKPIAndOEEPerfItems();
        }

        /// <summary>
        /// Checks if there are any alerts under the node with the given key.
        /// </summary>
        public bool HasAnyAlerts(string key)
        {
            bool alertFound = false;

            foreach (var child in GetAllChildren(key))
            {
                if (TopologyTable[child].GetType() == typeof(Station) && ((Station)TopologyTable[child]).HasAlerts)
                {
                    alertFound = true;
                    break;
                }
            }
            return alertFound;
        }


        /// <summary>
        /// Retrieves all alerts under the node with the given key.
        /// </summary>
        public List<ContosoAlert> GetAllAlerts(string key)
        {
            List<ContosoAlert> allAlerts = new List<ContosoAlert>();

            // Walk through all nodes inclusive self.
            List<string> allNodes = GetAllChildren(key);
            allNodes.Add(key);
            foreach (var child in allNodes)
            {
                ContosoTopologyNode topologyNode = TopologyTable[child] as ContosoTopologyNode;
                if (topologyNode.HasAlerts)
                {
                    // We are only interested in active and acknowledged alerts.
                    allAlerts.AddRange(topologyNode.Alerts);
                }
            }
            return allAlerts;
        }

        /// <summary>
        /// Lookup the alert with alertId.
        /// </summary>
        public ContosoAlert FindAlert(long alertId)
        {
            List<string> allTopologyNodeKeys = GetAllChildren(TopologyRoot.Key);
            allTopologyNodeKeys.Add(TopologyRoot.Key);
            foreach (var key in allTopologyNodeKeys)
            {
                ContosoTopologyNode topologyNode = TopologyTable[key] as ContosoTopologyNode;
                if (topologyNode == null)
                {
                    Trace.TraceError($"Can not find node with key '{key}' in the topology.");
                    return null;
                }

                // Check the node for the alert id.
                ContosoAlert alert = topologyNode.FindAlert(alertId);

                // Return when found.
                if (alert != null)
                {
                    return alert;
                }
            }
            
            Trace.TraceError($"Can not find alert with id  '{alertId}'.");
            return null;
        }

        /// <summary>
        /// Gets alert information for the given topology node and all nodes below it. 
        /// Multiple alerts with the same cause are consolidated and only the time information for the newest alert is returned.
        /// This information will be shown in the UX.
        /// </summary>
        public List<ContosoAlertInfo> GetAlerts(string key)
        {
            List<ContosoAlert> alertList = GetAllAlerts(key);
            List<ContosoAlertInfo> dashboardAlerts = new List<ContosoAlertInfo>();
            ContosoAlertCause dummyAlertCause = new ContosoAlertCause();
            ContosoAlertInfo dashboardAlert = new ContosoAlertInfo();

            if (alertList.Count == 0)
            {
                return dashboardAlerts;
            }

            List<string> allNodeKey = GetAllChildren(key);
            // Add key itself as well.
            allNodeKey.Add(key);
            foreach (var nodeKey in allNodeKey)
            {
                ContosoTopologyNode topologyNode = TopologyTable[nodeKey] as ContosoTopologyNode;
                List<ContosoAlert> childAlerts = topologyNode.Alerts;
                if (childAlerts.Count > 0)
                {
                    if (topologyNode.GetType() == typeof (Station))
                    {
                        Station station = topologyNode as Station;
                        // The OPC UA nodes only generate value alerts.
                        foreach (var opcNode in station.NodeList)
                        {
                            foreach (ContosoAlertCause alertCause in Enum.GetValues(dummyAlertCause.GetType()))
                            {
                                if (alertCause == ContosoAlertCause.AlertCauseValueBelowMinimum ||
                                    alertCause == ContosoAlertCause.AlertCauseValueAboveMaximum)
                                {
                                    // Aggregate similar alerts.
                                    List<ContosoAlert> sameCauseAlerts = childAlerts.FindAll(x => (x.Cause == alertCause && x.Key == nodeKey && x.SubKey == opcNode.NodeId && x.Time != DateTime.MinValue));
                                    if (sameCauseAlerts.Count > 0)
                                    {
                                        // Find the newest alert.
                                        long newestTicks = sameCauseAlerts.Max(x => x.Time.Ticks);
                                        ContosoAlert alert = sameCauseAlerts.Find(x => x.Time.Ticks == newestTicks);

                                        // Create alert dashboard info and add it to the dashboard alert list.
                                        dashboardAlert = CreateDashboardAlertInfo(alert, sameCauseAlerts.Count, nodeKey);
                                        dashboardAlerts.Add(dashboardAlert);
                                    }
                                }
                            }
                        }

                        // For the station node we handle performance alerts.
                        foreach (ContosoAlertCause alertCause in Enum.GetValues(dummyAlertCause.GetType()))
                        {
                            if (alertCause == ContosoAlertCause.AlertCauseValueBelowMinimum ||
                                alertCause == ContosoAlertCause.AlertCauseValueAboveMaximum)
                            {
                                continue;
                            }
                            // Aggregate similar alerts.
                            List<ContosoAlert> sameCauseAlerts = childAlerts.FindAll(x => (x.Cause == alertCause && x.Key == nodeKey && x.Time != DateTime.MinValue));
                            if (sameCauseAlerts.Count > 0)
                            {
                                // Find the newest alert.
                                long newestTicks = sameCauseAlerts.Max(x => x.Time.Ticks);
                                ContosoAlert alert = sameCauseAlerts.Find(x => x.Time.Ticks == newestTicks);

                                // Create alert dashboard info and add it to the dashboard alert list.
                                dashboardAlert = CreateDashboardAlertInfo(alert, sameCauseAlerts.Count, nodeKey);
                                dashboardAlerts.Add(dashboardAlert);
                            }
                        }

                    }
                    else
                    {
                        foreach (ContosoAlertCause alertCause in Enum.GetValues(dummyAlertCause.GetType()))
                        {
                            // Aggregate similar alerts.
                            List<ContosoAlert> sameCauseAlerts = childAlerts.FindAll(x => (x.Cause == alertCause && x.Key == nodeKey && x.Time != DateTime.MinValue));
                            if (sameCauseAlerts.Count > 0)
                            {
                                // Find the newest alert.
                                long newestTicks = sameCauseAlerts.Max(x => x.Time.Ticks);
                                ContosoAlert alert = sameCauseAlerts.Find(x => x.Time.Ticks == newestTicks);

                                // Create alert dashboard info and add it to the dashboard alert list.
                                dashboardAlert = CreateDashboardAlertInfo(alert, sameCauseAlerts.Count, nodeKey);
                                dashboardAlerts.Add(dashboardAlert);
                            }
                        }
                    }
                }
            }

            dashboardAlerts.Sort(delegate (ContosoAlertInfo x, ContosoAlertInfo y)
            {
                if (x.Time < y.Time)
                {
                    return 1;
                }
                if (x.Time == y.Time)
                {
                    return 0;
                }
                return -1;
            });
            return dashboardAlerts;
        }

        /// <summary>
        /// Returns information for all children of the given topology node.
        /// This information will be shown in the UX. 
        /// </summary>
        /// <param name="parentKey"></param>
        public List<ContosoChildInfo> GetChildrenInfo(string parentKey)
        {
            List<ContosoChildInfo> childrenInfo = new List<ContosoChildInfo>();

            // Update type of children for the given key.
            ContosoTopologyNode parent = (ContosoTopologyNode)TopologyTable[parentKey];
            string childrenType;
            Type parentType = parent.GetType();
            childrenType = typeof(Factory).ToString();
            if (parentType == typeof(Factory))
            {
                childrenType = typeof(ProductionLine).ToString();
            }
            else if (parentType == typeof(ProductionLine))
            {
                childrenType = typeof(Station).ToString();
            }
            else if (parentType == typeof(Station))
            {
                childrenType = typeof(ContosoOpcUaNode).ToString();
            }

            // Prepare the list with the child objects for the view.
            if (childrenType == typeof(ContosoOpcUaNode).ToString())
            {
                Station station = (Station)TopologyTable[parentKey];
                foreach (ContosoOpcUaNode opcUaNode in station.NodeList)
                {
                    ContosoChildInfo childInfo = new ContosoChildInfo(station.Key, 
                                                                      opcUaNode.NodeId, 
                                                                      opcUaNode.Status.ToString(), 
                                                                      opcUaNode.SymbolicName, 
                                                                      opcUaNode.SymbolicName,
                                                                      station.Location.City, 
                                                                      station.Location.Latitude, 
                                                                      station.Location.Longitude, 
                                                                      opcUaNode.Visible, 
                                                                      opcUaNode.Last.Value.ToString("0.###",CultureInfo.InvariantCulture), 
                                                                      opcUaNode.Units);
                    childrenInfo.Add(childInfo);
                }
            }
            else
            {
                var childrenKeys = ((ContosoTopologyNode)TopologyTable[parentKey]).GetChildren();
                foreach (string key in childrenKeys)
                {
                    if (childrenType == typeof(Factory).ToString())
                    {
                        Factory factory = (Factory)TopologyTable[key];
                        ContosoChildInfo dashboardChild = new ContosoChildInfo(factory.Key, factory.Status.ToString(), factory.Name, factory.Description,
                                                                    factory.Location.City, factory.Location.Latitude, factory.Location.Longitude, true);
                        childrenInfo.Add(dashboardChild);
                    }
                    if (childrenType == typeof(ProductionLine).ToString())
                    {
                        ProductionLine productionLine = (ProductionLine)TopologyTable[key];
                        ContosoChildInfo dashboardChild = new ContosoChildInfo(productionLine.Key, productionLine.Status.ToString(), productionLine.Name, productionLine.Description,
                                                                    productionLine.Location.City, productionLine.Location.Latitude, productionLine.Location.Longitude, true);
                        childrenInfo.Add(dashboardChild);
                    }
                    if (childrenType == typeof(Station).ToString())
                    {
                        Station station = (Station)TopologyTable[key];
                        ContosoChildInfo dashboardChild = new ContosoChildInfo(station.Key, station.Status.ToString(), station.Name, station.Description,
                                                                    station.Location.City, station.Location.Latitude, station.Location.Longitude, true);
                        childrenInfo.Add(dashboardChild);
                    }
                }
            }
            return childrenInfo;
        }

        /// <summary>
        /// Returns the root node of the topology.
        /// </summary>
        public ContosoTopologyNode GetRootNode()
        {
            return this.TopologyRoot as ContosoTopologyNode;
        }

        /// <summary>
        /// Returns a list of all nodes under the given node, with the given type and name.
        /// </summary>
        /// <returns>
        public List<string> GetAllChildren(string key, Type type, string name)
        {
            List<string> allChildren = new List<string>();
            foreach (var child in ((TopologyNode)TopologyTable[key]).GetChildren())
            {
                ContosoTopologyNode node = TopologyTable[child] as ContosoTopologyNode;
                if (node != null)
                {
                    if (node.GetType() == type && node.Name == name)
                    {
                        allChildren.Add(child);
                    }
                    if (((TopologyNode)TopologyTable[child]).ChildrenCount > 0)
                    {
                        allChildren.AddRange(GetAllChildren(child, type, name));
                    }
                }
            }
            return allChildren;
        }

        /// <summary>
        /// Update the OEE and KPI values
        /// </summary>
        public void UpdateAllKPIAndOEEValues(int histogram = 0)
        {
            // topology is updated from bottom to top nodes, add Production Line view
            List<string> orderedlist = GetAllChildren(TopologyRoot.Key, typeof(ProductionLine));
            // add factory view
            orderedlist.AddRange(GetAllChildren(TopologyRoot.Key, typeof(Factory)));
            // add top view
            orderedlist.Add(TopologyRoot.Key);

            bool updateStatus = false;

            // update all, list is already in the right order to process sequentially
            foreach (string item in orderedlist)
            {
                ContosoTopologyNode actualNode = this[item] as ContosoTopologyNode;
                if (actualNode != null)
                {
                    ContosoAggregatedOeeKpiHistogram oeeKpiHistogram = actualNode[histogram];
                    for (int i = 0; i < oeeKpiHistogram.Intervals.Count; i++)
                    {
                        ContosoAggregatedOeeKpiTimeSpan oeeKpiNode = oeeKpiHistogram[i];
                        oeeKpiNode.Reset();

                        List<string> children = actualNode.GetChildren();
                        foreach (string child in children)
                        {
                            // special case for child as OPC UA server
                            Station childStation = this[child] as Station;
                            if (childStation != null)
                            {
                                ContosoAggregatedOeeKpiTimeSpan oeeKpiChild = childStation[histogram, i];
                                // add all Oee and KPI using the metric for stations
                                oeeKpiNode.AddStation(oeeKpiChild);
                                // update alerts for the station
                                ContosoAggregatedOeeKpiHistogram childOeeKpiHistogram = childStation[histogram];
                                if (childOeeKpiHistogram.CheckAlerts)
                                {
                                    UpdateKPIAndOeeAlerts(childStation);
                                }
                            }
                            else
                            {
                                ContosoTopologyNode childNode = this[child] as ContosoTopologyNode;
                                if (childNode != null)
                                {
                                    ContosoAggregatedOeeKpiTimeSpan oeeKpiChild = childNode[histogram, i];
                                    // add all Oee and KPI using the default metric
                                    oeeKpiNode.Add(oeeKpiChild);
                                }
                                else
                                {
                                    continue;
                                }
                            }
                        }
                        if (oeeKpiNode.EndTime > oeeKpiHistogram.EndTime)
                        {
                            oeeKpiHistogram.EndTime = oeeKpiNode.EndTime;
                        }
                    }

                    if (oeeKpiHistogram.CheckAlerts)
                    {
                        // check for alerts
                        UpdateKPIAndOeeAlerts(actualNode);
                        updateStatus = true;
                    }
                }
            }
            if (updateStatus)
            {
                UpdateAllStatusTopology();
            }
        }

        /// <summary>
        /// Update the OEE and KPI values.
        /// </summary>
        public void UpdateAllKPIAndOEEPerfItems()
        {
            // topology is updated from bottom to top nodes, add Production Line view
            List<string> orderedlist = GetAllChildren(TopologyRoot.Key, typeof(ProductionLine));
            // add factory view
            orderedlist.AddRange(GetAllChildren(TopologyRoot.Key, typeof(Factory)));
            // add top view
            orderedlist.Add(TopologyRoot.Key);

            // update all, list is already in the right order to process sequentially
            foreach (string item in orderedlist)
            {
                ContosoTopologyNode actualNode = this[item] as ContosoTopologyNode;
                if (actualNode != null)
                {
                    List<string> children = actualNode.GetChildren();
                    double addedChilds = 0.0;
                    actualNode.PerformanceSettingUpdateReset();

                    foreach (string child in children)
                    {
                        // special case for child as OPC UA server
                        Station station = this[child] as Station;
                        if (station != null)
                        {
                            addedChilds += 1.0;
                            actualNode.PerformanceSettingAddStation(station);
                            continue;
                        }
                        ContosoTopologyNode childNode = this[child] as ContosoTopologyNode;
                        if (childNode != null)
                        {
                            addedChilds += 1.0;
                            actualNode.PerformanceSettingAdd(childNode);
                        }
                    }
                    actualNode.PerformanceSettingUpdateDone(addedChilds);
                }
            }
        }

        /// <summary>
        /// Update status through the topology hierarchy.
        /// </summary>
        public void UpdateAllStatusTopology()
        {
            // topology is updated from bottom to top nodes, add Production Line view
            List<string> orderedlist = GetAllChildren(TopologyRoot.Key, typeof(ProductionLine));
            // add factory view
            orderedlist.AddRange(GetAllChildren(TopologyRoot.Key, typeof(Factory)));
            // add top view
            orderedlist.Add(TopologyRoot.Key);

            foreach (string item in orderedlist)
            {
                ContosoPerformanceStatus status = ContosoPerformanceStatus.Good;
                ContosoTopologyNode actualNode = this[item] as ContosoTopologyNode;

                if (actualNode != null)
                {
                    List<string> children = actualNode.GetChildren();
                    foreach (string child in children)
                    {
                        // special case for child as OPC UA server
                        Station station = this[child] as Station;
                        if (station != null)
                        {
                            foreach(ContosoOpcUaNode node in station.NodeList)
                            {
                                status = node.Status;
                                if (node.Status == ContosoPerformanceStatus.Poor)
                                {
                                    break;
                                }
                            }
                        }
                           
                        ContosoTopologyNode childNode = this[child] as ContosoTopologyNode;
                        if (childNode != null)
                        {
                            if (childNode.Status == ContosoPerformanceStatus.Poor)
                            {
                                status = ContosoPerformanceStatus.Poor;
                                break;
                            }
                        }
                    }
                    actualNode.Status = status;
                }
            }
        }

        /// <summary>
        /// Update the alerts for the specified node.
        /// </summary>
        void UpdateKPIAndOeeAlerts(ContosoTopologyNode node)
        {
            ContosoAggregatedOeeKpiTimeSpan oeeKpi = node.Last;

            node.Status |= UpdateAlert(node, oeeKpi.Kpi1.Kpi, oeeKpi.Kpi1.Time, node.Kpi1PerformanceSetting, ContosoAlertCause.AlertCauseKpi1BelowMinimum, ContosoAlertCause.AlertCauseKpi1AboveMaximum);
            node.Status |= UpdateAlert(node, oeeKpi.Kpi2.Kpi, oeeKpi.Kpi2.Time, node.Kpi2PerformanceSetting, ContosoAlertCause.AlertCauseKpi2BelowMinimum, ContosoAlertCause.AlertCauseKpi2AboveMaximum);
            node.Status |= UpdateAlert(node, node.OeeAvailabilityLast.OeeAvailability, node.OeeAvailabilityLast.Time, node.OeeAvailabilityPerformanceSetting);
            node.Status |= UpdateAlert(node, node.OeePerformanceLast.OeePerformance, node.OeePerformanceLast.Time, node.OeePerformancePerformanceSetting);
            node.Status |= UpdateAlert(node, node.OeeQualityLast.OeeQuality, node.OeeQualityLast.Time, node.OeeQualityPerformanceSetting);
        }

        /// <summary>
        /// Create an alert if conditions are met.
        /// </summary>
        ContosoPerformanceStatus UpdateAlert(
            ContosoTopologyNode node,
            double value,
            DateTime time,
            ContosoPerformanceSetting setting,
            ContosoAlertCause causeMin = ContosoAlertCause.AlertCauseValueBelowMinimum,
            ContosoAlertCause causeMax = ContosoAlertCause.AlertCauseValueAboveMaximum)
        {
            if (value != 0 && value < setting.Minimum)
            {
                ContosoAlert alert = new ContosoAlert(causeMin, node.Key, time);
                node.AddAlert(alert);
                node.Status = ContosoPerformanceStatus.Poor;
            }
            else if (value != 0 && value > setting.Maximum)
            {
                ContosoAlert alert = new ContosoAlert(causeMax, node.Key, time);
                node.AddAlert(alert);
                node.Status = ContosoPerformanceStatus.Poor;
            }
            else
            {
                node.Status = ContosoPerformanceStatus.Good;
            }
            return node.Status;
        }

        /// <summary>
        /// Constants used to populate new factory/production line.
        /// </summary>
        string _newFactoryName = Strings.NewFactoryName;
        string _newProductionLineName = Strings.NewProductionLineName;
        string _newStationName = Strings.NewStationName;
        const string _newFactoryImage = "newfactory.jpg";
        const string _newProductionLineImage = "assembly_floor.jpg";
        const string _newStationImage = "assembly_station.jpg";

        /// <summary>
        /// Get new factory node. Creates new node if new factory doesn't exist.
        /// </summary>
        public ContosoTopologyNode GetOrAddNewFactory()
        {
            List<string> newFactory = GetAllChildren(TopologyRoot.Key, typeof(Factory), _newFactoryName);
            if (newFactory.Count > 0)
            {
                // use existing new factory
                return (ContosoTopologyNode)TopologyTable[newFactory[0]];
            }
            // Add new factory to root
            FactoryDescription factoryDescription = new FactoryDescription();
            factoryDescription.Name = _newFactoryName;
            factoryDescription.Description = _newFactoryName;
            factoryDescription.Image = _newFactoryImage;
            factoryDescription.Guid = Guid.NewGuid().ToString();
            factoryDescription.Location.Latitude = 0;
            factoryDescription.Location.Longitude = 0;
            Factory factory = new Factory(factoryDescription);
            AddChild(TopologyRoot.Key, factory);
            return factory;
        }

        /// <summary>
        /// Get new production line node. Creates new node if new production line doesn't exist.
        /// </summary>
        public ContosoTopologyNode GetOrAddNewProductionLine()
        {
            ContosoTopologyNode newFactory = GetOrAddNewFactory();
            List<string> newProductionLine = GetAllChildren(newFactory.Key, typeof(ProductionLine), _newProductionLineName);
            if (newProductionLine.Count > 0)
            {
                // use existing new production line
                return (ContosoTopologyNode)TopologyTable[newProductionLine[0]];
            }
            // Add new production Line to existing factory
            ProductionLineDescription productionLineDescription = new ProductionLineDescription();
            productionLineDescription.Name = _newProductionLineName;
            productionLineDescription.Description = _newProductionLineName;
            productionLineDescription.Guid = Guid.NewGuid().ToString();
            productionLineDescription.Image = _newProductionLineImage;
            ProductionLine productionLine = new ProductionLine(productionLineDescription);
            productionLine.Location = newFactory.Location;
            AddChild(newFactory.Key, productionLine);
            return productionLine;
        }

        /// <summary>
        /// Add new stations to a new factory with a new production line.
        /// </summary>
        public void AddNewStations(List<string> opcUriList)
        {
            foreach (string opcUri in opcUriList)
            {
                try
                {
                    ContosoTopologyNode newProductionLine = GetOrAddNewProductionLine();
                    StationDescription desc = new StationDescription();
                    desc.Name = opcUri;
                    desc.Description = _newStationName;
                    desc.Image = _newStationImage;
                    desc.OpcUri = opcUri;
                    Station station = new Station(desc);
                    station.Location = newProductionLine.Location;
                    TopologyNode node = station as TopologyNode;
                    AddChild(newProductionLine.Key, node);
                    UpdateAllKPIAndOEEPerfItems();
                }
                catch
                {
                    Trace.TraceError("Failed to add station {0} to topology", opcUri);
                }
            }
        }

        /// <summary>
        /// Gets the OPC UA node for the station with the given key.
        /// </summary>
        public ContosoOpcUaNode GetOpcUaNode(string key, string nodeId)
        {
            ContosoOpcUaServer opcUaServer = TopologyTable[key] as ContosoOpcUaServer;
            if (opcUaServer != null)
            {
                return opcUaServer.GetOpcUaNode(nodeId) as ContosoOpcUaNode;
            }
            return null;
        }

        /// <summary>
        /// Get the Station topology node with the given key.
        /// </summary>
        public Station GetStation(string key)
        {
            return TopologyTable[key] as Station;
        }

        /// <summary>
        /// Create alert information for the UX from the given alert.
        /// </summary>
        private ContosoAlertInfo CreateDashboardAlertInfo(ContosoAlert alert, long sameCauseAlertsCount, string nodeKey)
        {
            ContosoAlertInfo alertInfo = new ContosoAlertInfo();
            ContosoTopologyNode topologyNode = TopologyTable[alert.Key] as ContosoTopologyNode;

            alertInfo.AlertId = alert.AlertId;
            alertInfo.Cause = alert.Cause;
            alertInfo.Occurences = sameCauseAlertsCount;
            List<string> parentList = GetFullHierarchy(nodeKey);
            alertInfo.TopologyDetails = new string[ContosoAlertInfo.MaxTopologyDetailsCount];
            int index = 0;
            if (parentList.Count == 1)
            {
                alertInfo.TopologyDetails[index] = topologyNode.Name;
            }
            else
            {
                parentList.Reverse();
                parentList.RemoveAt(0);
                foreach (var parent in parentList)
                {
                    ContosoTopologyNode parentNode = (ContosoTopologyNode)TopologyTable[parent];
                    alertInfo.TopologyDetails[index++] = parentNode.Name;
                }
                // Check if the alert has been caused by an OPC UA node value.
                if (alert.SubKey != "null")
                {
                    ContosoOpcUaNode contosoOpcUaNode = Startup.Topology.GetOpcUaNode(alert.Key, alert.SubKey);
                    alertInfo.TopologyDetails[index] = contosoOpcUaNode.SymbolicName;
                    alertInfo.Maximum = contosoOpcUaNode.Maximum;
                    alertInfo.Minimum = contosoOpcUaNode.Minimum;
                }
                else
                {
                    switch (alert.Cause)
                    {
                        case ContosoAlertCause.AlertCauseOeeOverallBelowMinimum:
                        case ContosoAlertCause.AlertCauseOeeOverallAboveMaximum:
                            alertInfo.Maximum = topologyNode.OeeOverallPerformanceSetting.Maximum;
                            alertInfo.Minimum = topologyNode.OeeOverallPerformanceSetting.Minimum;
                            break;
                        case ContosoAlertCause.AlertCauseOeeAvailabilityBelowMinimum:
                        case ContosoAlertCause.AlertCauseOeeAvailabilityAboveMaximum:
                            alertInfo.Maximum = topologyNode.OeeAvailabilityPerformanceSetting.Maximum;
                            alertInfo.Minimum = topologyNode.OeeAvailabilityPerformanceSetting.Minimum;
                            break;
                        case ContosoAlertCause.AlertCauseOeePerformanceBelowMinimum:
                        case ContosoAlertCause.AlertCauseOeePerformanceAboveMaximum:
                            alertInfo.Maximum = topologyNode.OeePerformancePerformanceSetting.Maximum;
                            alertInfo.Minimum = topologyNode.OeePerformancePerformanceSetting.Minimum;
                            break;
                        case ContosoAlertCause.AlertCauseOeeQualityBelowMinimum:
                        case ContosoAlertCause.AlertCauseOeeQualityAboveMaximum:
                            alertInfo.Maximum = topologyNode.OeeQualityPerformanceSetting.Maximum;
                            alertInfo.Minimum = topologyNode.OeeQualityPerformanceSetting.Minimum;
                            break;
                        case ContosoAlertCause.AlertCauseKpi1BelowMinimum:
                        case ContosoAlertCause.AlertCauseKpi1AboveMaximum:
                            alertInfo.Maximum = topologyNode.Kpi1PerformanceSetting.Maximum;
                            alertInfo.Minimum = topologyNode.Kpi1PerformanceSetting.Minimum;
                            break;
                        case ContosoAlertCause.AlertCauseKpi2BelowMinimum:
                        case ContosoAlertCause.AlertCauseKpi2AboveMaximum:
                            alertInfo.Maximum = topologyNode.Kpi2PerformanceSetting.Maximum;
                            alertInfo.Minimum = topologyNode.Kpi2PerformanceSetting.Minimum;
                            break;
                    }
                }
            }
            alertInfo.Key = alert.Key;
            alertInfo.SubKey = alert.SubKey;
            alertInfo.UxTime = string.Format("{0}-{1}", alert.Time.ToString("t"), alert.Time.ToString("d"));
            alertInfo.Time = alert.Time;
            alertInfo.Status = alert.Status;
            alertInfo.AlertActionInfo = topologyNode.CreateAlertActionInfo(alert.Cause, alert.SubKey);
            return alertInfo;
        }
    }
}
