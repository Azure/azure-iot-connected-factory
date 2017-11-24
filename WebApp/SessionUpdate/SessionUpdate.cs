
using Microsoft.AspNet.SignalR;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Contoso;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp
{
    public class SessionUpdate
    {
        public static void ConfigureUpdateSessions(CancellationToken token)
        {
            try
            {
                _updateClientOeeKpiData = new AutoResetEvent(false);
                Task.Run(async () => await UpdateSessionsOeeKpiData(token));
                _updateClientAlertData = new AutoResetEvent(false);
                Task.Run(async () => await UpdateSessionsAlertData(token));
                _updateClientChildrenData = new AutoResetEvent(false);
                Task.Run(async () => await UpdateSessionsChildrenData(token));
            }
            catch (Exception e)
            {
                Trace.TraceError($"Exception in ConfigureUpdateSessions ({e.Message})");
            }
        }

        public static void Dispose()
        {
            if (_updateClientOeeKpiData != null)
            {
                _updateClientOeeKpiData.Dispose();
            }
            if (_updateClientAlertData != null)
            {
                _updateClientAlertData.Dispose();
            }
            if (_updateClientChildrenData != null)
            {
                _updateClientChildrenData.Dispose();
            }
        }

        public static void TriggerSessionOeeKpiDataUpdate()
        {
            _updateClientOeeKpiData?.Set();
        }

        public static void TriggerSessionAlertDataUpdate()
        {
            _updateClientAlertData?.Set();
        }

        public static void TriggerSessionChildrenDataUpdate()
        {
            _updateClientChildrenData?.Set();
        }

        /// <summary>
        /// Update the browser OEE/KPI data using SignalR
        /// </summary>
        private static async Task UpdateSessionsOeeKpiData(CancellationToken ct)
        {
            while (true)
            {
                _updateClientOeeKpiData.WaitOne(Timeout.Infinite);

                if (ct.IsCancellationRequested)
                {
                    break;
                }

                OeeKpiDataUpdate[] sessionOeeKpiDataUpdate = new OeeKpiDataUpdate[Startup.SessionList.Count];
                int sessionUpdateIndex = 0;

                try
                {
                    foreach (KeyValuePair<string, DashboardModel> session in Startup.SessionList)
                    {
                        OeeKpiDataUpdate sessionOeeKpiData = new OeeKpiDataUpdate();
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
                        sessionOeeKpiDataUpdate[sessionUpdateIndex] = sessionOeeKpiData;
                        sessionUpdateIndex++;
                    }

                    if (Startup.SessionList.Count > 0 && sessionUpdateIndex > 0)
                    {
                        string _sessionOeeKpiDataUpdateJson = JsonConvert.SerializeObject(sessionOeeKpiDataUpdate);
                        IHubContext hubContext = GlobalHost.ConnectionManager.GetHubContext<TelemetryHub>();
                        hubContext.Clients.All.updateSessionOeeKpiData(_sessionOeeKpiDataUpdateJson);
                    }
                }
                catch (Exception e)
                {
                    Trace.TraceInformation($"Exception in UpdateSessionsOeeKpiData ({e.Message})");
                }
            }
        }

        /// <summary>
        /// Update the browser alert list data using SignalR
        /// </summary>
        private static async Task UpdateSessionsAlertData(CancellationToken ct)
        {
            while (true)
            {
                _updateClientAlertData.WaitOne(Timeout.Infinite);

                if (ct.IsCancellationRequested)
                {
                    break;
                }

                AlertDataUpdate[] sessionAlertDataUpdate = new AlertDataUpdate[Startup.SessionList.Count];
                int sessionUpdateIndex = 0;

                try
                {
                    foreach (KeyValuePair<string, DashboardModel> session in Startup.SessionList)
                    {
                        AlertDataUpdate sessionAlertData = new AlertDataUpdate();
                        string topNodeKey = session.Value.TopNode.Key;

                        // Update the alert data.
                        sessionAlertData.SessionId = session.Key;
                        sessionAlertData.TopNode = topNodeKey;
                        sessionAlertData.Alerts = Startup.Topology.GetAlerts(topNodeKey);

                        // Update the data sent to the clients.
                        sessionAlertDataUpdate[sessionUpdateIndex] = sessionAlertData;
                        sessionUpdateIndex++;
                    }

                    if (Startup.SessionList.Count > 0 && sessionUpdateIndex > 0)
                    {
                        string _sessionAlertDataUpdateJson = JsonConvert.SerializeObject(sessionAlertDataUpdate);
                        IHubContext hubContext = GlobalHost.ConnectionManager.GetHubContext<TelemetryHub>();
                        hubContext.Clients.All.updateSessionAlertData(_sessionAlertDataUpdateJson);
                    }
                }
                catch (Exception e)
                {
                    Trace.TraceInformation($"Exception in UpdateSessionsAlertData ({e.Message})");
                }
            }
        }

        /// <summary>
        /// Update the browser children list data content using SignalR
        /// </summary>
        private static async Task UpdateSessionsChildrenData(CancellationToken ct)
        {
            while (true)
            {
                _updateClientChildrenData.WaitOne(Timeout.Infinite);

                if (ct.IsCancellationRequested)
                {
                    break;
                }

                ChildrenDataUpdate[] sessionChildrenDataUpdate = new ChildrenDataUpdate[Startup.SessionList.Count];
                int sessionUpdateIndex = 0;

                try
                {
                    foreach (KeyValuePair<string, DashboardModel> session in Startup.SessionList)
                    {
                        ChildrenDataUpdate sessionChildrenData = new ChildrenDataUpdate();
                        string topNodeKey = session.Value.TopNode.Key;

                        // Update the children data.
                        sessionChildrenData.SessionId = session.Key;
                        sessionChildrenData.TopNode = topNodeKey;
                        sessionChildrenData.Children = Startup.Topology.GetChildrenInfo(topNodeKey);

                        // Update the data sent to the clients.
                        sessionChildrenDataUpdate[sessionUpdateIndex] = sessionChildrenData;
                        sessionUpdateIndex++;
                    }

                    if (Startup.SessionList.Count > 0 && sessionUpdateIndex > 0)
                    {
                        string _sessionChildrenDataUpdateJson = JsonConvert.SerializeObject(sessionChildrenDataUpdate);
                        IHubContext hubContext = GlobalHost.ConnectionManager.GetHubContext<TelemetryHub>();
                        hubContext.Clients.All.updateSessionChildrenData(_sessionChildrenDataUpdateJson);
                    }
                }
                catch (Exception e)
                {
                    Trace.TraceInformation($"Exception in UpdateSessionsChildrenData ({e.Message})");
                }
            }
        }

        private static AutoResetEvent _updateClientOeeKpiData = null;
        private static AutoResetEvent _updateClientAlertData = null;
        private static AutoResetEvent _updateClientChildrenData = null;
    }
}

