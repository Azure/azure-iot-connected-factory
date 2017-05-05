
using Opc.Ua.Server;
using System.Collections.Generic;
using Station;

namespace Opc.Ua.Sample.Simulation
{
    public partial class FactoryStationServer : StandardServer
    {
        protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
        {
            List<INodeManager> nodeManagers = new List<INodeManager>();
            nodeManagers.Add(new StationNodeManager(server, configuration));
            return new MasterNodeManager(server, configuration, null, nodeManagers.ToArray());
        }

        protected override ServerProperties LoadServerProperties()
        {
            ServerProperties properties = new ServerProperties();
            properties.ManufacturerName = "Contoso";
            properties.ProductName      = "OPC UA Factory Station Simulation";
            properties.ProductUri       = "";
            properties.SoftwareVersion  = Utils.GetAssemblySoftwareVersion();
            properties.BuildNumber      = Utils.GetAssemblyBuildNumber();
            properties.BuildDate        = Utils.GetAssemblyTimestamp();
            return properties; 
        }
    }
}
