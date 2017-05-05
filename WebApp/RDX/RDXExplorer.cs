using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Contoso;
using System;
using System.Collections.Generic;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.RDX
{
    /// <summary>
    /// RDX data explorer utility functions
    /// </summary>
    public static class RDXExplorer
    {
        const string rdxExplorerBaseUrl =
            "https://insights.{0}/?environmentId={1}&environmentName={2}&from={3}&to={4}";
        const string rdxExplorerPredicate = "&predicate={0}";
        const string rdxExplorerDimensionProperty = "&dimensionProperty={0}";
        const string rdxExplorerMeasureProperty = "&measureProperty={0}";

        public static long DateTimeToJavascript(DateTime dateTime)
        {
            return (long)dateTime.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
        }

        /// <summary>
        /// Returns a browser Url to inspect range with RDX Explorer
        /// </summary>
        /// <param name="fromDate">start of time range</param>
        /// <param name="toDate">end of time range</param>
        /// <returns>Base Url to RDX Explorer</returns>
        public static string GetExplorerBaseUrl(DateTime fromDate, DateTime toDate)
        {
            string dnsName = RDXQueryClient.DNSName;
            string envId = RDXQueryClient.EnvironmentId;
            string envName = RDXQueryClient.EnvironmentName;
            long from = DateTimeToJavascript(fromDate);
            long to = DateTimeToJavascript(toDate);
            return String.Format(rdxExplorerBaseUrl, dnsName, envId, Uri.EscapeUriString(envName), from, to);
        }

        /// <summary>
        /// Return a Url to open RDX Explorer and show data split by station.
        /// </summary>
        /// <param name="fromDate">start date and time for display range</param>
        /// <param name="toDate">end date and time of display range</param>
        /// <param name="showCount">true to show Count, otherwise Value</param>
        /// <returns>The Url</returns>
        public static string GetExplorerDefaultView(DateTime fromDate, DateTime toDate, bool showCount = true)
        {
            string baseUrl = GetExplorerBaseUrl(fromDate, toDate);
            string dimension = String.Format(rdxExplorerDimensionProperty,
                Uri.EscapeUriString(RDXOpcUaQueries.OpcServerUri));
            string measure = "";
            if (!showCount)
            {
                measure = String.Format(rdxExplorerMeasureProperty,
                    Uri.EscapeUriString(RDXOpcUaQueries.OpcMonitoredItemValue));
            }
            return baseUrl + dimension + measure;
        }

        /// <summary>
        /// Return a Url to open RDX Explorer for a specific station and show data split by NodeId or DisplayName.
        /// </summary>
        /// <param name="fromDate">start date and time for display range</param>
        /// <param name="toDate">end date and time of display range</param>
        /// <param name="appUri">Application Uri of the station</param>
        /// <param name="showByName">true to show server node by DisplayName, otherwise by NodeId</param>
        /// <param name="showCount">true to show Count, otherwise show Value</param>
        /// <returns>The Url</returns>
        public static string GetExplorerStationView(DateTime fromDate, DateTime toDate, string appUri, bool showByName = true, bool showCount = true)
        {
            string baseUrl = GetExplorerBaseUrl(fromDate, toDate);
            string stationQuery = Uri.EscapeUriString(String.Format(RDXOpcUaQueries.OpcServerPredicate, appUri));
            string predicate = String.Format(rdxExplorerPredicate, stationQuery);
            string dimension = String.Format(rdxExplorerDimensionProperty,
                Uri.EscapeUriString(showByName ? RDXOpcUaQueries.OpcDisplayName : RDXOpcUaQueries.OpcMonitoredItemId));
            string measure = "";
            if (!showCount)
            {
                measure = String.Format(rdxExplorerMeasureProperty,
                    Uri.EscapeUriString(RDXOpcUaQueries.OpcMonitoredItemValue));
            }
            return baseUrl + predicate + dimension + measure;
        }

        /// <summary>
        /// Test the explorer url for all stations
        /// </summary>
        /// <param name="topology"></param>
        public static void TestExplorerViews(ContosoTopology topology)
        {
            List<string> stations = topology.GetAllChildren(topology.TopologyRoot.Key, typeof(Station));
            int count = 0;
            DateTime Now = DateTime.UtcNow;
            string url;
            url = GetExplorerDefaultView(Now.Subtract(TimeSpan.FromHours(8)), Now, false);
            RDXTrace.TraceInformation(url);
            url = GetExplorerDefaultView(Now.Subtract(TimeSpan.FromDays(1)), Now, true);
            RDXTrace.TraceInformation(url);
            foreach (string appUri in stations)
            {
                url = GetExplorerStationView(Now.Subtract(TimeSpan.FromHours(1)), Now,
                    appUri, (count & 1)==0, (count & 2)==0);
                RDXTrace.TraceInformation(url);
                count++;
            }
        }

    }
}

