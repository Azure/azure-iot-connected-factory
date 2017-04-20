using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Topology;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Contoso;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Models;
using Microsoft.AspNet.SignalR;
using Microsoft.Rdx.SystemExtensions;
using Microsoft.Rdx.Client.Query.ObjectModel.Aggregates;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.RDX
{
    /// <summary>
    /// Topology query worker to update Oee and Kpi values
    /// </summary>
    public class RDXTopologyWorker
    {
        /// <summary>
        /// Contoso Topology structures
        /// </summary>
        private ITopologyTree _tree;
        private ContosoTopology _topology;

        /// <summary>
        /// List of topology workers
        /// </summary>
        private List<Task> _workers;
        private int _busyWorkers;
        private int _workerStartDelayed;

        /// <summary>
        /// Ctor of the topology worker
        /// </summary>
        /// <param name="tree">Topology tree to query</param>
        public RDXTopologyWorker(ITopologyTree tree)
        {
            _busyWorkers = 0;
            _workerStartDelayed = 0;
            _tree = tree;
            _topology = _tree as ContosoTopology;
            _workers = new List<Task>();
        }

        /// <summary>
        /// Start the topology worker threads
        /// </summary>
        public void StartWorker()
        {
            StartWorker(CancellationToken.None);
        }

        /// <summary>
        /// Start the topology worker threads
        /// </summary>
        public void StartWorker(CancellationToken token)
        {
            try
            {
                // create a worker thread for each aggregation task
                ContosoTopologyNode root = _topology.GetRootNode();
                for (int aggregateIndex = 0; aggregateIndex < root.Aggregations.Count; aggregateIndex++)
                {
                    // need a local copy of aggregateIndex to asynchronously start worker properly
                    int workerIndex = aggregateIndex;
                    _workers.Add(Task.Run(async () => await Worker(workerIndex, token)));
                }
            }
            catch (Exception e)
            {
                RDXTrace.TraceError("Start aggregation workers failed: {0}:", e.Message);
            }
        }

        /// <summary>
        /// Worker to query one aggregate of a topology
        /// </summary>
        /// <param name="aggregateIndex">The aggregation view to update by the worker</param>
        /// <param name="token">CancellationToken</param>
        public async Task Worker(int aggregateIndex, CancellationToken token)
        {
            RDXOpcUaQueries opcUaQueries = new RDXOpcUaQueries(token);
            RDXOeeKpiQueries oeeKpiQueries = new RDXOeeKpiQueries(opcUaQueries, token);

            RDXTrace.TraceInformation("RDX Worker {0} started", aggregateIndex);

            // give app some time to start before updating queries
            await Task.Delay(10000 * (aggregateIndex + 1));

            while (!token.IsCancellationRequested)
            {
                Stopwatch stopWatch = new Stopwatch();
                DateTime nextUpdate = DateTime.MaxValue;
                bool resetStartDelayed = false; 

                stopWatch.Start();

                Interlocked.Increment(ref _busyWorkers);

                // the station and node list is updated, other workers are delayed
                while (_workerStartDelayed != 0)
                {
                    RDXTrace.TraceInformation("RDX Worker {0} delayed", aggregateIndex);
                    await Task.Delay(1000);
                }

                try
                {
                    List<Task> tasks = new List<Task>();
                    ContosoTopologyNode rootNode = _topology.GetRootNode();
                    ContosoAggregatedOeeKpiHistogram aggregatedTimeSpan = rootNode[aggregateIndex];
                    DateTimeRange searchSpan = RDXUtils.TotalSearchRangeFromNow(aggregatedTimeSpan);

                    RDXTrace.TraceInformation("RDX Worker {0} updating Range {1} to {2}",
                        aggregateIndex, searchSpan.From, searchSpan.To);

                    // calc next update. To time is already rounded for update time span.
                    nextUpdate = searchSpan.To + rootNode[aggregateIndex].UpdateTimeSpan;

                    // query all stations in topology and find all active servers in timespan
                    Task<StringDimensionResult> aggServerTask = opcUaQueries.AggregateServers(searchSpan);

                    // always get an aggregate of all activity for the latest interval, use it as a cache
                    TimeSpan intervalTimeSpan = aggregatedTimeSpan[0].IntervalTimeSpan;
                    DateTimeRange aggregateSpan = RDXUtils.CalcAggregationRange(aggregatedTimeSpan, searchSpan.To);
                    RDXCachedAggregatedQuery fullQuery = new RDXCachedAggregatedQuery(opcUaQueries);
                    Task aggServerAndNodesTask = fullQuery.Execute(aggregateSpan);

                    // wait for all outstanding aggregates
                    tasks.Add(aggServerTask);
                    tasks.Add(aggServerAndNodesTask);
                    await RDXUtils.WhenAllTasks("Aggregates", tasks, stopWatch);

                    List<string> topologyStations = _topology.GetAllChildren(_tree.TopologyRoot.Key, typeof(Station));
                    List<string> opcUaServers = await aggServerTask;

                    // intersect list of active servers and schedule all queries
                    tasks.Clear();
                    List<string> opcUaServersToQuery = opcUaServers.Intersect(topologyStations, StringComparer.InvariantCultureIgnoreCase).ToList();
                    await oeeKpiQueries.ScheduleAllOeeKpiQueries(searchSpan, _topology, fullQuery, opcUaServersToQuery, tasks, aggregateIndex);

                    // wait for all outstanding queries
                    await RDXUtils.WhenAllTasks("Queries", tasks, stopWatch);

                    // Update the topology Oee and KPI values
                    _topology.UpdateAllKPIAndOEEValues(aggregateIndex);

                    // one worker issues the Browser update
                    if (aggregatedTimeSpan.UpdateBrowser)
                    {
                        // Push updates to dashboard
                        BrowserUpdate();
                        RDXTrace.TraceInformation("BrowserUpdate finished after {0}ms",
                            stopWatch.ElapsedMilliseconds);
                    }

                    // add new stations and nodes to topology
                    if (aggregatedTimeSpan.UpdateTopology)
                    {
                        // delay other workers
                        Interlocked.Exchange(ref _workerStartDelayed, 1);
                        resetStartDelayed = true;
                        // only add stations and nodes if no other worker is busy yet
                        if (Interlocked.Increment(ref _busyWorkers) == 2)
                        {
                            AddNewServers(fullQuery, topologyStations);
                            await AddNewNodes(fullQuery, topologyStations);
                            RDXTrace.TraceInformation("Add New Server and Nodes finished after {0}ms",
                                stopWatch.ElapsedMilliseconds);
                        }
                    }
                }
                catch (Exception e)
                {
                    RDXTrace.TraceError("Exception {0} in Worker after {1}ms",
                        e.Message, stopWatch.ElapsedMilliseconds);
                }
                finally
                {
                    Interlocked.Decrement(ref _busyWorkers);
                    if (resetStartDelayed)
                    {
                        Interlocked.Decrement(ref _busyWorkers);
                        Interlocked.Exchange(ref _workerStartDelayed, 0);
                    }
                }

                RDXTrace.TraceInformation("RDX Worker {0} schedule next update for {1}",
                    aggregateIndex, nextUpdate);

                TimeSpan delay = nextUpdate.Subtract(DateTime.UtcNow);
                if (delay.TotalMilliseconds > 0)
                    await Task.Delay(delay, token);

            }
        }

        /// <summary>
        /// Compares active OPC UA servers and topology server list. 
        /// New OPC UA servers are added to topology.
        /// </summary>
        /// <param name="opcUaServers">List of publishing OPC UA servers</param>
        /// <param name="topologyStations">List of stations in topology</param>
        private void AddNewServers(RDXCachedAggregatedQuery fullQuery, List<string> topologyStations)
        {
            List<string> opcUaServers = fullQuery.GetActiveServerList();
            List<string> newServers = opcUaServers.Except(topologyStations, StringComparer.InvariantCultureIgnoreCase).ToList();
            if (newServers.Count > 0)
            {
                _topology.AddNewStations(newServers);
            }
        }

        /// <summary>
        /// Search cached query for all active nodeId of all servers in topology.
        /// Add new nodes to station topology.
        /// </summary>
        /// <param name="fullQuery">A cached query</param>
        /// <param name="topologyStations">List of stations in topology</param>
        private async Task AddNewNodes(RDXCachedAggregatedQuery fullQuery, List<string> topologyStations)
        {
            List<string> opcUaServers = fullQuery.GetActiveServerList();
            foreach (string opcUaServer in opcUaServers)
            {
                ContosoOpcUaServer topologyNode = _topology[opcUaServer] as ContosoOpcUaServer;
                if (topologyNode != null)
                {
                    List<string> topologyNodeIdList = topologyNode.NodeList.Select(x => x.NodeId).ToList();
                    List<string> activeNodeIdList = fullQuery.GetActiveNodeIdList(opcUaServer);
                    var newNodeIdList = activeNodeIdList.Except(topologyNodeIdList, StringComparer.InvariantCultureIgnoreCase);
                    if (newNodeIdList.Count() > 0)
                    {
                        RDXOpcUaQueries opcUaQueries = new RDXOpcUaQueries(CancellationToken.None);
                        foreach (string newNodeId in newNodeIdList)
                        {

                            RDXTrace.TraceInformation("RDX Worker adding node {0} to server {1}", newNodeId, opcUaServer);
                            string symbolicName = await opcUaQueries.QueryDisplayName(DateTime.UtcNow, opcUaServer, newNodeId);
                            topologyNode.AddOpcServerNode(
                                newNodeId,
                                symbolicName,
                                null,
                                ContosoOpcNodeOpCode.Last,
                                null,
                                true,
                                null,
                                null,
                                null,
                                null,
                                null
                                );
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Update the browser content using SignalR
        /// </summary>
        private void BrowserUpdate()
        {
            ChildrenDataUpdate[] sessionChildrenDataUpdate = new ChildrenDataUpdate[Startup.SessionList.Count];
            OeeKpiDataUpdate[] sessionOeeKpiDataUpdate = new OeeKpiDataUpdate[Startup.SessionList.Count];
            AlertDataUpdate[] sessionAlertDataUpdate = new AlertDataUpdate[Startup.SessionList.Count];
            int sessionUpdateIndex = 0;
            foreach (KeyValuePair<string, DashboardModel> session in Startup.SessionList)
            {
                ChildrenDataUpdate sessionChildrenData = new ChildrenDataUpdate();
                OeeKpiDataUpdate sessionOeeKpiData = new OeeKpiDataUpdate();
                AlertDataUpdate sessionAlertData = new AlertDataUpdate();
                string topNodeKey = session.Value.TopNode.Key;

                // Update the OEE/KPI relevant data.
                sessionOeeKpiData.SessionId = session.Key;
                sessionOeeKpiData.TopNode = topNodeKey;

                // Add the performance data relevant for the session.
                ContosoTopologyNode topNode = (ContosoTopologyNode)Startup.Topology[topNodeKey];
                ContosoAggregatedOeeKpiTimeSpan oeeKpiLast = topNode.Last;
                sessionOeeKpiData.Kpi1Last = oeeKpiLast.Kpi1;
                sessionOeeKpiData.Kpi1PerformanceSetting = topNode.Kpi1PerformanceSetting;
                sessionOeeKpiData.Kpi2Last = oeeKpiLast.Kpi2;
                sessionOeeKpiData.Kpi2PerformanceSetting = topNode.Kpi2PerformanceSetting;
                sessionOeeKpiData.OeeAvailabilityLast = oeeKpiLast.OeeAvailability;
                sessionOeeKpiData.OeeAvailabilityPerformanceSetting = topNode.OeeAvailabilityPerformanceSetting;
                sessionOeeKpiData.OeePerformanceLast = oeeKpiLast.OeePerformance;
                sessionOeeKpiData.OeePerformancePerformanceSetting = topNode.OeePerformancePerformanceSetting;
                sessionOeeKpiData.OeeQualityLast = oeeKpiLast.OeeQuality;
                sessionOeeKpiData.OeeQualityPerformanceSetting = topNode.OeeQualityPerformanceSetting;
                sessionOeeKpiData.OeeOverallLast = oeeKpiLast.OeeOverall;
                sessionOeeKpiData.OeeOverallPerformanceSetting = topNode.OeeOverallPerformanceSetting;

                // Update the alert data.
                sessionAlertData.SessionId = session.Key;
                sessionAlertData.TopNode = topNodeKey;
                sessionAlertData.Alerts = Startup.Topology.GetAlerts(topNodeKey);

                // Update the children data.
                sessionChildrenData.SessionId = session.Key;
                sessionChildrenData.TopNode = topNodeKey;
                sessionChildrenData.Children = Startup.Topology.GetChildrenInfo(topNodeKey);

                // Update the data sent to the clients.
                sessionChildrenDataUpdate[sessionUpdateIndex] = sessionChildrenData;
                sessionOeeKpiDataUpdate[sessionUpdateIndex] = sessionOeeKpiData;
                sessionAlertDataUpdate[sessionUpdateIndex] = sessionAlertData;
                sessionUpdateIndex++;
            }

            if (Startup.SessionList.Count > 0 && sessionUpdateIndex > 0)
            {
                string _sessionChildrenDataUpdateJson = JsonConvert.SerializeObject(sessionChildrenDataUpdate);
                string _sessionOeeKpiDataUpdateJson = JsonConvert.SerializeObject(sessionOeeKpiDataUpdate);
                string _sessionAlertDataUpdateJson = JsonConvert.SerializeObject(sessionAlertDataUpdate);
                IHubContext hubContext = GlobalHost.ConnectionManager.GetHubContext<TelemetryHub>();
                hubContext.Clients.All.updateSessionChildrenData(_sessionChildrenDataUpdateJson);
                hubContext.Clients.All.updateSessionOeeKpiData(_sessionOeeKpiDataUpdateJson);
                hubContext.Clients.All.updateSessionAlertData(_sessionAlertDataUpdateJson);
            }
        }
    }
}

