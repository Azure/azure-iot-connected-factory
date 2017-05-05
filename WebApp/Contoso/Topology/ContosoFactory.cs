using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Contoso
{
    public class FactoryLocationDescription
    {
        [JsonProperty]
        public string City;

        [JsonProperty]
        public string Country;

        [JsonProperty]
        public double Latitude;

        [JsonProperty]
        public double Longitude;

        public FactoryLocationDescription()
        {
            City = "New City";
            Country = "New Country";
            Latitude = 48.1374300;
            Longitude = 11.5754900;
        }
    }

    public class FactoryDescription : ContosoTopologyDescriptionCommon
    {
        [JsonProperty]
        public string Guid;

        [JsonProperty]
        public FactoryLocationDescription Location;

        [JsonProperty]
        public List<ProductionLineDescription> ProductionLines;

        public FactoryDescription()
        {
            Location = new FactoryLocationDescription();
            ProductionLines = new List<ProductionLineDescription>();
        }
    }

    /// <summary>
    /// The location of the factory.
    /// </summary>
    public class FactoryLocation
    {
        /// <summary>
        /// The city where the factory is located.
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// The country where the factory is located.
        /// </summary>
        public string Country { get; set; }

        /// <summary>
        /// The latitude of the geolocation of the factory.
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// The longitude of the geolocation of the factory.
        /// </summary>
        public double Longitude { get; set; }
    }


    /// <summary>
    /// Class to define a factory in the topology tree.
    /// </summary>
    public class Factory : ContosoTopologyNode
    {
        /// <summary>
        /// Ctor of a factory in the topology tree.
        /// </summary>
        /// <param name="factoryDescription">The topology description for the factory.</param>
        public Factory(FactoryDescription factoryDescription) : base(factoryDescription.Guid, factoryDescription.Name, factoryDescription.Description, factoryDescription)
        {
            Location = new FactoryLocation();
            Location.City = factoryDescription.Location.City;
            Location.Country = factoryDescription.Location.Country;
            Location.Latitude = factoryDescription.Location.Latitude;
            Location.Longitude = factoryDescription.Location.Longitude;
        }
    }
}
