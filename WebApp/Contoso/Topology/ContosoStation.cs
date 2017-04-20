using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Contoso
{
    public class StationDescription : ContosoTopologyDescriptionCommon
    {
        [JsonProperty]
        public string OpcUri;

        [JsonProperty]
        public List<ContosoOpcNodeDescription> OpcNodes;

        public StationDescription()
        {
            OpcNodes = new List<ContosoOpcNodeDescription>();
        }
    }

    /// <summary>
    /// Class to define a station in the topology tree.
    /// </summary>
    public class Station : ContosoOpcUaServer
    {
        /// <summary>
        /// Ctor of the node using topology description data.
        /// </summary>
        /// <param name="stationDescription">The topology description for the station.</param>
        public Station(StationDescription stationDescription) : base(stationDescription.OpcUri, stationDescription.Name, stationDescription.Description, stationDescription)
        {
            // List of node relevances.
            List<ContosoPerformanceRelevance> opcUaNodeRelevances = null;

            foreach (var opcNode in stationDescription.OpcNodes)
            {
                // Initialize relevance and alerts.
                opcUaNodeRelevances = null;

                // Initialize relevance of the OPC UA node. This will be used to detect, when dashboard values needs to be udpated.
                if (opcNode.Relevance != null && opcNode.Relevance.Count > 0)
                {
                    opcUaNodeRelevances = new List<ContosoPerformanceRelevance>();
                    ContosoPerformanceRelevance relevance = ContosoPerformanceRelevance.NotRelevant;
                    foreach (var relevanceDescription in opcNode.Relevance)
                    {
                        switch (relevanceDescription)
                        {
                            case "OeeOverall":
                            case "OeeAvailability":
                            case "OeePerformance":
                            case "OeeQuality":
                                throw new Exception(string.Format("The node '{0}' in Station wit URI '{1}' shows '{2}' relevance. This is not allowed since this tag is used for internal processing only. Pllease change.", opcNode.NodeId, Key, relevanceDescription));

                            case "OeeAvailability_Running":
                            case "OeeAvailability_Fault":
                            case "OeeAvailability_Station_Running":
                            case "OeeAvailability_Station_Fault":
                                if (stationDescription.OeeAvailability == null)
                                {
                                    throw new Exception(string.Format("The node '{0}' in Station wit URI '{1}' shows '{2}' relevance, but there are no performance settings for 'OeeAvailability' defined for the Station. Please change.", opcNode.NodeId, Key, relevanceDescription));
                                }
                                goto case "SetRelevanceOperation";

                            case "OeePerformance_Ideal":
                            case "OeePerformance_Actual":
                                if (stationDescription.OeePerformance == null)
                                {
                                    throw new Exception(string.Format("The node '{0}' in Station wit URI '{1}' shows '{2}' relevance, but there are no performance settings for 'OeePerformance' defined for the Station. Please change.", opcNode.NodeId, Key, relevanceDescription));
                                }
                                goto case "SetRelevanceOperation";

                            case "OeeQuality_Good":
                            case "OeeQuality_Bad":
                            case "OeeQuality_Station_Good":
                            case "OeeQuality_Station_Bad":
                                if (stationDescription.OeeQuality == null)
                                {
                                    throw new Exception(string.Format("The node '{0}' in Station wit URI '{1}' shows '{2}' relevance, but there are no performance settings for 'OeeQuality' defined for the Station. Please change.", opcNode.NodeId, Key, relevanceDescription));
                                }
                                goto case "SetRelevanceOperation";

                            case "Kpi1":
                            case "Kpi1_Station":
                                if (stationDescription.Kpi1 == null)
                                {
                                    throw new Exception(string.Format("The node '{0}' in Station wit URI '{1}' shows '{2}' relevance, but there are no performance settings for 'Kpi1' defined for the Station. Please change.", opcNode.NodeId, Key, relevanceDescription));
                                }
                                goto case "SetRelevanceOperation";

                            case "Kpi2":
                                if (stationDescription.Kpi2 == null)
                                {
                                    throw new Exception(string.Format("The node '{0}' in Station wit URI '{1}' shows '{2}' relevance, but there are no performance settings for 'Kpi2' defined for the Station. Please change.", opcNode.NodeId, Key, relevanceDescription));
                                }
                                goto case "SetRelevanceOperation";

                            default:
                                throw new Exception(string.Format("The 'Relevance' value '{0}' in node '{1}' of Station with URI '{2}' is unknown. Please change.", relevanceDescription, opcNode.NodeId, Key));

                            case "SetRelevanceOperation":
                                if (Enum.TryParse(relevanceDescription, out relevance))
                                {
                                    opcUaNodeRelevances.Add(relevance);
                                }
                                break;

                        }
                    }
                }
                // Add the OPC UA node to this station.
                AddOpcServerNode(opcNode, opcUaNodeRelevances);
            }
        }
    }
}
