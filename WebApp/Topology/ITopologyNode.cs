using System.Collections.Generic;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Topology
{
    public interface ITopologyNode
    {
        List<string> GetChildren();

        void AddChild(ref TopologyNode child);

        string Key { get; set; }
    }
}
