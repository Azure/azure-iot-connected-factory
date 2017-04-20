using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Topology
{
    /// <summary>
    /// Represents the topology tree.
    /// </summary>
    public class TopologyTree : ITopologyTree
    {
        /// <summary>
        /// The root node of the topology.
        /// </summary>
        private TopologyNode _topologyRoot;

        /// <summary>
        /// All nodes of the topology as a hashtable. 
        /// </summary>
        protected Hashtable TopologyTable;

        /// <summary>
        /// The root node property of the topology.
        /// </summary>
        public TopologyNode TopologyRoot
        {
            get
            {
                return _topologyRoot;
            }
            set
            {
                if (_topologyRoot != null)
                {
                    throw new Exception("Topology root is already initialized."); ;
                }
                _topologyRoot = value;
                TopologyTable[_topologyRoot.Key] = _topologyRoot;
            }
        }

        /// <summary>
        /// Default Ctor for the TopologyTree class.
        /// </summary>
        public TopologyTree()
        {
            TopologyTable = new Hashtable((IEqualityComparer)null);
        }

        /// <summary>
        /// Array operator, only supporting get, to easily access a node.
        /// </summary>
        /// <param name="key">The key of the element to be accessed.</param>
        /// <returns>The node for the given key.</returns>
        public TopologyNode this[string key]
        {
            get
            {
                return (TopologyNode)TopologyTable[key];
            }
        }

        /// <summary>
        /// Adds a new child to the node with the given key.
        /// </summary>
        /// <param name="key">The key identifieing the node to which child should be added.
        /// <param name="child">The child node to add into the topology below the node identified by key.
        public void AddChild(string key, TopologyNode child)
        {
            if (TopologyTable.ContainsKey(child.Key))
            {
                throw new Exception(string.Format(CultureInfo.InvariantCulture, "The topology key '{0}' is already used. Please change.", child.Key));
            }
            ((TopologyNode)TopologyTable[key]).AddChild(ref child);
            TopologyTable[child.Key] = child;
        }

        /// <summary>
        /// Returns a list of all nodes under this node.
        /// </summary>
        /// <param name="key">The key of the node,for which the list should be returned.</param>
        /// <returns>A list of all nodes under node with the given key.</returns>
        public List<string> GetAllChildren(string key)
        {
            List<string> allChildren = new List<string>();

            foreach (var child in ((TopologyNode)TopologyTable[key]).GetChildren())
            {
                allChildren.Add(child);
                if (((TopologyNode)TopologyTable[child]).ChildrenCount > 0)
                {
                    allChildren.AddRange(GetAllChildren(child));
                }
            }
            return allChildren;
        }

        /// <summary>
        /// Returns a list of all nodes under this node with the given type.
        /// </summary>
        /// <param name="key">The key of the node,for which the list should be returned.</param>
        /// <param name="type">The type of the nodes returned.</param>
        /// <returns>A list of all nodes under node with the given key.</returns>
        public List<string> GetAllChildren(string key, Type type)
        {
            List<string> allChildren = new List<string>();

            foreach (var child in ((TopologyNode)TopologyTable[key]).GetChildren())
            {
                if (TopologyTable[child].GetType() == type)
                {
                    allChildren.Add(child);
                }
                if (((TopologyNode)TopologyTable[child]).ChildrenCount > 0)
                {
                    allChildren.AddRange(GetAllChildren(child, type));
                }
            }
            return allChildren;
        }

        /// <summary>
        /// Returns a list of all parent nodes including the given node and the root node.
        /// </summary>
        /// <param name="key">The key of the node,for which the list should be returned.</param>
        /// <returns>A list of all nodes above the given node with the given key.</returns>
        public List<string> GetFullHierarchy(string key)
        {
            List<string> allParents = new List<string>();
            string parent = key;
            do
            {
                allParents.Add(parent);
                parent = ((TopologyNode)TopologyTable[parent]).Parent;
            } while (parent != null);
            return allParents;
        }

        /// <summary>
        /// Returns a list of all parent nodes with the given type.
        /// </summary>
        /// <param name="key">The key of the node,for which the list should be returned.</param>
        /// <param name="type">The type of the nodes returned.</param>
        /// <returns>A list of all nodes above the given node with the given key.</returns>
        public List<string> GetAllParents(string key, Type type)
        {
            List<string> allParents = new List<string>();
            string parent = key;
            do
            {
                parent = ((TopologyNode)TopologyTable[parent]).Parent;
                if (TopologyTable[parent].GetType() == type)
                {
                    allParents.Add(parent);
                }
            } while (parent != TopologyRoot.Key);
            return allParents;
        }

        /// <summary>
        /// Checks if the given child is somewhere below the given root node.
        /// </summary>
        /// <param name="child">The key of the child.</param>
        /// <param name="root">The key of the root node which should be checked for.</param>
        /// <returns>True if the child is somewhere under root, otherwise false.</returns>
        public bool IsChild(string child, string root)
        {
            // Validation that keys are known.
            if (TopologyTable.ContainsKey(child) && TopologyTable.ContainsKey(root))
            {
                string parent = child;
                do
                {
                    parent = ((TopologyNode)TopologyTable[parent]).Parent;
                    if (parent == root)
                    {
                        return true;
                    }
                } while (parent != TopologyRoot.Key);
            }
            return false;
        }
    }
}
