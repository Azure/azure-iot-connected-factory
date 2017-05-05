using System.Collections.Generic;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Contoso;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Models;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp
{
    public partial class Startup
    {
        /// <summary>
        /// Represents the full topology of the company
        /// </summary>
        public static ContosoTopology Topology;

        /// <summary>
        /// Holds the list of active sessions with all the relevant information.
        /// </summary>
        public static Dictionary<string, DashboardModel> SessionList = new Dictionary<string, DashboardModel>();

        public void ConfigureTopology()
        {
            Topology = new ContosoTopology(System.Web.HttpContext.Current.Server.MapPath(@"~/bin/Contoso/Topology/ContosoTopologyDescription.json"));
        }
    }
}