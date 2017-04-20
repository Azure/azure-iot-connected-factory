using GlobalResources;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Configuration;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Helpers
{
    public static class HeaderHelper
    {
        public static string GetHeaderTitle()
        {
            var defaultSolutionName = Strings.DefaultSolutionName;
            return ConfigurationProvider.GetConfigurationSettingValueOrDefault("SolutionName", defaultSolutionName);
        }
    }
}