using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Twin;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Registry;
using System;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Configuration;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp
{
    public partial class Startup
    {
        /// <summary>
        /// Initialize the Twin service connection
        /// </summary>
        public static TwinService TwinService;
        public static RegistryService RegistryService;

        public void ConfigureTwinService()
        {
            TwinService = new TwinService(TwinServiceUrl, Resource);
        }

        public void ConfigureRegistryService()
        {
            RegistryService = new RegistryService(RegistryServiceUrl, Resource);
        }

        private string Resource { get; } = ConfigurationProvider.GetConfigurationSettingValue("Audience");

        private string TwinServiceUrl { get; } = ConfigurationProvider.GetConfigurationSettingValue("TwinService") != null
                                           ? ConfigurationProvider.GetConfigurationSettingValue("TwinService")
                                           : "http://localhost:9041";

        private string RegistryServiceUrl { get; } = ConfigurationProvider.GetConfigurationSettingValue("RegistryService") != null
                                                ? ConfigurationProvider.GetConfigurationSettingValue("RegistryService")
                                                : "http://localhost:9042";
    }
}

