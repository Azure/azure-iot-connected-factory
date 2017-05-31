using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Contoso;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.OpcUa;
using Microsoft.Rdx.SystemExtensions;
using Microsoft.Rdx.Client.Query.Expressions;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.RDX
{

    /// <summary>
    /// Delegate to operate on the query value.
    /// </summary>
    /// <param name="rdxQuery">The query information.</param>
    /// <param name="opcUaNode">The topology information.</param>
    public delegate Task RDXOpCode(RDXOeeKpiQuery rdxQuery, ContosoOpcUaNode opcUaNode);

    /// <summary>
    /// Implementation of all operations and helpers. 
    /// </summary>
    public class RDXContosoOpCodes 
    {
        /// <summary>
        /// Get the delegate for an operator
        /// </summary>
        /// <param name="opCode">Operation</param>
        /// <returns>The delegate</returns>
        public static RDXOpCode GetOperator(ContosoOpcNodeOpCode opCode)
        {
            switch (opCode)
            {
                case ContosoOpcNodeOpCode.Nop: return Nop;
                case ContosoOpcNodeOpCode.Diff: return Diff;
                case ContosoOpcNodeOpCode.Avg: return Avg;
                case ContosoOpcNodeOpCode.Sum: return Sum;
                case ContosoOpcNodeOpCode.Last: return Last;
                case ContosoOpcNodeOpCode.Count: return Count;
                case ContosoOpcNodeOpCode.Max: return Max;
                case ContosoOpcNodeOpCode.Min: return Min;
                case ContosoOpcNodeOpCode.Const: return Const;
                case ContosoOpcNodeOpCode.SubMaxMin: return SubMaxMin;
                case ContosoOpcNodeOpCode.Timespan: return Timespan;
                default:
                    break;
            }
            return null;
        }

        /// <summary>
        /// Calls the delegate for an operator.
        /// </summary>
        /// <param name="opCode">Operation to execute</param>
        /// <param name="rdxQuery">Query info.</param>
        /// <param name="opcUaNode">Topology node.</param>
        public static Task CallOperator(ContosoOpcNodeOpCode opCode, RDXOeeKpiQuery rdxQuery, ContosoOpcUaNode opcUaNode)
        {
            RDXOpCode opCodeFunc = GetOperator(opCode);
            if (opCodeFunc == null)
            {
                return Task.CompletedTask;
            }
            return opCodeFunc(rdxQuery, opcUaNode);
        }

        /// <summary>
        /// NOP delegate. Just updates the time, value is not updated.
        /// </summary>
        public static Task Nop(RDXOeeKpiQuery rdxQuery, ContosoOpcUaNode opcUaNode)
        {
            opcUaNode.Last.Time = DateTime.UtcNow;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Diff delegate. Calculate the difference between first value and last value in timespan.
        /// </summary>
        public static async Task Diff(RDXOeeKpiQuery rdxQuery, ContosoOpcUaNode opcUaNode)
        {
            opcUaNode.Last.Value = await rdxQuery.OpcUaQueries.DiffQuery(rdxQuery.SearchSpan, rdxQuery.AppUri, opcUaNode.NodeId);
            opcUaNode.Last.Time = rdxQuery.SearchSpan.To;
            opcUaNode.UpdateRelevance(rdxQuery.TopologyNode);
        }

        /// <summary>
        /// Average delegate. Calculate the average of all values in timespan.
        /// </summary>
        public static Task Avg(RDXOeeKpiQuery rdxQuery, ContosoOpcUaNode opcUaNode)
        {
            return Aggregate(rdxQuery, opcUaNode, RDXOpcUaQueries.AverageValues());
        }

        /// <summary>
        /// Sum delegate. Adds all values in timespan.
        /// </summary>
        public static Task Sum(RDXOeeKpiQuery rdxQuery, ContosoOpcUaNode opcUaNode)
        {
            return Aggregate(rdxQuery, opcUaNode, RDXOpcUaQueries.SumValues());
        }

        /// <summary>
        /// Last delegate. Gets last (newest) value in a timespan.
        /// </summary>
        public static async Task Last(RDXOeeKpiQuery rdxQuery, ContosoOpcUaNode opcUaNode)
        {
            opcUaNode.Last.Value = await rdxQuery.OpcUaQueries.GetLatestQuery(rdxQuery.SearchSpan.To, rdxQuery.AppUri, opcUaNode.NodeId);
            opcUaNode.Last.Time = rdxQuery.SearchSpan.To;
            opcUaNode.UpdateRelevance(rdxQuery.TopologyNode);
        }

        /// <summary>
        /// Min delegate. Returns smallest value in a timespan.
        /// </summary>
        public static Task Min(RDXOeeKpiQuery rdxQuery, ContosoOpcUaNode opcUaNode)
        {
            return Aggregate(rdxQuery, opcUaNode, RDXOpcUaQueries.MinValues());
        }

        /// <summary>
        /// Max delegate. Returns largest value in a timespan.
        /// </summary>
        public static Task Max(RDXOeeKpiQuery rdxQuery, ContosoOpcUaNode opcUaNode)
        {
            return Aggregate(rdxQuery, opcUaNode, RDXOpcUaQueries.MaxValues());
        }

        /// <summary>
        /// Count delegate. Returns number of events in a timespan.
        /// </summary>
        public static Task Count(RDXOeeKpiQuery rdxQuery, ContosoOpcUaNode opcUaNode)
        {
            return Aggregate(rdxQuery, opcUaNode, Expression.Count());
        }

        /// <summary>
        /// Const delegate. Returns a constant value defined in the Node Id field.
        /// </summary>
        public static Task Const(RDXOeeKpiQuery rdxQuery, ContosoOpcUaNode opcUaNode)
        {
            opcUaNode.Last.Value = (double) opcUaNode.ConstValue;
            opcUaNode.Last.Time = DateTime.MinValue;
            opcUaNode.UpdateRelevance(rdxQuery.TopologyNode);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Timespan delegate. Returns the timespan as value.
        /// </summary>
        public static Task Timespan(RDXOeeKpiQuery rdxQuery, ContosoOpcUaNode opcUaNode)
        {
            TimeSpan timespan = rdxQuery.SearchSpan.To.Subtract(rdxQuery.SearchSpan.From);
            switch (opcUaNode.Units)
            {
                case "h":
                    opcUaNode.Last.Value = timespan.TotalHours;
                    break;
                case "m":
                    opcUaNode.Last.Value = timespan.TotalMinutes;
                    break;
                case "s":
                    opcUaNode.Last.Value = timespan.TotalSeconds;
                    break;
                case "t":
                    opcUaNode.Last.Value = timespan.Ticks;
                    break;
                case "ms":
                default:
                    opcUaNode.Last.Value = timespan.TotalMilliseconds;
                    break;
            }
            opcUaNode.Last.Time = rdxQuery.SearchSpan.To;
            opcUaNode.UpdateRelevance(rdxQuery.TopologyNode);
            return Task.CompletedTask;
        }

        /// <summary>
        /// SubMaxMin delegate. Subtracts min from max value in a timespan.
        /// </summary>
        /// <param name="rdxQuery"></param>
        /// <param name="opcUaNode"></param>
        public static async Task SubMaxMin(RDXOeeKpiQuery rdxQuery, ContosoOpcUaNode opcUaNode)
        {
            double max = await rdxQuery.OpcUaQueries.GetAggregatedNode(rdxQuery.SearchSpan, rdxQuery.AppUri, opcUaNode.NodeId, RDXOpcUaQueries.MaxValues());
            double min = await rdxQuery.OpcUaQueries.GetAggregatedNode(rdxQuery.SearchSpan, rdxQuery.AppUri, opcUaNode.NodeId, RDXOpcUaQueries.MinValues());
            opcUaNode.Last.Value = max - min;
            opcUaNode.Last.Time = rdxQuery.SearchSpan.To;
            opcUaNode.UpdateRelevance(rdxQuery.TopologyNode);
        }

        /// <summary>
        /// Query for a single value.
        /// </summary>
        private static async Task Aggregate(RDXOeeKpiQuery rdxQuery, ContosoOpcUaNode opcUaNode, AggregateExpression expression)
        {
            opcUaNode.Last.Value = await rdxQuery.OpcUaQueries.GetAggregatedNode(rdxQuery.SearchSpan, rdxQuery.AppUri, opcUaNode.NodeId, expression);
            opcUaNode.Last.Time = rdxQuery.SearchSpan.To;
            opcUaNode.UpdateRelevance(rdxQuery.TopologyNode);
        }
    }


    /// <summary>
    /// Schedule Oee and Kpi queries for one OPC UA server
    /// in a topology node for a single timespan
    /// </summary>
    public class RDXOeeKpiQuery : ContosoOeeKpiOpCodeQueryInfo
    {
        public RDXOpcUaQueries OpcUaQueries { get; }
        public DateTimeRange SearchSpan { get; }

        public RDXOeeKpiQuery(
            RDXOpcUaQueries opcUaQueries,
            DateTimeRange searchSpan,
            string appUri,
            ContosoAggregatedOeeKpiTimeSpan topologyNode
            ) : base(appUri, topologyNode)
        {
            OpcUaQueries = opcUaQueries;
            SearchSpan = searchSpan;
        }

        /// <summary>
        /// Query all relevant nodes of the station, add tasks to list or await tasks sequentially.
        /// </summary>
        /// <param name="tasks">The task list</param>
        /// <param name="nodelist">The node list to query</param>
        /// <param name="awaitTasks">serialize or schedule all</param>
        /// <param name="aggregatedQuery">precachedQuery</param>
        public async Task QueryAllNodes(List<Task> tasks, List<OpcUaNode> nodelist, bool awaitTasks, RDXCachedAggregatedQuery aggregatedQuery = null)
        {
            foreach (ContosoOpcUaNode node in nodelist)
            {
                if (node.Relevance != null &&
                    node.OpCode != ContosoOpcNodeOpCode.Undefined)
                {
                    Task newTask;
                    // query opcode and update relevance
                    if (aggregatedQuery != null)
                    {
                        newTask = aggregatedQuery.CallOperator(node.OpCode, this, node);
                    }
                    else
                    {
                        newTask = RDXContosoOpCodes.CallOperator(node.OpCode, this, node);
                    }
                    if (newTask != null &&
                        newTask != Task.CompletedTask &&
                        newTask.Status != TaskStatus.RanToCompletion)
                    {
                        if (awaitTasks)
                        {
                            await newTask;
                        }
                        else
                        {
                            tasks.Add(newTask);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Process the Oee and Kpi queries on a node
    /// </summary>
    public class RDXOeeKpiQueries
    {

        private RDXOpcUaQueries _opcUaQueries;

        public RDXOeeKpiQueries(CancellationToken token)
        {
            _opcUaQueries = new RDXOpcUaQueries(token);
        }

        public RDXOeeKpiQueries(RDXOpcUaQueries opcUaQueries, CancellationToken token)
        {
            _opcUaQueries = opcUaQueries;
        }

        /// <summary>
        /// Queries all intervals, servers and nodes in a topology for new values.
        /// Updates all relevances and checks for alerts.
        /// Returns list of task. 
        /// Caller must wait for tasks to run to completion before topology is fully up to date.
        /// </summary>
        /// <param name="totalSearchSpan">Search span of query</param>
        /// <param name="topology">Topology to update</param>
        /// <param name="fullQuery">The cached query for the search span</param>
        /// <param name="opcUaServers">List of OPC UA servers to query for values</param>
        /// <param name="tasks">List of async tasks processing queries</param>
        /// <param name="aggregateIndex">The index of the aggregate in the topology nodes</param>
        public async Task ScheduleAllOeeKpiQueries(
            DateTimeRange totalSearchSpan,
            ContosoTopology topology,
            RDXCachedAggregatedQuery fullQuery,
            List<string> opcUaServers,
            List<Task> tasks,
            int aggregateIndex)
        {
            RDXAggregatedQueryCache queryCache = new RDXAggregatedQueryCache();
            queryCache.List.Add(fullQuery);

            foreach (string appUri in opcUaServers)
            {
                Station station = topology[appUri] as Station;
                if (station != null)
                {
                    ContosoAggregatedOeeKpiHistogram oeeKpiHistogram = station[aggregateIndex];
                    DateTime toTime = totalSearchSpan.To;
                    int intervals = oeeKpiHistogram.Intervals.Count;
                    oeeKpiHistogram.EndTime = toTime;
                    station[aggregateIndex].EndTime = toTime;

                    foreach (ContosoAggregatedOeeKpiTimeSpan oeeKpiTimeSpan in oeeKpiHistogram.Intervals)
                    {
                        DateTime fromTime;

                        // first interval is current time 
                        if (totalSearchSpan.To != toTime || intervals == 1)
                        {
                            fromTime = toTime.Subtract(oeeKpiTimeSpan.IntervalTimeSpan);
                        }
                        else
                        {
                            fromTime = RDXUtils.RoundDateTimeToTimeSpan(toTime.Subtract(TimeSpan.FromSeconds(1)), oeeKpiTimeSpan.IntervalTimeSpan);
                        }

                        DateTimeRange intervalSearchSpan = new DateTimeRange(fromTime, toTime);

                        if (toTime <= oeeKpiTimeSpan.EndTime)
                        {
                            // The interval is still up to date from a previous query, skip
                            toTime = fromTime;
                            continue;
                        }

                        oeeKpiTimeSpan.EndTime = toTime;
                        toTime = fromTime;

                        // find a cached query, if not try to cache the aggregation for this search span
                        RDXCachedAggregatedQuery aggregatedQuery = queryCache.Find(intervalSearchSpan);
                        if (aggregatedQuery == null)
                        {
                            RDXCachedAggregatedQuery newQuery = new RDXCachedAggregatedQuery(_opcUaQueries);
                            queryCache.List.Add(newQuery);
                            tasks.Add(newQuery.Execute(intervalSearchSpan));
                            aggregatedQuery = queryCache.Find(intervalSearchSpan);
                        }

                        RDXOeeKpiQuery oeeKpiQuery = new RDXOeeKpiQuery(_opcUaQueries, intervalSearchSpan, appUri, oeeKpiTimeSpan);

                        await oeeKpiQuery.QueryAllNodes(tasks, station.NodeList, oeeKpiHistogram.AwaitTasks, aggregatedQuery);

                        if (oeeKpiHistogram.CheckAlerts)
                        {
                            station.Status = ContosoPerformanceStatus.Good;
                            foreach (ContosoOpcUaNode node in station.NodeList)
                            {
                                if (node.Minimum != null || node.Maximum != null)
                                {
                                    station.Status |= await CheckAlert(intervalSearchSpan, appUri, station, node);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Test alert condition for nodes
        /// </summary>
        /// <param name="searchSpan"></param>
        /// <param name="appUri"></param>
        /// <param name="topologyNode"></param>
        /// <param name="node"></param>
        public async Task<ContosoPerformanceStatus> CheckAlert(DateTimeRange searchSpan, string appUri, ContosoTopologyNode topologyNode, ContosoOpcUaNode node)
        {
            double value = await _opcUaQueries.GetLatestQuery(searchSpan.To, appUri, node.NodeId);
            // Check for an alert condition.
            if (node.Minimum != null && value < node.Minimum)
            {
                // Add an alert to the server this OPC UA node belongs to.
                ContosoAlert alert = new ContosoAlert(ContosoAlertCause.AlertCauseValueBelowMinimum, appUri, node.NodeId, searchSpan.To);
                topologyNode.AddAlert(alert);
                node.Status = ContosoPerformanceStatus.Poor;
            }
            else if (node.Maximum != null && value > node.Maximum)
            {
                // Add an alert to the server this OPC UA node belongs to.
                ContosoAlert alert = new ContosoAlert(ContosoAlertCause.AlertCauseValueAboveMaximum, appUri, node.NodeId, searchSpan.To);
                topologyNode.AddAlert(alert);
                node.Status = ContosoPerformanceStatus.Poor;
            }
            else
            {
                node.Status = ContosoPerformanceStatus.Good;
            }
            return node.Status;
        }
    }
}

