using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Helpers;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp
{
    public partial class Startup
    {
        /// <summary>
        /// Our startup method
        /// </summary>
        public void ConfigureOpcUa()
        {
            OpcSessionHelper.Instance.LoadApplicationConfiguration().Wait();
        }
    }
}