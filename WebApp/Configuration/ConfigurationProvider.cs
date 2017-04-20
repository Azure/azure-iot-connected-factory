using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Configuration
{
    public static class ConfigurationProvider
    {
        static readonly Dictionary<string, string> _configuration = new Dictionary<string, string>();
        const string _configToken = "config:";

        public static string GetConfigurationSettingValue(string configurationSettingName)
        {
            return GetConfigurationSettingValueOrDefault(configurationSettingName, string.Empty);
        }

        public static string GetConfigurationSettingValueOrDefault(string configurationSettingName, string defaultValue)
        {

                if (!_configuration.ContainsKey(configurationSettingName))
                {
                    string configValue = CloudConfigurationManager.GetSetting(configurationSettingName);
                    bool isEmulated = Environment.CommandLine.Contains("iisexpress.exe");

                    if (isEmulated && (configValue != null && configValue.StartsWith(_configToken, StringComparison.OrdinalIgnoreCase)))
                    {
                        if (!EnvironmentDescription.IsInitialized)
                        {
                            LoadLocalEnvironmentConfig(configurationSettingName, configValue);
                        }

                        configValue = EnvironmentDescription.GetSetting(
                            configValue.Substring(configValue.IndexOf(_configToken, StringComparison.Ordinal) + _configToken.Length));
                    }
                    try
                    {
                        _configuration.Add(configurationSettingName, configValue);
                    }
                    catch (ArgumentException)
                    {
                        // at this point, this key has already been added on a different
                        // thread, so we're fine to continue
                    }
                }

            return _configuration[configurationSettingName];
        }

        public static string GetRootPath()
        {
            var executingPath = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);

            // Check for local execution
            var rootPath = executingPath;
            if (rootPath == null)
            {
                return null;
            }

            bool fileFound;
            do
            {
                var fileName = string.Concat(rootPath, "\\local.config.user");
                fileFound = File.Exists(fileName);
                var rootPathLen = rootPath.LastIndexOf('\\');
                if (fileFound || rootPathLen == -1)
                {
                    break;
                }
                rootPath = rootPath.Substring(0, rootPathLen);
            } while (true);

            if (fileFound)
            {
                return rootPath;
            }
            return null;
        }

        private static void LoadLocalEnvironmentConfig(string configurationSettingName, string configValue)
        {
            string rootPath = GetRootPath();

            if (rootPath == null)
            {
                string error = string.Format(CultureInfo.InvariantCulture, "Unable to locate local.config.user to read '{0}' for setting '{1}'.  Make sure you have run 'build.cmd local' if you try to run locally.", configValue, configurationSettingName);
                Trace.TraceError(error);
                throw new ArgumentException(error);
            }
            else
            {
                EnvironmentDescription.Init(rootPath + "\\local.config.user");
            }
        }
    }
}
