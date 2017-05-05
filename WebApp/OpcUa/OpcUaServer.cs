using System.Collections.Generic;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Contoso;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.OpcUa
{
    /// <summary>
    /// Class for a OPC UA node in an OPC UA server.
    /// </summary>
    public class OpcUaNode
    {
        /// <summary>
        /// The OPC UA node Id of this OPC UA node.
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        /// The OPC UA symbolic name for this OPC UA node.
        /// </summary>
        public string SymbolicName { get; set; }

        /// <summary>
        /// Ctor for the OPC UA node..
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="symbolicName"></param>
        public OpcUaNode(string nodeId, string symbolicName)
        {
            NodeId = nodeId;
            SymbolicName = symbolicName;
        }
    }

    /// <summary>
    /// Represents an OPC UA server. It owns all OPC UA entities (like OPC UA NodeId's, ...) exposed by the server.
    /// </summary>
    public class OpcUaServer : ContosoTopologyNode
    {
        /// <summary>
        /// The list of OPC UA nodes in this OPC UA server.
        /// </summary>
        public List<OpcUaNode> NodeList;

        /// <summary>
        /// Creates a new topology node, which is an OPC UA server. A station in the topology is equal to an OPC UA server.
        /// This is the reason why the station description is passed in as a parameter.
        /// </summary>
        /// <param name="uri">The OPC UA URI of this server. This must be unique globally.</param>
        /// <param name="name">The name of the OPC UA server. This is read from the topology configuration file and shown in the UX.</param>
        /// <param name="description">The name of the OPC UA server. This is read from the topology configuration file and shown in the UX.</param>
        /// <param name="stationDescription">Topology description of the station.</param>
        public OpcUaServer(string uri, string name, string description, StationDescription stationDescription) : base(uri, name, description, stationDescription)
        {
            NodeList = new List<OpcUaNode>();
        }

        /// <summary>
        /// Adds an OPC UA node to this OPC UA server.
        /// </summary>
        /// <param name="opcUaNodeId">The OPC UA NodeId for the OPC UA node to add.</param>
        /// <param name="opcUaSymbolicName">The OPC UA symbolic name of the OPC UA node to add.</param>
        public void AddOpcUaServerNode(string opcUaNodeId, string opcUaSymbolicName)
        {
            OpcUaNode opcNodeObject = new OpcUaNode(opcUaNodeId, opcUaSymbolicName);
            NodeList.Add(opcNodeObject);
        }
        /// <summary>
        /// Checks if the OPC UA server has an OPC UA node with the given nodeId.
        /// </summary>
        /// <param name="opcUaNodeId">The OPC UA NodeId to check for.</param>
        /// <returns>The node specified or null.</returns>
        public OpcUaNode GetOpcUaNode(string opcUaNodeId)
        {
            foreach (OpcUaNode opcUaNode in NodeList)
            {
                if (opcUaNode.NodeId == opcUaNodeId)
                {
                    return opcUaNode;
                }
            }
            return null;
        }
    }
}
