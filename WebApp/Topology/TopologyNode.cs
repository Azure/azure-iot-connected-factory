using System;
using System.Collections.Generic;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Topology
{
    /// <summary>
    /// 
    /// Represents a node in the topology.
    /// </summary>
    public class TopologyNode : ITopologyNode, IEqualityComparer<TopologyNode>
    {
        /// <summary>
        /// The list of children of this node.
        /// </summary>
        List<string> Children;

        /// <summary>
        /// Constructor for TopologyNode
        /// </summary>
        /// <param name="key">The key of the new node.</param>
        public TopologyNode(string key)
        {
            Children = new List<string>();
            Key = key;
            Parent = null;
        }

        /// <summary>
        /// The key of the node.
        /// </summary>
        /// <returns>Key of this node.</returns>
        public string Key { get; set; }

        /// <summary>
        /// The parent node of the node.
        /// </summary>
        /// <returns>Key of the parent of this node.</returns>
        public string Parent { get; set; }

        /// <summary>
        /// Returns the number of children. 
        /// </summary>
        /// <returns>Number of children of this node.</returns>
        public ulong ChildrenCount { get; set; }

        /// <summary>
        /// Get the children nodes of this node.
        /// </summary>
        /// <returns>List of all children of this node.</returns>
        public List<string> GetChildren()
        {
            return Children;
        }

        /// <summary>
        /// Adds another child to this node.
        /// </summary>
        /// <param name="child">The child to add</param>
        public void AddChild(ref TopologyNode child)
        {
            child.Parent = Key;
            ChildrenCount++;
            Children.Add(child.Key);
        }

    
        /// <summary>
        /// Implementation of IEqualtyComparer:Equals.
        /// </summary>
        /// <param name="node1">The node to compare with node2.</param>
        /// <param name="node2">The node to compare with node1.</param>
        /// <returns>Result of the comparison between node1 and node2.</returns>
        public bool Equals(TopologyNode node1, TopologyNode node2)
        {
            return node1.Key.Equals(node2.Key);
        }

        /// <summary>
        /// Implementation to build the right hash.
        /// </summary>
        /// <param name="node">The object to build the hash for.</param>
        /// <returns>Hash code of the key of the node.</returns>
        public int GetHashCode(TopologyNode node) {
            return Key.GetHashCode();
        }
    }
}

