
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Configuration;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp
{
    public partial class Startup
    {
        public Task ConfigureIotHub(CancellationToken ct)
        {
            return Task.Run(async () => await ConnectToIotHubAsync(ct));
        }

        private async Task ConnectToIotHubAsync(CancellationToken ct)
        {
            EventProcessorHost eventProcessorHost;

            // Get configuration settings
            string iotHubTelemetryConsumerGroup = ConfigurationProvider.GetConfigurationSettingValue("IotHubTelemetryConsumerGroup");
            string iotHubEventHubName = ConfigurationProvider.GetConfigurationSettingValue("IotHubEventHubName");
            string iotHubEventHubEndpointIotHubOwnerConnectionString = ConfigurationProvider.GetConfigurationSettingValue("IotHubEventHubEndpointIotHubOwnerConnectionString");
            string solutionStorageAccountConnectionString = ConfigurationProvider.GetConfigurationSettingValue("SolutionStorageAccountConnectionString");

            // Initialize EventProcessorHost. 
            Trace.TraceInformation("Creating EventProcessorHost for IoTHub: {0}, ConsumerGroup: {1}, ConnectionString: {2}, StorageConnectionString: {3}",
                iotHubEventHubName, iotHubTelemetryConsumerGroup, iotHubEventHubEndpointIotHubOwnerConnectionString, solutionStorageAccountConnectionString);
            string StorageContainerName = "telemetrycheckpoints";
            eventProcessorHost = new EventProcessorHost(
                    iotHubEventHubName,
                    iotHubTelemetryConsumerGroup,
                    iotHubEventHubEndpointIotHubOwnerConnectionString,
                    solutionStorageAccountConnectionString,
                    StorageContainerName);

            // Registers the Event Processor Host and starts receiving messages.
            EventProcessorOptions options = new EventProcessorOptions();
            options.InitialOffsetProvider = ((partitionId) => DateTime.UtcNow);
            options.SetExceptionHandler(EventProcessorHostExceptionHandler);
            try
            {
                await eventProcessorHost.RegisterEventProcessorAsync<MessageProcessor>(options);
                Trace.TraceInformation($"EventProcessor successfully registered");
            }
            catch (Exception e)
            {
                Trace.TraceInformation($"Exception during register EventProcessorHost '{e.Message}'");
            }

            // Wait till shutdown.
            while (true)
            {
                if (ct.IsCancellationRequested)
                {
                    Trace.TraceInformation($"Application is shutting down. Unregistering EventProcessorHost...");
                    await eventProcessorHost.UnregisterEventProcessorAsync();
                    return;
                }
                await Task.Delay(1000);
            }
        }

        public void EventProcessorHostExceptionHandler(ExceptionReceivedEventArgs args)
        {
            Trace.TraceInformation($"EventProcessorHostException: {args.Exception.Message}");
        }
    }
}