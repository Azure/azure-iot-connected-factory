using System;
using System.Collections.Generic;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Topology
{
    public interface ITopologyTree
    {
        TopologyNode TopologyRoot { get; }

        void AddChild(string key, TopologyNode child);

        List<string> GetAllChildren(string key);

        List<string> GetAllChildren(string key, Type type);
    }
}
