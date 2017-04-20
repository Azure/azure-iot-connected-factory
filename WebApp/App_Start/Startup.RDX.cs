using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.RDX;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Topology;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp
{
    public partial class Startup
    {
        public void ConfigureRDX()
        {
            RDXQueryClient.Create();
        }

        RDXTopologyWorker _rdxWorker;

        public void ConfigureRDXTopologyWorker(ITopologyTree topology)
        {
            _rdxWorker = new RDXTopologyWorker(topology);
            _rdxWorker.StartWorker();
        }

    }
}