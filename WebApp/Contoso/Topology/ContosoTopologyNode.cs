using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Topology;
using static Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.RDX.RDXUtils;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Contoso
{
    /// <summary>
    /// Class to define a Contoso specific node in the topology. Each Contoso node has a status, name and description, as well
    /// as certain performance targets and telemetry time series for OEE/KPI.
    /// </summary>
    public class ContosoTopologyNode : TopologyNode
    {
        /// <summary>
        /// Specify the aggregation views
        /// </summary>
        public enum AggregationView
        {
            Last = 0,
            Hour = 1,
            Day = 2,
            Week = 3
        }

        /// <summary>
        /// Default image for the node if nothing is configured.
        /// </summary>
        private string _defaultImage = "microsoft.jpg";

        /// <summary>
        /// Status of the topology node.
        /// </summary>
        public ContosoPerformanceStatus Status { get; set; }

        /// <summary>
        /// Name of the topology node.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of the topology node.
        /// </summary>
        public string Description { get; set; }

        public FactoryLocation Location { get; set; }

        /// <summary>
        /// Path to an image showing this topology node.
        /// </summary>
        public string ImagePath { get; set; }

        /// <summary>
        /// OEE Availability definition of the topology node.
        /// </summary>
        public ContosoPerformanceSetting OeeAvailabilityPerformanceSetting { get; set; }

        /// <summary>
        /// OEE Performance definition of the topology node.
        /// </summary>
        public ContosoPerformanceSetting OeePerformancePerformanceSetting { get; set; }

        /// <summary>
        /// OEE Quality definition of the topology node.
        /// </summary>
        public ContosoPerformanceSetting OeeQualityPerformanceSetting { get; set; }

        /// <summary>
        /// OEE Overall definition of the topology node.
        /// </summary>
        public ContosoPerformanceSetting OeeOverallPerformanceSetting { get; set; }

        /// <summary>
        /// KPI1 definition of the topology node.
        /// </summary>
        public ContosoPerformanceSetting Kpi1PerformanceSetting { get; set; }

        /// <summary>
        /// KPI2 definition of the topology node.
        /// </summary>
        public ContosoPerformanceSetting Kpi2PerformanceSetting { get; set; }

        /// <summary>
        /// Reset all performance settings for calculations.
        /// </summary>
        public void PerformanceSettingUpdateReset()
        {
            Kpi1PerformanceSetting.Reset();
            Kpi2PerformanceSetting.Reset();
            OeeAvailabilityPerformanceSetting.Reset();
            OeeOverallPerformanceSetting.Reset();
            OeePerformancePerformanceSetting.Reset();
            OeeQualityPerformanceSetting.Reset();
        }

        /// <summary>
        /// End of performance setting update.
        /// </summary>
        public void PerformanceSettingUpdateDone(double addedChilds)
        {
            Kpi1PerformanceSetting.Done(addedChilds);
            Kpi2PerformanceSetting.Done(addedChilds);
            OeeAvailabilityPerformanceSetting.Done(addedChilds);
            OeeOverallPerformanceSetting.Done(addedChilds);
            OeePerformancePerformanceSetting.Done(addedChilds);
            OeeQualityPerformanceSetting.Done(addedChilds);
        }

        /// <summary>
        /// Add performance setting of a station.
        /// </summary>
        public void PerformanceSettingAddStation(ContosoTopologyNode childNode)
        {
            Kpi1PerformanceSetting.AddChildStation(childNode.Kpi1PerformanceSetting);
            Kpi2PerformanceSetting.AddChildStation(childNode.Kpi2PerformanceSetting);
            OeeAvailabilityPerformanceSetting.AddChildStation(childNode.OeeAvailabilityPerformanceSetting);
            OeeOverallPerformanceSetting.AddChildStation(childNode.OeeOverallPerformanceSetting);
            OeePerformancePerformanceSetting.AddChildStation(childNode.OeePerformancePerformanceSetting);
            OeeQualityPerformanceSetting.AddChildStation(childNode.OeeQualityPerformanceSetting);
        }

        /// <summary>
        /// Add performance setting of a node.
        /// </summary>
        public void PerformanceSettingAdd(ContosoTopologyNode childNode)
        {
            Kpi1PerformanceSetting.AddChild(childNode.Kpi1PerformanceSetting);
            Kpi2PerformanceSetting.AddChild(childNode.Kpi2PerformanceSetting);
            OeeAvailabilityPerformanceSetting.AddChild(childNode.OeeAvailabilityPerformanceSetting);
            OeeOverallPerformanceSetting.AddChild(childNode.OeeOverallPerformanceSetting);
            OeePerformancePerformanceSetting.AddChild(childNode.OeePerformancePerformanceSetting);
            OeeQualityPerformanceSetting.AddChild(childNode.OeeQualityPerformanceSetting);
        }

        /// <summary>
        /// List of OEE and KPI aggregations.
        /// </summary>
        public List<ContosoAggregatedOeeKpiHistogram> Aggregations;

        /// <summary>
        /// Object to lock accesses to Alerts list.
        /// </summary>
        private Object _alertsLock;

        /// <summary>
        /// Alerts in this topology node.
        /// </summary>
        public List<ContosoAlert> Alerts;

        /// <summary>
        /// Ctor for the topology node using values.
        /// </summary>
        public ContosoTopologyNode(string key, string name, string description) : base(key)
        {
            Name = name;
            Description = description;
            ImagePath = "/Content/img/" + _defaultImage;
        }

        /// <summary>
        /// Ctor for a topology node in the topology using the topology node description.
        /// </summary>
        public ContosoTopologyNode(string key, string name, string description, ContosoTopologyDescriptionCommon topologyDescription) : this(key, name, description)
        {
            if (topologyDescription == null)
            {
                throw new ArgumentException("topologyDescription must be a non-null value");
            }

            // Initialize all OEE/KPI definitions and time series.
            if (topologyDescription.OeeOverall != null)
            {
                OeeOverallPerformanceSetting = new ContosoPerformanceSetting(ContosoPerformanceSettingAggregator.Percent, key, topologyDescription.OeeOverall);
            }
            if (topologyDescription.OeePerformance != null)
            {
                OeePerformancePerformanceSetting = new ContosoPerformanceSetting(ContosoPerformanceSettingAggregator.Percent, key, topologyDescription.OeePerformance);
            }
            if (topologyDescription.OeeAvailability != null)
            {
                OeeAvailabilityPerformanceSetting = new ContosoPerformanceSetting(ContosoPerformanceSettingAggregator.Percent, key, topologyDescription.OeeAvailability);
            }
            if (topologyDescription.OeeQuality != null)
            {
                OeeQualityPerformanceSetting = new ContosoPerformanceSetting(ContosoPerformanceSettingAggregator.Percent, key, topologyDescription.OeeQuality);
            }
            if (topologyDescription.Kpi1 != null)
            {
                Kpi1PerformanceSetting = new ContosoPerformanceSetting(ContosoPerformanceSettingAggregator.MinStation, key, topologyDescription.Kpi1);
            }
            if (topologyDescription.Kpi2 != null)
            {
                Kpi2PerformanceSetting = new ContosoPerformanceSetting(ContosoPerformanceSettingAggregator.Add, key, topologyDescription.Kpi2);
            }

            // Create all aggregated OEE and KPI data structures
            // Access of the specific views is hardcoded in AggregationView
            Aggregations = new List<ContosoAggregatedOeeKpiHistogram>();
            // this is the 'Last' aggregation used for the dashboard and has just one timespan
            Aggregations.Add(new ContosoAggregatedOeeKpiHistogram(TimeSpan.FromMinutes(60), 1, TimeSpan.FromSeconds(10), false, true, true, true));
            // all other aggregations are used for the time series and span an hour, a day and a week
            Aggregations.Add(new ContosoAggregatedOeeKpiHistogram(TimeSpan.FromMinutes(5), 12, TimeSpan.FromMinutes(5), true));
            Aggregations.Add(new ContosoAggregatedOeeKpiHistogram(TimeSpan.FromHours(1), 24, TimeSpan.FromHours(1), true));
            Aggregations.Add(new ContosoAggregatedOeeKpiHistogram(TimeSpan.FromDays(1), 7, TimeSpan.FromDays(1), true));

            // Create alert list.
            Alerts = new List<ContosoAlert>();
            _alertsLock = new Object();

            // Initialize image path.
            if (topologyDescription.Image != null && topologyDescription.Image != "")
            {
                ImagePath = "/Content/img/" + topologyDescription.Image;
            }
        }

        /// <summary>
        /// Checks if the node has alerts.
        /// </summary>
        public bool HasAlerts
        {
            get
            {
                return Alerts.Count == 0 ? false : true;
            }
        }

        /// <summary>
        /// The alert with the given Id will be set to status acknowledged.
        /// </summary>
        public bool AcknowledgeAlert(long alertId)
        {
            // Acknowledge the alert.
            lock (_alertsLock)
            {
                {
                    foreach (var alert in Alerts)
                    {
                        if (alert.AlertId == alertId)
                        {
                            alert.Status = ContosoAlertStatus.AlertStatusAcknowledged;
                            return true;
                        }
                    }
                }
            }
            // The alert was not found.
            Trace.TraceError($"alertId '{alertId}' in node '{Key}' could not be found.");
            return false;
        }


        /// <summary>
        /// The all alerts, which have the same cause and are older then the given one are deleted.
        /// </summary>
        public bool CloseAlert(long alertId)
        {
            lock (_alertsLock)
            {
                // Verify existence of the alertId.
                ContosoAlert alert = Alerts.Find(x => x.AlertId == alertId);
                if (alert == null)
                {
                    Trace.TraceError($"alertId '{alertId}' in node '{Key}' could not be found.");
                    return false;
                }

                // Fail if the alert is not yet acknowledged.
                if (alert.Status != ContosoAlertStatus.AlertStatusAcknowledged)
                {
                    Trace.TraceError($"alert with alertId '{alertId}' in node '{Key}' could not be closed, since it is not acknowledged ('{alert.Status}')");
                    return false;
                }

                // Close all older alerts.
                List<ContosoAlert> oldAlerts = Alerts.FindAll(x => (x.Time <= alert.Time && x.Cause == alert.Cause));
                foreach (var alertToRemove in oldAlerts)
                {
                    Alerts.RemoveAll(x => x == alertToRemove);
                }
            }
            return true;
        }

        /// <summary>
        /// The alert is added to the alert list of this node.
        /// </summary>
        public void AddAlert(ContosoAlert alert)
        {
            lock (_alertsLock)
            {
                Alerts.Add(alert);
            }
        }

        /// <summary>
        /// The alert is added to the alert list of this node.
        /// </summary>
        public ContosoAlert FindAlert(long alertId)
        {
            ContosoAlert alert;
            lock (_alertsLock)
            {
                alert = Alerts.Find(x => x.AlertId == alertId);
            }
            return alert;
        }

        /// <summary>
        /// Creates the info for alert actions, which is sent to the client.
        /// </summary>
        public List<ContosoAlertActionInfo> CreateAlertActionInfo(ContosoAlertCause alertCause, string subKey)
        {
            // Get the alert actions.
            List<ContosoAlertActionDefinition> alertActionDefinitions = GetAlertActions(alertCause, subKey);
            if (alertActionDefinitions == null)
            {
                return null;
            }

            // Create the alert info list.
            long id = 0;
            return alertActionDefinitions.Select(actionDefinition => new ContosoAlertActionInfo(id++, actionDefinition.Type, actionDefinition.Description)).ToList();
        }

        /// <summary>
        /// Get the alert actions definitions for the given alert cause..
        /// </summary>
        public List<ContosoAlertActionDefinition> GetAlertActions(ContosoAlertCause alertCause, string subKey)
        {
            List<ContosoAlertActionDefinition> alertActionDefinitions = null;

            // Handle topology nodes if no subKey (OPC UA nodeid) is given.
            if (string.IsNullOrEmpty(subKey) || subKey.Equals("null"))
            {
                // Choose actions.
                switch (alertCause)
                {
                    case ContosoAlertCause.AlertCauseOeeOverallBelowMinimum:
                        alertActionDefinitions = OeeOverallPerformanceSetting.MinimumAlertActions;
                        break;
                    case ContosoAlertCause.AlertCauseOeeOverallAboveMaximum:
                        alertActionDefinitions = OeeOverallPerformanceSetting.MaximumAlertActions;
                        break;
                    case ContosoAlertCause.AlertCauseOeeAvailabilityBelowMinimum:
                        alertActionDefinitions = OeeAvailabilityPerformanceSetting.MinimumAlertActions;
                        break;
                    case ContosoAlertCause.AlertCauseOeeAvailabilityAboveMaximum:
                        alertActionDefinitions = OeeAvailabilityPerformanceSetting.MaximumAlertActions;
                        break;
                    case ContosoAlertCause.AlertCauseOeePerformanceBelowMinimum:
                        alertActionDefinitions = OeePerformancePerformanceSetting.MinimumAlertActions;
                        break;
                    case ContosoAlertCause.AlertCauseOeePerformanceAboveMaximum:
                        alertActionDefinitions = OeePerformancePerformanceSetting.MaximumAlertActions;
                        break;
                    case ContosoAlertCause.AlertCauseOeeQualityBelowMinimum:
                        alertActionDefinitions = OeeQualityPerformanceSetting.MinimumAlertActions;
                        break;
                    case ContosoAlertCause.AlertCauseOeeQualityAboveMaximum:
                        alertActionDefinitions = OeeQualityPerformanceSetting.MaximumAlertActions;
                        break;
                    case ContosoAlertCause.AlertCauseKpi1BelowMinimum:
                        alertActionDefinitions = Kpi1PerformanceSetting.MinimumAlertActions;
                        break;
                    case ContosoAlertCause.AlertCauseKpi1AboveMaximum:
                        alertActionDefinitions = Kpi1PerformanceSetting.MaximumAlertActions;
                        break;
                    case ContosoAlertCause.AlertCauseKpi2BelowMinimum:
                        alertActionDefinitions = Kpi2PerformanceSetting.MinimumAlertActions;
                        break;
                    case ContosoAlertCause.AlertCauseKpi2AboveMaximum:
                        alertActionDefinitions = Kpi2PerformanceSetting.MaximumAlertActions;
                        break;
                }
            }
            else
            {
                Station station = this as Station;
                ContosoOpcUaNode opcUaNode = station.NodeList.Find(node => node.NodeId == subKey) as ContosoOpcUaNode;
                switch (alertCause)
                {
                    case ContosoAlertCause.AlertCauseValueBelowMinimum:
                        alertActionDefinitions = opcUaNode.MinimumAlertActions;
                        break;
                    case ContosoAlertCause.AlertCauseValueAboveMaximum:
                        alertActionDefinitions = opcUaNode.MaximumAlertActions;
                        break;
                }
            }

            return alertActionDefinitions;
        }

        /// <summary>
        /// Aggregations can be accessed by index
        /// </summary>
        public ContosoAggregatedOeeKpiHistogram this[int i]
        {
            get { return Aggregations[i]; }
        }

        /// <summary>
        /// Fields in aggregations can be accessed by index
        /// </summary>
        public ContosoAggregatedOeeKpiTimeSpan this[int i, int v]
        {
            get { return Aggregations[i].Intervals[v]; }
        }

        /// <summary>
        /// Aggregations [0,0] is for the dashboard and can be accessed directly
        /// </summary>
        public ContosoAggregatedOeeKpiTimeSpan Last { get { return Aggregations[0].Intervals[0]; } }
        public ContosoKpi1Data Kpi1Last { get { return Last.Kpi1; } }
        public ContosoKpi2Data Kpi2Last { get { return Last.Kpi2; } }
        public ContosoOeeOverallData OeeOverallLast { get { return Last.OeeOverall; } }
        public ContosoOeeAvailabilityData OeeAvailabilityLast { get { return Last.OeeAvailability; } }
        public ContosoOeePerformanceData OeePerformanceLast { get { return Last.OeePerformance; } }
        public ContosoOeeQualityData OeeQualityLast { get { return Last.OeeQuality; } }

        /// <summary>
        /// Return the time series for a relevance
        /// </summary>
        public AggregatedTimeSeriesResult AggregatedOeeKpiTimeSeries(AggregationView aggregationView, ContosoPerformanceRelevance relevance, TimeSpan scaledTimeSpan)
        {
            int aggregateIndex = (int)aggregationView;
            int count = Aggregations[aggregateIndex].Intervals.Count;
            AggregatedTimeSeriesResult result = new AggregatedTimeSeriesResult(count, Aggregations[aggregateIndex].EndTime, Aggregations[aggregateIndex].IntervalTimeSpan);
            double scaleFactor = 100.0 * OeeKpiScaleFactor(relevance, Aggregations[aggregateIndex].IntervalTimeSpan, scaledTimeSpan);

            for (int i = 0; i < count; i++)
            {
                result.YValues[count - i - 1] = Math.Round(scaleFactor * GetData(Aggregations[aggregateIndex].Intervals[i], relevance).Value) / 100.0;
            }
            return result;
        }

        /// <summary>
        /// Get the relevant Oee Kpi data item.
        /// </summary>
        private ContosoDataItem GetData(ContosoAggregatedOeeKpiTimeSpan oeeKpi, ContosoPerformanceRelevance relevance)
        {
            switch (relevance)
            {
                case ContosoPerformanceRelevance.Kpi1:
                    return oeeKpi.Kpi1;
                case ContosoPerformanceRelevance.Kpi2:
                    return oeeKpi.Kpi2;
                case ContosoPerformanceRelevance.OeeQuality:
                    return oeeKpi.OeeQuality;
                case ContosoPerformanceRelevance.OeePerformance:
                    return oeeKpi.OeePerformance;
                case ContosoPerformanceRelevance.OeeAvailability:
                    return oeeKpi.OeeAvailability;
                case ContosoPerformanceRelevance.OeeOverall:
                    return oeeKpi.OeeOverall;
            }
            throw new Exception("Invalid Performance Relevance");
        }

        /// <summary>
        /// Return the Oee Kpi scale factor to the display time span.
        /// Oee are percentage values and do not require scaling.
        /// Kpi are scaled to common time span.
        /// </summary>
        private double OeeKpiScaleFactor(ContosoPerformanceRelevance relevance, TimeSpan interval, TimeSpan scaledInterval)
        {
            switch (relevance)
            {
                case ContosoPerformanceRelevance.Kpi1:
                case ContosoPerformanceRelevance.Kpi2:
                    return scaledInterval.TotalSeconds / interval.TotalSeconds;
                case ContosoPerformanceRelevance.OeeQuality:
                case ContosoPerformanceRelevance.OeePerformance:
                case ContosoPerformanceRelevance.OeeAvailability:
                case ContosoPerformanceRelevance.OeeOverall:
                    return 1.0;
            }
            throw new Exception("Invalid Performance Relevance");
        }


    }
}
