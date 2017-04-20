using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Contoso;
using Microsoft.Rdx.SystemExtensions;
using Microsoft.Rdx.Client.Query.ObjectModel.Aggregates;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.RDX
{
    /// <summary>
    /// Cache for Aggregated queries. Keeps list of queries with a given search span.
    /// </summary>
    public class RDXAggregatedQueryCache
    {
        /// <summary>
        /// The list with all queries
        /// </summary>
        public List<RDXCachedAggregatedQuery> List = new List<RDXCachedAggregatedQuery>();

        /// <summary>
        /// Get the aggregated query of a specific search span.
        /// </summary>
        /// <param name="searchSpan">Date and time span for the query</param>
        /// <returns>Aggregated query or null if not found</returns>
        public RDXCachedAggregatedQuery Find(DateTimeRange searchSpan)
        {
            return List.Find(x => x.SearchSpan == searchSpan);
        }
    }

    /// <summary>
    /// Cache entry of an aggregated query.
    /// </summary>
    public class RDXCachedAggregatedQuery
    {
        RDXOpcUaQueries _opcUaQueries;
        AggregateResult _result;
        Task<AggregateResult> _task;
        public DateTimeRange SearchSpan;

        public RDXCachedAggregatedQuery(RDXOpcUaQueries opcUaQueries)
        {
            _opcUaQueries = opcUaQueries;
        }

        /// <summary>
        /// Issue a task for a cached query for all active nodes and stations
        /// </summary>
        /// <param name="searchSpan">Date and time span for the query</param>
        public Task Execute(DateTimeRange searchSpan)
        {
            SearchSpan = searchSpan;
            _task = _opcUaQueries.GetAllAggregatedStationsAndNodes(searchSpan);
            return _task;
        }

        /// <summary>
        /// Get the value of an operation for OPC UA server Node Id
        /// </summary>
        /// <param name="opCode">Operation to get the value</param>
        /// <param name="appUri">The OPC UA server application Uri</param>
        /// <param name="nodeId">The node id in the OPC UA server namespace</param>
        /// <returns>The value</returns>
        private double GetValue(ContosoOpcNodeOpCode opCode, string appUri, string nodeId)
        {
            double? value = null;
            var appUriIndex = _result.Dimension.IndexOf(appUri);
            var nodeIdIndex = _result.Aggregate.Dimension.IndexOf(nodeId);
            if (appUriIndex >= 0 && nodeIdIndex >= 0)
            {
                if (opCode == ContosoOpcNodeOpCode.SubMaxMin)
                {
                    double? max = _result.Aggregate.Aggregate.Measures.TryGetPropertyMeasure<double?>(new int[] { appUriIndex, nodeIdIndex, (int)RDXOpcUaQueries.AggregateIndex.Max });
                    if (max == null)
                    {
                        value = 0.0;
                    }
                    else
                    {
                        value = max - _result.Aggregate.Aggregate.Measures.TryGetPropertyMeasure<double?>(new int[] { appUriIndex, nodeIdIndex, (int)RDXOpcUaQueries.AggregateIndex.Min });
                    }
                }
                else
                {
                    value = _result.Aggregate.Aggregate.Measures.TryGetPropertyMeasure<double?>(new int[] { appUriIndex, nodeIdIndex, RDXUtils.AggregatedOperatorIndex(opCode) });
                }
            }
            // The node/station is inactive or there were no events in the searchspan! 
            if (value == null)
            {
                value = 0.0;
            }
            return (double)value;
        }

        /// <summary>
        /// Returns list of active nodeId for a specific server
        /// </summary>
        public List<string> GetActiveNodeIdList(string appUri)
        {
            List<string> result = new List<string>();
            double ? value = null;
            var appUriIndex = _result.Dimension.IndexOf(appUri);
            if (appUriIndex >= 0)
            {
                var nodeCount = _result.Aggregate.Dimension.Count;
                for (int nodeIdIndex = 0; nodeIdIndex < nodeCount; nodeIdIndex++)
                {
                    value = _result.Aggregate.Aggregate.Measures.TryGetPropertyMeasure<double?>(new int[] { appUriIndex, nodeIdIndex, (int)RDXOpcUaQueries.AggregateIndex.Count });
                    if (value != null)
                    {
                        var nodeId = _result.Aggregate.Dimension[nodeIdIndex];
                        result.Add((string)nodeId);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Returns list of servers in aggregated query.
        /// </summary>
        /// <returns>List of active servers</returns>
        public List<string> GetActiveServerList()
        {
            if (_result != null)
            {
                return (List<string>)_result.Dimension;
            }
            else
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// Call the operator on a query and node. 
        /// Gets value from cache if operation is supported, 
        /// if not forwards the call to single query engine.
        /// </summary>
        /// <param name="opCode">Operation to get the value</param>
        /// <param name="rdxQuery">The query</param>
        /// <param name="opcUaNode">The Topology node </param>
        public async Task CallOperator(ContosoOpcNodeOpCode opCode, RDXOeeKpiQuery rdxQuery, ContosoOpcUaNode opcUaNode)
        {
            if (RDXUtils.IsAggregatedOperator(opCode) &&
                _task != null)
            {
                _result = await _task;
                if (_result != null)
                {
                    double? value = GetValue(opCode, rdxQuery.AppUri, opcUaNode.NodeId);
                    if (value != null)
                    {
                        opcUaNode.Last.Value = (double)value;
                        opcUaNode.Last.Time = rdxQuery.SearchSpan.To;
                        opcUaNode.UpdateRelevance(rdxQuery.TopologyNode);
                    }
                }
                return;
            }
            // issue a single query if the operator can not be handled from aggregated values
            await RDXContosoOpCodes.CallOperator(opCode, rdxQuery, opcUaNode);
        }
    }
}

