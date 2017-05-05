using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Contoso
{
    public class ProductionLineDescription : ContosoTopologyDescriptionCommon
    {
        [JsonProperty]
        public string Guid;

        [JsonProperty]
        public List<StationDescription> Stations;
    }

    /// <summary>
    /// Class to define a production line in the topology tree.
    /// </summary>
    public class ProductionLine : ContosoTopologyNode
    {
        /// <summary>
        /// Ctor of a production line in the topology tree.
        /// </summary>
        /// <param name="productionLineDescription">The topology description for the production line.</param>
        public ProductionLine(ProductionLineDescription productionLineDescription) : base(productionLineDescription.Guid, productionLineDescription.Name, productionLineDescription.Description, productionLineDescription)
        {
        }
    }
}
