
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Configuration;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp
{
    public partial class Startup
    {
        public void ConfigureIotHub()
        {
            Task.Run(async () => await ConnectToIotHubAsync());
        }

        private async Task ConnectToIotHubAsync()
        {
            // Get configuration settings
            string iotHubTelemetryConsumerGroup = ConfigurationProvider.GetConfigurationSettingValue("IotHubTelemetryConsumerGroup");
            string iotHubEventHubName = ConfigurationProvider.GetConfigurationSettingValue("IotHubEventHubName");
            string iotHubEventHubEndpointIotHubOwnerConnectionString = ConfigurationProvider.GetConfigurationSettingValue("IotHubEventHubEndpointIotHubOwnerConnectionString");
            string solutionStorageAccountConnectionString = ConfigurationProvider.GetConfigurationSettingValue("SolutionStorageAccountConnectionString");

            // Initialize EventProcessorHost. 
            Trace.TraceInformation("Creating EventProcessorHost for IoTHub: {0}, ConsumerGroup: {1}, ConnectionString: {2}, StorageConnectionString: {3}",
                iotHubEventHubName, iotHubTelemetryConsumerGroup, iotHubEventHubEndpointIotHubOwnerConnectionString, solutionStorageAccountConnectionString);
            string StorageContainerName = "telemetrycheckpoints";
            _eventProcessorHost = new EventProcessorHost(
                    iotHubEventHubName,
                    iotHubTelemetryConsumerGroup,
                    iotHubEventHubEndpointIotHubOwnerConnectionString,
                    solutionStorageAccountConnectionString,
                    StorageContainerName);

            // Registers the Event Processor Host and starts receiving messages
            EventProcessorOptions options = new EventProcessorOptions();
            options.InitialOffsetProvider = ((partitionId) => DateTime.UtcNow);
            options.SetExceptionHandler(EventProcessorHostExceptionHandler);
            try
            {
                await _eventProcessorHost.RegisterEventProcessorAsync<MessageProcessor>(options);
                Trace.TraceInformation($"EventProcessor successfully registered");
            }
            catch (Exception e)
            {
                Trace.TraceInformation($"Exception during register EventProcessorHost '{e.Message}'");
            }
        }

        public void EventProcessorHostExceptionHandler(ExceptionReceivedEventArgs args)
        {
            Trace.TraceInformation($"EventProcessorHostException: {args.Exception.Message}");
        }
        private static EventProcessorHost _eventProcessorHost;
    }
}