using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.ComponentModel;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.OpcUa;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Contoso
{
    /// <summary>
    /// Contoso related base information for query
    /// </summary>
    public class ContosoOeeKpiOpCodeQueryInfo
    {
        public string AppUri { get; }
        public ContosoAggregatedOeeKpiTimeSpan TopologyNode { get; }

        public ContosoOeeKpiOpCodeQueryInfo(string appUri, ContosoAggregatedOeeKpiTimeSpan topologyNode)
        {
            AppUri = appUri;
            TopologyNode = topologyNode;
        }
    }

    /// <summary>
    /// Operation for queries of node values.
    /// </summary>
    [JsonConverter(typeof(Newtonsoft.Json.Conver‌​ters.StringEnumConve‌​rter))]
    public enum ContosoOpcNodeOpCode
    {
        Undefined = 0,
        Diff = 1,
        Avg = 2,
        Sum = 3,
        Last = 4,
        Count = 5,
        Max = 6,
        Min = 7,
        Const = 8,
        Nop = 9,
        SubMaxMin = 10,
        Timespan = 11
    };

    /// <summary>
    /// Class to parse the Contoso specific OPC UA node description.
    /// </summary>
    public class ContosoOpcNodeDescription
    {
        [JsonProperty]
        public string NodeId;

        [JsonProperty]
        public string SymbolicName;

        [JsonProperty]
        public List<string> Relevance;

        [DefaultValue(ContosoOpcNodeOpCode.Undefined)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public ContosoOpcNodeOpCode OpCode;

        [JsonProperty]
        public string Units;

        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool Visible;

        [JsonProperty]
        public double? ConstValue;

        [JsonProperty]
        public double? Minimum;

        [JsonProperty]
        public double? Maximum;

        [JsonProperty]
        public List<ContosoAlertActionDescription> MinimumAlertActions;

        [JsonProperty]
        public List<ContosoAlertActionDescription> MaximumAlertActions;
    }

    /// <summary>
    /// Class for Contoso OPC UA node information.
    /// </summary>
    public class ContosoOpcUaNode : OpcUaNode
    {
        /// <summary>
        /// This list defines for which performance parameters this node is relevant for.
        /// A node with relevance for the OEE/KPI calulations is regularly updated
        /// with new data.
        /// </summary>
        public List<ContosoPerformanceRelevance> Relevance { get; set; }

        /// <summary>
        /// The last value obtained by a query.
        /// </summary>
        public ContosoDataItem Last;

        /// <summary>
        /// Physical unit for value, optional.
        /// </summary>
        public string Units { get; }

        /// <summary>
        /// Tag if node should be visible in Dashboard.
        /// </summary>
        public bool Visible { get; }

        /// <summary>
        /// Specifies the operation in the query to get the value of the node.
        /// </summary>
        public ContosoOpcNodeOpCode OpCode { get; set; }

        /// <summary>
        /// Status of the OPC UA Node.
        /// </summary>
        public ContosoPerformanceStatus Status { get; set; }

        /// <summary>
        /// Const Value if OpCode Const is chosen.
        /// </summary>
        public double? ConstValue { get; set; }

        /// <summary>
        /// If the actual value falls below this value, an alert is generated.
        /// </summary>
        public double? Minimum { get; set; }

        /// <summary>
        /// If the actual value raises above Maximum, an alert is created.
        /// </summary>
        public double? Maximum { get; set; }

        /// <summary>
        /// Defines the actions a user could execute as reaction for a minimum alert.
        /// </summary>
        public List<ContosoAlertActionDefinition> MinimumAlertActions;

        /// <summary>
        /// Defines the actions a user could execute as reaction for a maximum alert.
        /// </summary>
        public List<ContosoAlertActionDefinition> MaximumAlertActions;

        /// <summary>
        /// Ctor for a Contoso OPC UA node, specifying alert related information.
        /// </summary>
        public ContosoOpcUaNode(
            string opcUaNodeId,
            string opcUaSymbolicName,
            List<ContosoPerformanceRelevance> opcUaNodeRelevance,
            ContosoOpcNodeOpCode opCode,
            string units,
            bool visible,
            double? constValue,
            double? minimum,
            double? maximum,
            List<ContosoAlertActionDefinition> minimumAlertActionDefinitions,
            List<ContosoAlertActionDefinition> maximumlertActionDefinitions
            )
            : base(opcUaNodeId, opcUaSymbolicName)
        {
            Relevance = opcUaNodeRelevance;
            OpCode = opCode;
            Units = units;
            Visible = visible;
            ConstValue = constValue;
            Minimum = minimum;
            Maximum = maximum;
            MinimumAlertActions = minimumAlertActionDefinitions;
            MaximumAlertActions = maximumlertActionDefinitions;
            Last = new ContosoDataItem();
        }

        /// <summary>
        /// Ctor for a Contoso OPC UA node, using alert related descriptions.
        /// </summary>

        public ContosoOpcUaNode(
            string opcUaNodeId,
            string opcUaSymbolicName,
            List<ContosoPerformanceRelevance> opcUaNodeRelevance,
            ContosoOpcNodeDescription opcNodeDescription)
            : base(opcUaNodeId, opcUaSymbolicName)
        {
            Relevance = opcUaNodeRelevance;
            OpCode = opcNodeDescription.OpCode;
            Units = opcNodeDescription.Units;
            Visible = opcNodeDescription.Visible;
            ConstValue = opcNodeDescription.ConstValue;
            Minimum = opcNodeDescription.Minimum;
            Maximum = opcNodeDescription.Maximum;

            MinimumAlertActions = new List<ContosoAlertActionDefinition>();
            MinimumAlertActions.AddRange(ContosoAlertActionDefinition.Init(opcNodeDescription.MinimumAlertActions));
            MaximumAlertActions = new List<ContosoAlertActionDefinition>();
            MaximumAlertActions.AddRange(ContosoAlertActionDefinition.Init(opcNodeDescription.MaximumAlertActions));
            Last = new ContosoDataItem();
        }

        /// <summary>
        /// Callback for a query to update the relevance data of a topology node.
        /// </summary>
        public void UpdateRelevanceItem(ContosoAggregatedOeeKpiTimeSpan topologyNode, ContosoPerformanceRelevance relevance, ContosoDataItem data)
        {
            switch (relevance)
            {
                case ContosoPerformanceRelevance.Kpi2:
                case ContosoPerformanceRelevance.Kpi2_Station:
                    topologyNode.Kpi2.Kpi = data.Value;
                    topologyNode.Kpi2.Time = data.Time;
                    break;
                case ContosoPerformanceRelevance.Kpi1:
                case ContosoPerformanceRelevance.Kpi1_Station:
                    topologyNode.Kpi1.Kpi = data.Value;
                    topologyNode.Kpi1.Time = data.Time;
                    break;
                case ContosoPerformanceRelevance.OeeQuality_Bad:
                    topologyNode.OeeQuality.Bad = data.Value;
                    topologyNode.OeeQuality.Time = new DateTime(Math.Max(topologyNode.OeeQuality.Time.Ticks, data.Time.Ticks), DateTimeKind.Utc);
                    topologyNode.OeeOverall.Quality = topologyNode.OeeQuality.OeeQuality;
                    break;
                case ContosoPerformanceRelevance.OeeQuality_Station_Good:
                case ContosoPerformanceRelevance.OeeQuality_Good:
                    topologyNode.OeeQuality.Good = data.Value;
                    topologyNode.OeeQuality.Time = new DateTime(Math.Max(topologyNode.OeeQuality.Time.Ticks, data.Time.Ticks), DateTimeKind.Utc);
                    topologyNode.OeeOverall.Quality = topologyNode.OeeQuality.OeeQuality;
                    break;
                case ContosoPerformanceRelevance.OeePerformance_Ideal:
                    topologyNode.OeePerformance.IdealCycleTime = data.Value;
                    topologyNode.OeePerformance.Time = new DateTime(Math.Max(topologyNode.OeePerformance.Time.Ticks, data.Time.Ticks), DateTimeKind.Utc);
                    topologyNode.OeeOverall.Performance = topologyNode.OeePerformance.OeePerformance;
                    break;
                case ContosoPerformanceRelevance.OeePerformance_Actual:
                    topologyNode.OeePerformance.ActualCycleTime = data.Value;
                    topologyNode.OeePerformance.Time = new DateTime(Math.Max(topologyNode.OeePerformance.Time.Ticks, data.Time.Ticks), DateTimeKind.Utc);
                    topologyNode.OeeOverall.Performance = topologyNode.OeePerformance.OeePerformance;
                    break;
                case ContosoPerformanceRelevance.OeeAvailability_Running:
                    topologyNode.OeeAvailability.OverallRunningTime = data.Value;
                    topologyNode.OeeAvailability.Time = new DateTime(Math.Max(topologyNode.OeeAvailability.Time.Ticks, data.Time.Ticks), DateTimeKind.Utc);
                    topologyNode.OeeOverall.Availability = topologyNode.OeeAvailability.OeeAvailability;
                    break;
                case ContosoPerformanceRelevance.OeeAvailability_Fault:
                    topologyNode.OeeAvailability.OverallFaultTime = data.Value;
                    topologyNode.OeeAvailability.Time = new DateTime(Math.Max(topologyNode.OeeAvailability.Time.Ticks, data.Time.Ticks), DateTimeKind.Utc);
                    topologyNode.OeeOverall.Availability = topologyNode.OeeAvailability.OeeAvailability;
                    break;
            }
        }

        /// <summary>
        /// Update all relevance parameters after a node value update.
        /// </summary>
        public void UpdateRelevance(ContosoAggregatedOeeKpiTimeSpan topologyNode)
        {
            if (Relevance != null)
            {
                foreach (ContosoPerformanceRelevance relevance in Relevance)
                {
                    UpdateRelevanceItem(topologyNode, relevance, Last);
                }
            }
        }
    }

    /// <summary>
    /// Class for Contoso OPC UA server information.
    /// </summary>
    public class ContosoOpcUaServer : OpcUaServer
    {
        /// <summary>
        /// Ctor for Contoso OPC UA server using the station description.
        /// </summary>
        public ContosoOpcUaServer(
            string uri,
            string name,
            string description,
            StationDescription stationDescription)
            : base(uri, name, description, stationDescription)
        {
        }

        /// <summary>
        /// Adds an OPC UA Node to this OPC UA server topology node.
        /// </summary>
        public void AddOpcServerNode(
            string opcUaNodeId,
            string opcUaSymbolicName,
            List<ContosoPerformanceRelevance> opcUaNodeRelevance,
            ContosoOpcNodeOpCode opCode,
            string units,
            bool visible,
            double? constValue,
            double? minimum,
            double? maximum,
            List<ContosoAlertActionDefinition> minimumAlertActionDefinitions,
            List<ContosoAlertActionDefinition> maximumAlertActionDefinitions)
        {
            foreach (var node in NodeList)
            {
                if (OpCodeRequiresOpcUaNode(opCode) &&
                    node.NodeId == opcUaNodeId
                    )
                {
                    throw new Exception(string.Format("The OPC UA node with NodeId '{0}' and SymbolicName '{1}' does already exist. Please change.", opcUaNodeId, opcUaSymbolicName));
                }
            }
            ContosoOpcUaNode opcUaNodeObject = new ContosoOpcUaNode(
                opcUaNodeId,
                opcUaSymbolicName,
                opcUaNodeRelevance,
                opCode,
                units,
                visible,
                constValue,
                minimum,
                maximum,
                minimumAlertActionDefinitions,
                maximumAlertActionDefinitions);

            NodeList.Add(opcUaNodeObject);
        }

        /// <summary>
        /// Adds an OPC UA Node to this OPC UA server topology node using the OPC UA node description.
        /// </summary>
        public void AddOpcServerNode(ContosoOpcNodeDescription opcUaNodeDescription, List<ContosoPerformanceRelevance> opcUaNodeRelevance)
        {
            foreach (var node in NodeList)
            {
                if (OpCodeRequiresOpcUaNode(opcUaNodeDescription.OpCode) &&
                    node.NodeId == opcUaNodeDescription.NodeId
                    )
                {
                    throw new Exception(string.Format("The OPC UA node with NodeId '{0}' and SymbolicName '{1}' does already exist. Please change.",
                        opcUaNodeDescription.NodeId, opcUaNodeDescription.SymbolicName));
                }
            }
            ContosoOpcUaNode opcUaNodeObject = new ContosoOpcUaNode(
                opcUaNodeDescription.NodeId,
                opcUaNodeDescription.SymbolicName,
                opcUaNodeRelevance,
                opcUaNodeDescription);

            NodeList.Add(opcUaNodeObject);
        }

        /// <summary>
        /// Test if opcode requires a unique OPC UA Node Id.
        /// </summary>
        private static bool OpCodeRequiresOpcUaNode(ContosoOpcNodeOpCode opCode)
        {
            switch (opCode)
            {
                case ContosoOpcNodeOpCode.Const:
                case ContosoOpcNodeOpCode.Timespan:
                case ContosoOpcNodeOpCode.Nop:
                    return false;
            }
            return true;
        }
    }
}
