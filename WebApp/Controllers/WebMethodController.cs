
using GlobalResources;
using Microsoft.AspNet.SignalR;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Contoso;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Helpers;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Models;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.RDX;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Security;
using Newtonsoft.Json;
using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.Web.Mvc;
using static Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.RDX.RDXUtils;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Controllers
{
    public class WebMethodController : Controller
    {
        /// <summary>
        /// Web API to get the data for the specified topology node, view and performance.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken(Order = 1)]
        [RequirePermission(Permission.ViewTelemetry)]
        public string GetOeeKpiData(string key, ContosoTopologyNode.AggregationView view, ContosoPerformanceRelevance relevance)
        {
            AggregatedTimeSeriesResult timeSeries;
            ContosoTopologyNode node = (ContosoTopologyNode)Startup.Topology[key];

            if (node == null)
            {
                return JsonConvert.SerializeObject("Error");
            }
            else
            {
                timeSeries = node.AggregatedOeeKpiTimeSeries(view, relevance, TimeSpan.FromHours(1));
            }
            return JsonConvert.SerializeObject(timeSeries);
        }

        /// <summary>
        /// Web API to get the data for the specified OPC UA node and view.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken(Order = 1)]
        [RequirePermission(Permission.ViewTelemetry)]
        public string GetDataForOpcUaNode(string key, string nodeId, ContosoTopologyNode.AggregationView view)
        {
            AggregatedTimeSeriesResult timeSeries;
            ContosoOpcUaNode contosoOpcUaNode = Startup.Topology.GetOpcUaNode(key, nodeId);
            Station station = Startup.Topology.GetStation(key);
            string[] data = new string[3];

            if ((contosoOpcUaNode == null) || (station == null))
            {
                data[0] = "Error";
                return JsonConvert.SerializeObject(data);
            }

            if (!(RDXUtils.IsAggregatedOperator(contosoOpcUaNode.OpCode)))
            {
                data[0] = "NoTimeSeries";
                // Non aggregating opcodes are not updated unless they have a relevance
                data[1] = contosoOpcUaNode.Last.Value.ToString("0.###", CultureInfo.InvariantCulture);

                if (contosoOpcUaNode.Units != null)
                {
                    data[2] = contosoOpcUaNode.Units.ToString();
                }
                else
                {
                    data[2] = "";
                }
                return JsonConvert.SerializeObject(data);
            }

            Task<AggregatedTimeSeriesResult> task = Task.Run(() => RDXUtils.AggregatedNodeId(station, contosoOpcUaNode, view));

            // Will block until the task is completed...
            timeSeries = task.Result;

            return JsonConvert.SerializeObject(timeSeries);
        }

        /// <summary>
        /// Web API to retrieve an URL to Azure Time Series Insights for the given topology node and view.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken(Order = 1)]
        [RequirePermission(Permission.ViewTelemetry)]
        public string RDXLink(string key, ContosoTopologyNode.AggregationView view)
        {
            string url = null;
            DateTime now = DateTime.UtcNow;
            DateTime past;

            switch (view)
            {
                case ContosoTopologyNode.AggregationView.Hour:
                    past = now.Subtract(TimeSpan.FromHours(1));
                    break;
                case ContosoTopologyNode.AggregationView.Day:
                    past = now.Subtract(TimeSpan.FromDays(1));
                    break;
                case ContosoTopologyNode.AggregationView.Week:
                    past = now.Subtract(TimeSpan.FromDays(7));
                    break;
                default:
                    past = now.Subtract(TimeSpan.FromDays(1));
                    break;
            }

            if (key != null && Startup.Topology.GetStation(key) != null)
            {
                url = RDXExplorer.GetExplorerStationView(past, now, key, true, false);
            }
            else
            {
                url = RDXExplorer.GetExplorerDefaultView(past, now, false);
            }
            
            return url;
        }

        /// <summary>
        /// Web API to execute an action the user has chosen as response to an alert.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken(Order = 1)]
        [RequirePermission(Permission.ActionAlerts)]
        public string AlertActionExecute(long alertId, int alertActionId)
        {
            var jsonResponse = new List<object>();

            try
            {
                // Lookup the alert.
                ContosoAlert alert = Startup.Topology.FindAlert(alertId);
                if (alert != null)
                {
                    // Validate the action id.
                    ContosoTopologyNode topologyNode = Startup.Topology[alert.Key] as ContosoTopologyNode;
                    List<ContosoAlertActionDefinition> alertActions = topologyNode.GetAlertActions(alert.Cause, alert.SubKey);

                    if (alertActions == null || alertActionId >= alertActions.Count)
                    {
                        Trace.TraceError($"alertActionId '{alertActionId}' is out of scope or unknown.");
                        Response.StatusCode = 1;
                        jsonResponse.Add(new {errorMessage = Strings.AlertIdUnknown});
                    }
                    else
                    {
                        bool updateSessionAlerts = false;

                        // Process the requested action.
                        switch (alertActions[alertActionId].Type)
                        {
                            case ContosoAlertActionType.AcknowledgeAlert:
                                // Update alert status.
                                if (topologyNode.AcknowledgeAlert(alertId) == false)
                                {
                                    Trace.TraceError(
                                        $"alertId '{alertId}' in node '{topologyNode.Key}' could not be acknowledged.");
                                    Response.StatusCode = 1;
                                    jsonResponse.Add(new { errorMessage = Strings.AlertIdUnknown });
                                }
                                updateSessionAlerts = true;
                                jsonResponse.Add(new { actionType = ContosoAlertActionType.AcknowledgeAlert });
                                break;
                            case ContosoAlertActionType.CloseAlert:
                                // Update alert status.
                                if (topologyNode.CloseAlert(alertId) == false)
                                {
                                    Trace.TraceError(
                                        $"alertId '{alertId}' in node '{topologyNode.Key}' could not be acknowledged.");
                                    Response.StatusCode = 1;
                                    jsonResponse.Add(new { errorMessage = Strings.AlertIdUnknown });
                                }
                                updateSessionAlerts = true;
                                jsonResponse.Add(new { actionType = ContosoAlertActionType.CloseAlert });
                                break;
                            case ContosoAlertActionType.CallOpcMethod:
                                // Validate that this is a OPC UA server.
                                if (topologyNode.GetType() != typeof (Station))
                                {
                                    Trace.TraceError($"Toplogy node '{topologyNode.Key}' is not an OPC UA server. No method call possible.");
                                    Response.StatusCode = 1;
                                    jsonResponse.Add(new {errorMessage = Strings.AlertIdUnknown});
                                    break;
                                }
                                // Parameter format: "<parent nodeId>, <method nodeId>, <opcua server uri>"
                                string[] parameter = alertActions[alertActionId].Parameter.Split(',');
                                jsonResponse.Add(new { errorMessage = CallOpcMethod(parameter[2].Trim(), parameter[0].Trim(), parameter[1].Trim()) });
                                break;
                            case ContosoAlertActionType.OpenWebPage:
                                jsonResponse.Add(new { actionType = ContosoAlertActionType.OpenWebPage });
                                string urlPath = alertActions[alertActionId].Parameter;
                                jsonResponse.Add(new { url = urlPath });
                                break;
                            case ContosoAlertActionType.None:
                            default:
                                Trace.TraceWarning($"alert type '{alertActions[alertActionId].Type}' of alert '{alertId}' resulted in no action.");
                                break;
                        }

                        // Update session alerts if requested.
                        if (updateSessionAlerts)
                        {
                            // Prepare the updated alert list.
                            AlertDataUpdate[] sessionAlertDataUpdate = new AlertDataUpdate[Startup.SessionList.Count];
                            int sessionUpdateIndex = 0;
                            foreach (KeyValuePair<string, DashboardModel> session in Startup.SessionList)
                            {
                                AlertDataUpdate sessionAlertData = new AlertDataUpdate();
                                string topNodeKey = session.Value.TopNode.Key;

                                // Update the alert data.
                                sessionAlertData.SessionId = Session.SessionID;
                                sessionAlertData.TopNode = topNodeKey;
                                sessionAlertData.Alerts = Startup.Topology.GetAlerts(topNodeKey);

                                // Update the data sent to the clients.
                                sessionAlertDataUpdate[sessionUpdateIndex] = sessionAlertData;
                                sessionUpdateIndex++;
                            }

                            // Update all clients
                            string sessionAlertDataUpdateJson = JsonConvert.SerializeObject(sessionAlertDataUpdate);
                            if (Startup.SessionList.Count > 0 && sessionUpdateIndex > 0)
                            {
                                IHubContext hubContext = GlobalHost.ConnectionManager.GetHubContext<TelemetryHub>();
                                hubContext.Clients.All.updateSessionAlertData(sessionAlertDataUpdateJson);
                            }
                        }
                    }
                }
                else
                {
                    Trace.TraceError($"alertId '{alertId}' is out of scope or unknown.");
                    Response.StatusCode = 1;
                    jsonResponse.Add(new { errorMessage = Strings.AlertIdUnknown });
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError($"exception '{ex.Message}' while processing alertId '{alertId}' and alertActionId '{alertActionId}");
                Response.StatusCode = 1;
                jsonResponse.Add(new { errorMessage = Strings.AlertIdUnknown });
            }

            return JsonConvert.SerializeObject(jsonResponse);
        }

        /// <summary>
        /// Calls a method in the specified OPC UA application.
        /// </summary>
        [RequirePermission(Permission.ControlOpcServer)]
        private string CallOpcMethod(string appUri, string parentNode, string methodNode)
        {
            string result = null;
            VariantCollection inputArguments = new VariantCollection();

            try
            {
                string sessionID = Guid.NewGuid().ToString();
                Session session = OpcSessionHelper.Instance.GetSessionWithImplicitTrust(sessionID, appUri).Result;
                if (session == null)
                {
                    result = $"Could not establish session to OPC UA server with appUri '{appUri}'";
                    Trace.TraceError(result);
                    return result;
                }
                
                CallMethodRequestCollection requests = new CallMethodRequestCollection();
                CallMethodResultCollection results;
                DiagnosticInfoCollection diagnosticInfos = null;
                CallMethodRequest request = new CallMethodRequest();
                request.ObjectId = new NodeId(parentNode);
                request.MethodId = new NodeId(methodNode);
                request.InputArguments = inputArguments;
                requests.Add(request);
                ResponseHeader responseHeader = session.Call(null, requests, out results, out diagnosticInfos);
                OpcSessionHelper.Instance.Disconnect(sessionID);
                if (StatusCode.IsBad(results[0].StatusCode))
                {
                    result = Strings.BrowserOpcMethodCallFailed + "`n" +
                             Strings.BrowserOpcMethodStatusCode + ": " + results[0].StatusCode + "`n";
                    if (diagnosticInfos.Count > 0)
                    {
                        result += "`n" + Strings.BrowserOpcDataDiagnosticInfoLabel + ": " + diagnosticInfos;
                    }
                    Trace.TraceInformation($"OPC UA method call to OPC UA server '{appUri}' method '{methodNode}' under parent node '{parentNode}' failed.");
                    Trace.TraceInformation($"Details: {result}'");
                }
                else
                {
                    if (results[0].OutputArguments.Count == 0)
                    {
                        result = Strings.BrowserOpcMethodCallSucceeded;
                    }
                    else
                    {
                        result = Strings.BrowserOpcMethodCallSucceededWithResults + "`n";
                        for (int ii = 0; ii < results[0].OutputArguments.Count; ii++)
                        {
                            result += results[0].OutputArguments[ii] + "`n";
                        }
                    }
                    Trace.TraceInformation($"OPC UA method call to OPC UA server '{appUri}' method '{methodNode}' under parent node '{parentNode}' succeeded.");
                    Trace.TraceInformation($"Details: {result}'");
                }
            }
            catch (Exception exception)
            {
                result = $"Exception while calling OPC UA method: {exception.Message}";
            }
            return result;
        }

    }
}