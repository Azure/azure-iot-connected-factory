
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Contoso;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp
{
    using static Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Controllers.DashboardController;
    using static Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.SessionUpdate;

    public class MonitoredItemDataValue
    {
        public string Value { get; set; }

        public string SourceTimestamp { get; set; }
    }

    public class PublisherMessage
    {
        [JsonProperty("ApplicationUri")]
        public string OpcUri { get; set; }

        [JsonProperty("DisplayName")]
        public string SymbolicName { get; set; }

        public string NodeId { get; set; }

        public MonitoredItemDataValue Value { get; set; }
    }

    /// <summary>
    /// This class processes all ingested data into IoTHub.
    /// </summary>
    public class MessageProcessor : IEventProcessor
    {
        /// <summary>
        /// Opens the event processing for the given partition context.
        /// </summary>
        public Task OpenAsync(PartitionContext context)
        {
            // get number of messages between checkpoints
            _checkpointStopwatch = new Stopwatch();
            _checkpointStopwatch.Start();

            if (_sessionUpdateStopwatch == null)
            {
                _sessionUpdateStopwatch = new Stopwatch();
            }
            return Task.CompletedTask;
        }

        public Task ProcessErrorAsync(PartitionContext context, Exception error)
        {
            Trace.TraceError($"Message processor error '{error.Message}' on partition with id '{context.PartitionId}'");
            Trace.TraceError($"Exception stack '{error.StackTrace}'");
            return Task.CompletedTask;
        }

        public async Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            if (reason == CloseReason.Shutdown)
            {
                await context.CheckpointAsync();
            }

            if (_sessionUpdateStopwatch != null)
            {
                _sessionUpdateStopwatch = null;
            }
        }

        private bool ProcessPublisherMessage(string opcUri, string nodeId, string sourceTimestamp, string value)
        {
            // Get the OPC UA node object.
            ContosoOpcUaNode opcUaNode = Startup.Topology.GetOpcUaNode(opcUri.ToLower(), nodeId);
            if (opcUaNode == null)
            {
                return false;
            }

            // Update last value.
            opcUaNode.LastValue = value;
            opcUaNode.LastValueTimestamp = sourceTimestamp;
            return true;
        }

        private async Task CheckpointAsync(PartitionContext context, Stopwatch checkpointStopwatch)
        {
            await context.CheckpointAsync();
            checkpointStopwatch.Restart();
            Trace.TraceInformation($"checkpoint completed at {DateTime.UtcNow}");
        }

        /// <summary>
        /// Process all events from OPC UA servers and update the last value of each node in the topology.
        /// </summary>
        public async Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> ingestedMessages)
        {
            _sessionUpdateStopwatch.Start();

            // process each message
            foreach (var eventData in ingestedMessages)
            {
                string message = null;
                try
                {
                    // checkpoint, so that the processor does not need to start from the beginning if it restarts
                    if (_checkpointStopwatch.Elapsed > TimeSpan.FromMinutes(_checkpointPeriodInMinutes))
                    {
                        Task.Run(async () => await CheckpointAsync(context, _checkpointStopwatch));
                    }

                    message = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);
                    if (message != null)
                    {
                        _processorHostMessages++;
                        // support batched messages as well as simple message ingests
                        if (message.StartsWith("["))
                        {
                            List<PublisherMessage> publisherMessages = JsonConvert.DeserializeObject<List<PublisherMessage>>(message);
                            foreach (var publisherMessage in publisherMessages)
                            {
                                if (publisherMessage != null)
                                {
                                    _publisherMessages++;
                                    try
                                    {
                                        ProcessPublisherMessage(publisherMessage.OpcUri, publisherMessage.NodeId, publisherMessage.Value.SourceTimestamp, publisherMessage.Value.Value);
                                        _lastSourceTimestamp = publisherMessage.Value.SourceTimestamp;
                                        _lastOpcUri = publisherMessage.OpcUri;
                                        _lastNodeId = publisherMessage.NodeId;
                                    }
                                    catch (Exception e)
                                    {
                                        Trace.TraceError($"Exception '{e.Message}' while processing message {publisherMessage}");
                                    }
                                }
                            }
                        }
                        else
                        {
                            PublisherMessage publisherMessage = JsonConvert.DeserializeObject<PublisherMessage>(message);
                            _publisherMessagesInvalidFormat++;
                            if (publisherMessage != null)
                            {
                                ProcessPublisherMessage(publisherMessage.OpcUri, publisherMessage.NodeId, publisherMessage.Value.SourceTimestamp, publisherMessage.Value.Value);
                                _lastSourceTimestamp = publisherMessage.Value.SourceTimestamp;
                                _lastOpcUri = publisherMessage.OpcUri;
                                _lastNodeId = publisherMessage.NodeId;
                            }
                        }

                        // if there are sessions looking at stations we update sessions each second
                        if (_sessionUpdateStopwatch.ElapsedMilliseconds > TimeSpan.FromSeconds(1).TotalMilliseconds)
                        {
                            if (SessionsViewingStationsCount() != 0)
                            {
                                try
                                {
                                    Trace.TraceInformation($"processorHostMessages {_processorHostMessages}, publisherMessages {_publisherMessages}/{_publisherMessagesInvalidFormat}, sourceTimestamp: '{_lastSourceTimestamp}'");
                                    Trace.TraceInformation($"opcUri '{_lastOpcUri}', nodeid '{_lastNodeId}'");
                                    TriggerSessionChildrenDataUpdate();
                                }
                                catch (Exception e)
                                {
                                    Trace.TraceError($"Exception '{e.Message}' while updating browser sessions");
                                }
                                finally
                                {
                                    _sessionUpdateStopwatch.Restart();
                                }
                            }
                        }
                    }

                }
                catch (Exception e)
                {
                    Trace.TraceError($"Exception '{e.Message}' processing message '{message}'");
                }
            }
        }

        private Stopwatch _checkpointStopwatch;
        private const double _checkpointPeriodInMinutes = 5;
        private Stopwatch _sessionUpdateStopwatch;
        private int _processorHostMessages;
        private int _publisherMessages;
        private int _publisherMessagesInvalidFormat;
        private string _lastSourceTimestamp;
        private string _lastOpcUri;
        private string _lastNodeId;
    }
}


