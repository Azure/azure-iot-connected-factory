using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Rdx.SystemExtensions;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Contoso;
using Microsoft.Rdx.Client.Query.ObjectModel.Aggregates;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.RDX
{
    /// <summary>
    /// RDX related utility functions
    /// </summary>
    public static class RDXUtils
    {

        /// <summary>
        /// Calc the aggregation range for given aggregation histogram
        /// </summary>
        /// <param name="aggregatedTimeSpan">Aggregated Histogram</param>
        /// <param name="toTime">End time of aggregation</param>
        /// <returns>The aggregation range</returns>
        public static DateTimeRange CalcAggregationRange(ContosoAggregatedOeeKpiHistogram aggregatedTimeSpan, DateTime toTime)
        {
            TimeSpan intervalTimeSpan = aggregatedTimeSpan[0].IntervalTimeSpan;
            DateTimeRange aggregateSpan = new DateTimeRange(
                (aggregatedTimeSpan.Intervals.Count == 1) ?
                toTime.Subtract(intervalTimeSpan) :
                RDXUtils.RoundDateTimeToTimeSpan(
                    toTime.Subtract(TimeSpan.FromSeconds(1)),
                    intervalTimeSpan),
                toTime
            );
            return aggregateSpan;
        }

        /// <summary>
        /// Get DateTimeRange to UtcNow, from rounded to timespan range
        /// </summary>
        public static DateTimeRange RoundDateTimeRangeFromNow(TimeSpan timeSpan)
        {
            DateTime toTime = DateTime.UtcNow;
            DateTimeRange range = new DateTimeRange(
                RDXUtils.RoundDateTimeToTimeSpan(
                    toTime.Subtract(TimeSpan.FromSeconds(1)),
                    timeSpan),
                toTime
            );
            return range;
        }


        /// <summary>
        /// Calc the search range for given aggregation histogram
        /// </summary>
        /// <param name="aggregatedTimeSpan">Aggregated Histogram</param>
        /// <returns>The search range</returns>
        public static DateTimeRange TotalSearchRangeFromNow(ContosoAggregatedOeeKpiHistogram aggregatedTimeSpan)
        {
            DateTime endTime = RDXUtils.RoundDateTimeToTimeSpan(
                DateTime.UtcNow,
                aggregatedTimeSpan.UpdateTimeSpan);
            DateTime startTime = endTime.Subtract(aggregatedTimeSpan.TotalTimeSpan);

            if (aggregatedTimeSpan.UpdateTimeSpan != aggregatedTimeSpan.IntervalTimeSpan)
            {
                if (aggregatedTimeSpan.TotalTimeSpan > aggregatedTimeSpan.IntervalTimeSpan)
                {
                    // The full search range is totaltime + interval
                    startTime = startTime.Subtract(aggregatedTimeSpan.IntervalTimeSpan);
                }

                startTime = startTime.Subtract(aggregatedTimeSpan.UpdateTimeSpan);
            }
            return new DateTimeRange(startTime, endTime);
        }

        /// <summary>
        /// Round a date and time to a given time span period.
        /// Time span must be given in either days, hours, minutes or seconds.
        /// Hours, minutes and seconds must be a divisible of a 24h, 60m or 60s.
        /// </summary>
        /// <param name="dateTime">The date and time to round</param>
        /// <param name="period">The time period to round to</param>
        /// <returns>The rounded time</returns>
        public static DateTime RoundDateTimeToTimeSpan(DateTime dateTime, TimeSpan period)
        {
            int year = dateTime.Year;
            int month = dateTime.Month;
            int day = dateTime.Day;
            int hour = dateTime.Hour;
            int minute = dateTime.Minute;
            int second = dateTime.Second;
            DateTime result;

            if (period.Days > 0)
            {
                // round to midnight
                int daysToSubtract = period.Days;
                result = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
                if (hour != 0 || minute != 0 || second != 0)
                {
                    daysToSubtract--;
                }
                result.Subtract(TimeSpan.FromDays(daysToSubtract));
            }
            else if (period.Hours > 0 && (24 % period.Hours) == 0)
            {
                // round to the full hours 
                int hoursToSubtract = hour % period.Hours;
                result = new DateTime(year, month, day, hour - hoursToSubtract, 0, 0, DateTimeKind.Utc);
            }
            else if (period.Minutes > 0 && (60 % period.Minutes) == 0)
            {
                // round to the minute
                int minutesToSubtract = minute % period.Minutes;
                result = new DateTime(year, month, day, hour, minute - minutesToSubtract, 0, DateTimeKind.Utc);
            }
            else if (period.Seconds > 0 && (60 % period.Seconds) == 0)
            {
                // round to the seconds
                int secondsToSubtract = second % period.Seconds;
                result = new DateTime(year, month, day, hour, minute, second - secondsToSubtract, DateTimeKind.Utc);
            }
            else
            {
                throw new Exception(String.Format("Unsupported Timespan {0} specified for rounding", period));
            }

            return result;
        }

        /// <summary>
        /// Helper to wait for all tasks in the list.
        /// </summary>
        /// <param name="what">Text for Trace Information</param>
        /// <param name="tasks">List of tasks</param>
        /// <param name="stopWatch">Stop watch of tasks</param>
        public static async Task WhenAllTasks(string what, List<Task> tasks, Stopwatch stopWatch)
        {
            // wait for all query tasks to finish and count statistics
            int successfullTasks = 0;
            int failedTasks = 0;
            int cancelledTasks = 0;
            int otherTasks = 0;

            await Task.WhenAll(tasks.ToArray());

            foreach (Task task in tasks)
            {
                switch (task.Status)
                {
                    case TaskStatus.RanToCompletion:
                        successfullTasks++;
                        break;
                    case TaskStatus.Canceled:
                        cancelledTasks++;
                        break;
                    case TaskStatus.Faulted:
                        failedTasks++;
                        break;
                    default:
                        otherTasks++;
                        break;
                }
            }

            RDXTrace.TraceInformation("WhenAllTasks {5} finished after {0}ms: Succeeded:{1} Failed:{2} Cancel:{3} Other:{4}",
                stopWatch.ElapsedMilliseconds, successfullTasks, failedTasks, cancelledTasks, otherTasks, what);

        }

        /// <summary>
        /// Test if operator can be aggregated by RDX 
        /// </summary>
        /// <param name="opCode"></param>
        /// <returns>True if operator is aggregated</returns>
        public static bool IsAggregatedOperator(ContosoOpcNodeOpCode opCode)
        {
            switch (opCode)
            {
                case ContosoOpcNodeOpCode.Avg:
                case ContosoOpcNodeOpCode.Sum:
                case ContosoOpcNodeOpCode.Count:
                case ContosoOpcNodeOpCode.Max:
                case ContosoOpcNodeOpCode.Min:
                case ContosoOpcNodeOpCode.SubMaxMin:
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Return the index of an operator result in a query measure.
        /// </summary>
        /// <param name="opCode">Operation</param>
        /// <returns>The index</returns>
        public static int AggregatedOperatorIndex(ContosoOpcNodeOpCode opCode)
        {
            switch (opCode)
            {
                case ContosoOpcNodeOpCode.Avg: return (int)RDXOpcUaQueries.AggregateIndex.Average;
                case ContosoOpcNodeOpCode.Sum: return (int)RDXOpcUaQueries.AggregateIndex.Sum;
                case ContosoOpcNodeOpCode.Count: return (int)RDXOpcUaQueries.AggregateIndex.Count;
                case ContosoOpcNodeOpCode.Max: return (int)RDXOpcUaQueries.AggregateIndex.Max;
                case ContosoOpcNodeOpCode.Min: return (int)RDXOpcUaQueries.AggregateIndex.Min;
            }
            return 0;
        }

        /// <summary>
        /// Return an array of DateTime with start time of each interval.
        /// </summary>
        /// <param name="aggregateFrom">The start time of the intervals</param>
        /// <param name="intervalTimeSpan">The timeSpan of each interval</param>
        /// <param name="dimension">The dimension of the array</param>
        public static DateTime[] CreateAggregateDateTimeArray(DateTime aggregateFrom, TimeSpan intervalTimeSpan, int dimension)
        {
            DateTime[] result = new DateTime[dimension];
            DateTime iterator = aggregateFrom;
            for (int i=0; i<dimension; i++)
            {
                result[i] = iterator;
                iterator += intervalTimeSpan;
            }
            return result;
        }

        /// <summary>
        /// Data structure to return x and y values for time series display
        /// </summary>
        public struct AggregatedTimeSeriesResult
        {
            public int Count { get; }
            public double[] YValues { get; set; }
            public DateTime[] XTime { get; }
            public string Units { get; }
            public DateTime EndTime { get; }
            public TimeSpan Interval { get; }

            public AggregatedTimeSeriesResult(int count, DateTime endTime, TimeSpan interval, string units = "")
            {
                Count = count;
                Units = units;
                YValues = new double[count];
                XTime = new DateTime[count];
                EndTime = endTime;
                Interval = interval;
                DateTime startTime = DateTime.MinValue;
                if (endTime > DateTime.MinValue)
                {
                    startTime = endTime.Subtract(TimeSpan.FromSeconds(interval.TotalSeconds * count));
                }
                for (int i = 0; i < count; i++)
                {
                    XTime[i] = startTime;
                    startTime += interval;
                }
            }
        }

        /// <summary>
        /// Return the time series for a given OPC UA server and a given node
        /// </summary>
        /// <param name="station">The OPC UA server</param>
        /// <param name="node">The OPC UA node Id</param>
        /// <param name="aggregationView">The hourly, daily or weekly view</param>
        /// <param name="getCount">Get event Count aggregate</param>
        public static async Task<AggregatedTimeSeriesResult> AggregatedNodeId(
            Station station,
            ContosoOpcUaNode node,
            ContosoTopologyNode.AggregationView aggregationView,
            bool getCount = false)
        {
            int aggregateIndex = (int)aggregationView;
            RDXOpcUaQueries opcUaQuery = new RDXOpcUaQueries(CancellationToken.None);
            ContosoAggregatedOeeKpiHistogram aggregatedTimeSpan = station[aggregateIndex];
            DateTimeRange searchSpan = RDXUtils.TotalSearchRangeFromNow(aggregatedTimeSpan);
            DateTimeRange aggregateSpan = new DateTimeRange(
                    searchSpan.To.Subtract(aggregatedTimeSpan.TotalTimeSpan),
                    searchSpan.To
                );
            double roundFactor = 100.0;

            int index = (int)RDXOpcUaQueries.AggregateIndex.Count;
            if (!getCount)
            {
                if (!IsAggregatedOperator(node.OpCode))
                {
                    throw new Exception("Unsupported Operator for aggregation");
                }

                // handle special case for SubMaxMin, result is derived by substraction
                if (node.OpCode == ContosoOpcNodeOpCode.SubMaxMin)
                {
                    index = (int)RDXOpcUaQueries.AggregateIndex.Max;
                }
                else
                {
                    index = AggregatedOperatorIndex(node.OpCode);
                }
            }

            int resultDimension = aggregatedTimeSpan.Intervals.Count;
            DateTime [] dateTimeResult = CreateAggregateDateTimeArray(aggregateSpan.From, aggregatedTimeSpan.IntervalTimeSpan, resultDimension);

            AggregateResult queryResult = await opcUaQuery.GetAllAggregatedNodesWithInterval(
                aggregateSpan,
                station.Key,
                node.NodeId,
                aggregatedTimeSpan.IntervalTimeSpan
                );

            int count = queryResult.Aggregate.Dimension.Count;
            AggregatedTimeSeriesResult result = new AggregatedTimeSeriesResult(resultDimension, aggregateSpan.To, aggregatedTimeSpan.IntervalTimeSpan, node.Units);
            if (queryResult != null)
            {
                for (int i = 0; i < count; i++)
                {
                    double? value = queryResult.Aggregate.Aggregate.Measures.TryGetPropertyMeasure<double?>(new int[] { 0, i, index });
                    if (value != null)
                    {
                        // find matching date/time slot for value
                        var dateTime = queryResult.Aggregate.Dimension[i];
                        int resultIndex = Array.IndexOf(dateTimeResult, dateTime);
                        if (resultIndex >= 0)
                        {
                            result.YValues[resultIndex] = roundFactor * (double)value;
                            if (node.OpCode == ContosoOpcNodeOpCode.SubMaxMin)
                            {
                                value = queryResult.Aggregate.Aggregate.Measures.TryGetPropertyMeasure<double?>(new int[] { 0, i, (int)RDXOpcUaQueries.AggregateIndex.Min });
                                if (value != null)
                                {
                                    result.YValues[resultIndex] -= roundFactor * (double)value;
                                }
                            }
                            result.YValues[resultIndex] = Math.Round(result.YValues[resultIndex]) / roundFactor;
                        }
                        else
                        {
                            throw new Exception("DateTime not found in aggregated query array");
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Return scale factor for opcode display in a time series.
        /// </summary>
        private static double OpCodeScaleFactor(ContosoOpcNodeOpCode opcode, TimeSpan interval, TimeSpan scaledInterval)
        {
            switch (opcode)
            {
                case ContosoOpcNodeOpCode.SubMaxMin:
                case ContosoOpcNodeOpCode.Sum:
                    return scaledInterval.TotalSeconds / interval.TotalSeconds;
                case ContosoOpcNodeOpCode.Max:
                case ContosoOpcNodeOpCode.Min:
                case ContosoOpcNodeOpCode.Avg:
                    return 1.0;
            }
            throw new Exception("Invalid op code for operation");
        }


        /// <summary>
        /// Test the aggregation view for all stations and nodes.
        /// </summary>
        /// <param name="topology"></param>
        public static async Task TestAggregatedNodeId(ContosoTopology topology)
        {
            foreach (ContosoTopologyNode.AggregationView aggregationView in Enum.GetValues(typeof(ContosoTopologyNode.AggregationView)))
            {
                List<string> stations = topology.GetAllChildren(topology.TopologyRoot.Key, typeof(Station));
                foreach (string appUri in stations)
                {
                    Station station = topology[appUri] as Station;
                    if (station != null)
                    {
                        foreach (ContosoOpcUaNode node in station.NodeList)
                        {
                            if (IsAggregatedOperator(node.OpCode))
                            {
                                await RDXUtils.AggregatedNodeId(station, node, aggregationView);
                            }
                        }
                    }
                }
            }
        }
    }
}

