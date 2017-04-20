using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Rdx.SystemExtensions;
using Microsoft.Rdx.Client.Events;
using Microsoft.Rdx.Client.Query;
using Microsoft.Rdx.Client.Query.Expressions;
using Microsoft.Rdx.Client.Query.ObjectModel.LimitExpressions;
using Microsoft.Rdx.Client.Query.ObjectModel.Aggregates;
using Microsoft.Rdx.Client.Query.ObjectModel.Predicates;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.RDX
{
    /// <summary>
    /// Queries for OPC UA servers
    /// </summary>
    public partial class RDXOpcUaQueries
    {
        /// <summary>
        /// Order of aggregates in queries
        /// </summary>
        public enum AggregateIndex : int
        {
            Count = 0,
            Min = 1,
            Max = 2,
            Average = 3,
            Sum = 4,
        };

        /// <summary>
        /// OPC UA Server Uri and node specific constants
        /// Nodes published from a OPC UA server in JSON format
        /// to IotHub follows these naming conventions
        /// </summary>
        public const string OpcServerUri = "ApplicationUri";
        public const string OpcMonitoredItemId = "NodeId";
        public const string OpcMonitoredItemValue = "Value.Value";
        public const string OpcDisplayName = "DisplayName";
        public const string OpcMonitoredItemTimestamp = "Value.SourceTimestamp";

        /// <summary>
        /// limits for OPC UA max dimension values
        /// </summary>
        const int opcMaxServerUri = 100;        // 100 servers
        const int opcMaxMonitoredItemId = 500;  // 500 nodes per server

        /// <summary>
        /// Default predicate strings to query for specific OPC UA servers or OPC UA nodeId 
        /// </summary>
        public const string OpcServerPredicate =
            "[" + OpcServerUri + "] HAS '{0}'";
        public const string OpcServerNodePredicate =
            "[" + OpcServerUri + "] HAS '{0}' AND [" + OpcMonitoredItemId + "] = '{1}'";
        public const string OpcNodePredicate = "[" + OpcMonitoredItemId + "] = '{0}'";

        /// <summary>
        /// Task runner helper
        /// </summary>
        private CancellationToken _cancellationToken;

        /// <summary>
        /// Ctor for the OPC UA query class
        /// </summary>
        /// <param name="token"></param>
        public RDXOpcUaQueries(CancellationToken token)
        {
            _cancellationToken = token;
        }

        /// <summary>
        /// Query to subtract the oldest value of a OPC UA node
        /// from the newest value in the search span
        /// </summary>
        /// <param name="searchSpan">Date and time span for the query</param>
        /// <param name="appUri">The OPC UA server application Uri</param>
        /// <param name="nodeId">The node id in the OPC UA server namespace</param>
        /// <returns>The difference</returns>
        public async Task<double> DiffQuery(DateTimeRange searchSpan, string appUri, string nodeId)
        {
            try
            {
                PredicateStringExpression expression = new PredicateStringExpression(
                    String.Format(OpcServerNodePredicate, appUri, nodeId));

                BaseLimitClause firstMember = new TopLimitClause(1,
                    new[]
                    {
                        new SortClause(new BuiltInPropertyReferenceExpression(BuiltInProperty.Timestamp),
                            SortOrderType.Asc)
                    });
                BaseLimitClause lastMember = new TopLimitClause(1,
                    new[]
                    {
                        new SortClause(new BuiltInPropertyReferenceExpression(BuiltInProperty.Timestamp),
                            SortOrderType.Desc)
                    });

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                Task<IEnumerable<IEvent>> firstTask = RDXQueryClient.GetEventsAsync(
                        searchSpan,
                        expression,
                        firstMember,
                        _cancellationToken);
                Task<IEnumerable<IEvent>> lastTask = RDXQueryClient.GetEventsAsync(
                        searchSpan,
                        expression,
                        lastMember,
                        _cancellationToken);
                await Task.WhenAll(new Task[] { firstTask, lastTask });
                IEnumerable<IEvent> firstEvent = firstTask.Result;
                IEnumerable<IEvent> lastEvent = lastTask.Result;
                stopwatch.Stop();
                RDXTrace.TraceInformation("DiffQuery queries took {0} ms", stopwatch.ElapsedMilliseconds);

                long first = GetValueOfProperty<long>(firstEvent.First<IEvent>(), OpcMonitoredItemValue);
                long last = GetValueOfProperty<long>(lastEvent.First<IEvent>(), OpcMonitoredItemValue);
                return last - first;
            }
            catch (Exception e)
            {
                RDXTrace.TraceError("DiffQuery Exception {0}", e.Message);
                return 0;
            }
        }

        /// <summary>
        /// Query for aggregation of count, min, max, sum and average for all active 
        /// nodes of the given OPC UA server in the given search span
        /// </summary>
        /// <param name="searchSpan">Date and time span for the query</param>
        /// <param name="appUri">The OPC UA server application Uri</param>
        /// <returns>The aggregated nodes</returns>
        public async Task<AggregateResult> GetAllAggregatedNodes(DateTimeRange searchSpan, string appUri)
        {
            try
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                PredicateStringExpression predicate = new PredicateStringExpression(
                    String.Format(OpcServerPredicate, appUri));

                Aggregate aggregate = new Aggregate(
                    Expression.UniqueValues(OpcMonitoredItemId, PropertyType.String, opcMaxMonitoredItemId),
                    new Aggregate(
                        Expression.Count(),
                        Expression.Min(OpcMonitoredItemValue, PropertyType.Double),
                        Expression.Max(OpcMonitoredItemValue, PropertyType.Double),
                        Expression.Average(OpcMonitoredItemValue, PropertyType.Double),
                        Expression.Sum(OpcMonitoredItemValue, PropertyType.Double)
                ));

                AggregatesResult aggregateResults = await RDXQueryClient.GetAggregatesAsync(
                    searchSpan,
                    predicate,
                    new[] { aggregate },
                    _cancellationToken);

                // Since there was 1 top level aggregate in request, there is 1 aggregate result.
                AggregateResult aggregateResult = aggregateResults[0];

                stopwatch.Stop();
                RDXTrace.TraceInformation("GetAllAggregatedNodes query took {0} ms", stopwatch.ElapsedMilliseconds);

                return aggregateResult;
            }
            catch (Exception e)
            {
                RDXTrace.TraceError("GetAllAggregatedNodes: Exception {0}", e.Message);
                return null;
            }
        }

        /// <summary>
        /// Convert Timespan to RDX Interval type.
        /// </summary>
        private static string TimeSpanToId(TimeSpan interval)
        {
            if (interval.Days > 0)
            {
                return String.Format("{0}d", interval.Days);
            }
            if (interval.Hours > 0)
            {
                return String.Format("{0}h", interval.Hours);
            }
            if (interval.Minutes > 0)
            {
                return String.Format("{0}m", interval.Minutes);
            }
            if (interval.Seconds > 0)
            {
                return String.Format("{0}s", interval.Seconds);
            }
            if (interval.Milliseconds > 0)
            {
                return String.Format("{0}ms", interval.Milliseconds);
            }
            throw new Exception("Invalid Timespan specified for RDX Histogram");
        }

        /// <summary>
        /// Query for aggregation of count, min, max, sum and average for all active 
        /// nodes of the given OPC UA server in the given search span as time histogram
        /// with interval
        /// </summary>
        /// <param name="searchSpan">Date and time span for the query</param>
        /// <param name="appUri">The OPC UA server application Uri</param>
        /// <param name="interval">Interval for Date Time Histogram</param>
        /// <returns>The aggregated nodes</returns>
        public async Task<AggregateResult> GetAllAggregatedNodesWithInterval(DateTimeRange searchSpan, string appUri, string nodeId, TimeSpan interval)
        {
            try
            {
                string id = TimeSpanToId(interval);

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                PredicateStringExpression predicate = new PredicateStringExpression(
                    String.Format(OpcServerNodePredicate, appUri, nodeId));
                Aggregate aggregate = new Aggregate(
                    Expression.UniqueValues(OpcMonitoredItemId, PropertyType.String, opcMaxMonitoredItemId),
                        new Aggregate(Expression.DateHistogram(BuiltInProperty.Timestamp, IntervalSize.FromId(id)),
                            new Aggregate(
                                Expression.Count(),
                                Expression.Min(OpcMonitoredItemValue, PropertyType.Double),
                                Expression.Max(OpcMonitoredItemValue, PropertyType.Double),
                                Expression.Average(OpcMonitoredItemValue, PropertyType.Double),
                                Expression.Sum(OpcMonitoredItemValue, PropertyType.Double)
                )));

                AggregatesResult aggregateResults = await RDXQueryClient.GetAggregatesAsync(
                    searchSpan,
                    predicate,
                    new[] { aggregate },
                    _cancellationToken);

                // Since there was 1 top level aggregate in request, there is 1 aggregate result.
                AggregateResult aggregateResult = aggregateResults[0];

                stopwatch.Stop();
                RDXTrace.TraceInformation("GetAllAggregatedNodes query took {0} ms", stopwatch.ElapsedMilliseconds);

                return aggregateResult;
            }
            catch (Exception e)
            {
                RDXTrace.TraceError("GetAllAggregatedNodes: Exception {0}", e.Message);
                return null;
            }
        }

        /// <summary>
        /// Query for aggregation of count, min, max, sum and average for all active 
        /// nodes of all OPC UA servers in the given search span
        /// </summary>
        /// <param name="searchSpan">Date and time span for the query</param>
        /// <returns>The aggregated servers and nodes</returns>
        public async Task<AggregateResult> GetAllAggregatedStationsAndNodes(DateTimeRange searchSpan)
        {
            try
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                Aggregate aggregate = new Aggregate(
                    Expression.UniqueValues(OpcServerUri, PropertyType.String, opcMaxServerUri),
                    new Aggregate(
                        Expression.UniqueValues(OpcMonitoredItemId, PropertyType.String, opcMaxMonitoredItemId),
                        new Aggregate(
                            Expression.Count(),
                            Expression.Min(OpcMonitoredItemValue, PropertyType.Double),
                            Expression.Max(OpcMonitoredItemValue, PropertyType.Double),
                            Expression.Average(OpcMonitoredItemValue, PropertyType.Double),
                            Expression.Sum(OpcMonitoredItemValue, PropertyType.Double)
                        )
                    )
                );

                AggregatesResult aggregateResults = await RDXQueryClient.GetAggregatesAsync(
                    searchSpan,
                    null,
                    new[] { aggregate },
                    _cancellationToken);

                // Since there was 1 top level aggregate in request, there is 1 aggregate result.
                AggregateResult aggregateResult = aggregateResults[0];

                stopwatch.Stop();
                RDXTrace.TraceInformation("GetAllAggregatedStationsAndNodes query took {0} ms", stopwatch.ElapsedMilliseconds);

                return aggregateResult;
            }
            catch (Exception e)
            {
                RDXTrace.TraceError("GetAllAggregatedStationsAndNodes: Exception {0}", e.Message);
                return null;
            }
        }

        /// <summary>
        /// Query for aggregation of a given measure for the 
        /// given node and given OPC UA server in the given search span
        /// </summary>
        /// <param name="searchSpan">Date and time span for the query</param>
        /// <param name="appUri">The OPC UA server application Uri</param>
        /// <param name="nodeId">The node id in the OPC UA server namespace</param>
        /// <param name="measure"></param>
        public async Task<double> GetAggregatedNode(DateTimeRange searchSpan, string appUri, string nodeId, AggregateExpression measure)
        {
            try
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                PredicateStringExpression predicate = new PredicateStringExpression(
                    String.Format(OpcServerNodePredicate, appUri, nodeId));
                Aggregate aggregate = new Aggregate(
                    measures: new AggregateExpression[] { measure }
                    );

                AggregatesResult aggregateResults = await RDXQueryClient.GetAggregatesAsync(
                    searchSpan,
                    predicate,
                    new[] { aggregate },
                    _cancellationToken);

                // Since there was 1 top level aggregate in request, there is 1 aggregate result.
                AggregateResult aggregateResult = aggregateResults[0];

                stopwatch.Stop();
                RDXTrace.TraceInformation("AggregateQuery query took {0} ms", stopwatch.ElapsedMilliseconds);

                if (aggregateResult.Measures == null)
                {
                    return 0.00;
                }

                return (double)aggregateResult.Measures[0];
            }
            catch (Exception e)
            {
                RDXTrace.TraceError("AggregateQuery: Exception {0}", e.Message);
                return 0;
            }
        }

        /// <summary>
        /// Helper to query for last event of given station and nodeid
        /// </summary>
        private Task<IEnumerable<IEvent>> GetLatestEvent(DateTime endTime, string appUri, string nodeId)
        {
            PredicateStringExpression expression = new PredicateStringExpression(
                String.Format(OpcServerNodePredicate, appUri, nodeId));
            DateTimeRange searchSpan = new DateTimeRange(endTime.Subtract(TimeSpan.FromDays(360)), endTime);
            BaseLimitClause lastMember = new TopLimitClause(1,
                new[]
                {
                    new SortClause(new BuiltInPropertyReferenceExpression(BuiltInProperty.Timestamp),
                        SortOrderType.Desc)
                });
            return RDXQueryClient.GetEventsAsync(
                    searchSpan,
                    expression,
                    lastMember,
                    _cancellationToken);
        }

        /// <summary>
        /// Query for latest event of the given node and 
        /// given OPC UA server from the given time and date
        /// </summary>
        /// <param name="endTime">Time to start from querying in the past</param>
        /// <param name="appUri">The OPC UA server application Uri</param>
        /// <param name="nodeId">The node id in the OPC UA server namespace</param>
        public async Task<double> GetLatestQuery(DateTime endTime, string appUri, string nodeId)
        {
            try
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                Task<IEnumerable<IEvent>> lastTask = GetLatestEvent(endTime, appUri, nodeId);
                IEnumerable<IEvent> lastEvent = await lastTask;
                stopwatch.Stop();
                RDXTrace.TraceInformation("GetLatestQuery took {0} ms", stopwatch.ElapsedMilliseconds);
                double last;
                try
                {
                    last = GetValueOfProperty<double>(lastEvent.First<IEvent>(), OpcMonitoredItemValue);
                }
                catch
                {
                    last = GetValueOfProperty<long>(lastEvent.First<IEvent>(), OpcMonitoredItemValue);
                }
                return last;
            }
            catch (Exception e)
            {
                RDXTrace.TraceError("GetLatestQuery Exception {0}", e.Message);
                return 0;
            }
        }

        /// <summary>
        /// Query for latest event of the given node and given OPC UA server 
        /// from the given time and date to find the display name for the nodeId.
        /// </summary>
        /// <param name="endTime">Time to start from querying in the past</param>
        /// <param name="appUri">The OPC UA server application Uri</param>
        /// <param name="nodeId">The node id in the OPC UA server namespace</param>
        /// <returns>The DisplayName of the nodeId</returns>
        public async Task<string> QueryDisplayName(DateTime endTime, string appUri, string nodeId)
        {
            try
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                Task<IEnumerable<IEvent>> lastTask = GetLatestEvent(endTime, appUri, nodeId);
                IEnumerable<IEvent> lastEvent = await lastTask;
                stopwatch.Stop();
                string name = GetValueOfProperty<string>(lastEvent.First<IEvent>(), OpcDisplayName);
                RDXTrace.TraceInformation("GetDisplayName took {0} ms", stopwatch.ElapsedMilliseconds);
                return name;
            }
            catch (Exception e)
            {
                RDXTrace.TraceError("GetDisplayName Exception {0}", e.Message);
            }
            return nodeId;
        }


        /// <summary>
        /// Query for all active OPC UA servers in given search span
        /// </summary>
        /// <param name="searchSpan">Date and time span for the query</param>
        /// <returns>List of active servers</returns>
        public async Task<StringDimensionResult> AggregateServers(DateTimeRange searchSpan)
        {
            Aggregate aggregate = new Aggregate(
                Expression.UniqueValues(OpcServerUri, PropertyType.String, opcMaxServerUri));

            AggregatesResult aggregateResults = await RDXQueryClient.GetAggregatesAsync(
                searchSpan,
                null,
                new[] { aggregate },
                _cancellationToken);

            // Since there was 1 top level aggregate in request, there is 1 aggregate result.
            AggregateResult aggregateResult = aggregateResults[0];
            StringDimensionResult dimension = aggregateResult.Dimension as StringDimensionResult;
            return dimension;
        }

        /// <summary>
        /// Measure Sum for GetAggregatedNode function
        /// Adds all values in the aggregation
        /// </summary>
        public static AggregateExpression SumValues()
        {
            return Expression.Sum(OpcMonitoredItemValue, PropertyType.Double);
        }

        /// <summary>
        /// Measure Average for GetAggregatedNode function
        /// Averages all values in the aggregation
        /// </summary>
        public static AggregateExpression AverageValues()
        {
            return Expression.Average(OpcMonitoredItemValue, PropertyType.Double);
        }

        /// <summary>
        /// Measure Maximum for GetAggregatedNode function
        /// Finds maximum of all values in the aggregation
        /// </summary>
        public static AggregateExpression MaxValues()
        {
            return Expression.Max(OpcMonitoredItemValue, PropertyType.Double);
        }

        /// <summary>
        /// Measure Minimum for GetAggregatedNode function
        /// Finds minimum of all values in the aggregation
        /// </summary>
        public static AggregateExpression MinValues()
        {
            return Expression.Min(OpcMonitoredItemValue, PropertyType.Double);
        }

        /// <summary>
        /// Helper to read a property from an event
        /// </summary>
        /// <typeparam name="T">The type of the property</typeparam>
        /// <param name="iEvent">The event</param>
        /// <param name="property">The property string</param>
        /// <returns>The value of the property</returns>
        private static T GetValueOfProperty<T>(IEvent iEvent, string property)
        {
            var result = iEvent.EventSchema.Properties.IndexOf(p => (p.Name == property));
            return (T)iEvent.Values[result];
        }
    }
}

