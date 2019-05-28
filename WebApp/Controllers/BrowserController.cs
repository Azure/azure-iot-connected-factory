
using GlobalResources;
using Microsoft.AspNet.SignalR;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Contoso;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Helpers;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Models;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Security;
using Newtonsoft.Json;
using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Controllers
{
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models;
    using System.Text.RegularExpressions;
    using static Startup;
    public class StatusHub : Hub { }

    [OutputCache(CacheProfile = "NoCacheProfile")]
    public class BrowserController : Controller
    {
        private class MethodCallParameterData
        {
            public string ObjectId { get; set; }

            public string Name { get; set; }

            public string Value { get; set; }

            public string ValueRank { get; set; }

            public string ArrayDimensions { get; set; }

            public string Description { get; set; }

            public string Datatype { get; set; }

            public string TypeName { get; set; }
        }

        /// <summary>
        /// Handle any unknown actions
        /// </summary>
        /// <param name="actionName"></param>
        protected override void HandleUnknownAction(string actionName)
        {
            Utils.Trace("Unknown action {0} - redirect to index...", actionName);
            RedirectToAction("Index").ExecuteResult(this.ControllerContext);
        }

        /// <summary>
        /// Create a list of endpoints for all servers
        /// </summary>
        /// <param name="actionName"></param>
        public List<Endpoint> CreateEndpointList()
        {
            List<Endpoint> endpointList = new List<Endpoint>();

            try
            {
                string supervisorId = Session["supervisorId"].ToString();
                IEnumerable<ApplicationInfoApiModel> applications = RegistryService.ListApplications();

                if (applications != null)
                {
                    foreach (var application in applications)
                    {
                        if (application.SupervisorId == supervisorId)
                        {
                            ApplicationRegistrationApiModel applicationRecord = RegistryService.GetApplication(application.ApplicationId);

                            foreach (var elem in applicationRecord.Endpoints)
                            {
                                EndpointInfoApiModel endpointModel = RegistryService.GetEndpoint(elem.Id);

                                Endpoint endpointInfo = new Endpoint();
                                endpointInfo.EndpointId = elem.Id;
                                endpointInfo.EndpointUrl = elem.Endpoint.Url;
                                endpointInfo.SecurityMode = elem.Endpoint.SecurityMode != null ? elem.Endpoint.SecurityMode.ToString() : string.Empty;
                                endpointInfo.SecurityPolicy = elem.Endpoint.SecurityPolicy != null ? elem.Endpoint.SecurityPolicy.Remove(0, elem.Endpoint.SecurityPolicy.IndexOf('#') + 1) : string.Empty;
                                endpointInfo.SecurityLevel = elem.SecurityLevel;
                                endpointInfo.ApplicationId = application.ApplicationId;
                                endpointInfo.ProductUri = application.ProductUri;
                                endpointInfo.Activated = endpointModel.ActivationState == EndpointActivationState.Activated || endpointModel.ActivationState == EndpointActivationState.ActivatedAndConnected;
                                endpointList.Add(endpointInfo);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Trace.TraceWarning("Can not get applications list");
                string errorMessage = string.Format(Strings.BrowserOpcException, e.Message,
                    e.InnerException?.Message ?? "--", e?.StackTrace ?? "--");
                Trace.TraceWarning(errorMessage);
            }
            return endpointList;
        }

        /// <summary>
        /// Default action of the controller.
        /// </summary>
        [HttpGet]
        [RequirePermission(Permission.BrowseOpcServer)]
        public ActionResult Index()
        {
            OpcSessionModel sessionModel = new OpcSessionModel();
            
            if (Session["EndpointId"] != null)
            {
                return View("Browse", sessionModel);
            }
           
            sessionModel.endpointList = CreateEndpointList();
            return View("Index", sessionModel);
        }

        /// <summary>
        /// Action to show a browser related  error message.
        /// </summary>
        [HttpGet]
        [RequirePermission(Permission.BrowseOpcServer)]
        public ActionResult Error(string errorMessage)
        {
            OpcSessionModel sessionModel = new OpcSessionModel
            {
                ErrorHeader = Strings.BrowserOpcErrorHeader,
                EndpointUrl = (string)Session["EndpointUrl"],
                ErrorMessage = HttpUtility.HtmlDecode(errorMessage)
            };

            return Json(sessionModel);
        }

        /// <summary>
        /// Start action of the controller.
        /// </summary>
        [HttpGet]
        [RequirePermission(Permission.BrowseOpcServer)]
        public ActionResult Start(string supervisorId)
        {
            OpcSessionModel sessionModel = new OpcSessionModel();

            Session["supervisorId"] = supervisorId;
            return Index();
        }

        /// <summary>
        /// Back action of the controller.
        /// </summary>
        [HttpGet]
        [RequirePermission(Permission.BrowseOpcServer)]
        public ActionResult Back(string backUrl)
        {
            if ((backUrl != null) && (backUrl.Contains("AddOpcServer")))
            {
                return RedirectToAction("Index", "AddOpcServer");
            }
            else
            {
                Session["EndpointId"] = null;
                return RedirectToAction("Index"); 
            }
        }

        /// <summary>
        /// Browse entry point.
        /// </summary>
        [HttpGet]
        [RequirePermission(Permission.BrowseOpcServer)]
        public ActionResult BrowseNodes(string endpointId, string endpointUrl, string productUri)
        {
            OpcSessionModel sessionModel = new OpcSessionModel { EndpointUrl = endpointUrl, EndpointId = endpointId };
       
            Session["EndpointId"] = endpointId;
            Session["EndpointUrl"] = endpointUrl;
            Session["ProductUri"] = productUri;
            return View("Browse", sessionModel);
        }

        /// <summary>
        /// Post form method to activate an endpoint.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken(Order = 1)]
        [RequirePermission(Permission.ControlOpcServer)]
        public async Task<ActionResult> Activate(string endpointId)
        {
            OpcSessionModel sessionModel = new OpcSessionModel { EndpointId = endpointId };

            try
            {
                await RegistryService.ActivateEndpointAsync(endpointId);
            }
            catch (Exception exception)
            {
                // Generate an error to be shown in the error view and trace    .
                string errorMessageTrace = string.Format(Strings.BrowserConnectException, exception.Message,
                exception.InnerException?.Message ?? "--", exception?.StackTrace ?? "--"); 
                Trace.TraceError(errorMessageTrace);
                if (exception.Message.Contains("ResourceNotFound"))
                {
                    sessionModel.ErrorHeader = Strings.BrowserEndpointErrorHeader;
                }
                else
                {
                    sessionModel.ErrorHeader = Strings.BrowserConnectErrorHeader;
                }
                return Json(sessionModel);
            }

            Session["EndpointId"] = endpointId;
            return Json(sessionModel);
        }

        /// <summary>
        /// Post method to deactivate an endpoint.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken(Order = 1)] 
        [RequirePermission(Permission.ControlOpcServer)]
        public ActionResult Deactivate(string endpointId)
        {
            Session["EndpointUrl"] = null;
            Session["EndpointId"] = null;

            try
            {
                RegistryService.DeActivateEndpoint(endpointId);
            }
            catch (Exception exception)
            {
                // Generate an error to be shown in trace.
                string errorMessageTrace = string.Format(Strings.BrowserConnectException, exception.Message,
                exception.InnerException?.Message ?? "--", exception?.StackTrace ?? "--");
                Trace.TraceError(errorMessageTrace);
            }

            OpcSessionModel sessionModel = new OpcSessionModel(); 
            return Json(sessionModel);
        }

        /// <summary>
        /// Post method to read information of the root node of connected OPC UA server.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken(Order = 1)]
        [RequirePermission(Permission.BrowseOpcServer)]
        public async Task<ActionResult> GetRootNode()
        {
            List<object> jsonTree = new List<object>();
            string endpointId = Session["EndpointId"].ToString();
            BrowseRequestApiModel model = new BrowseRequestApiModel();

            model.NodeId = "";
            model.TargetNodesOnly = true;

            try
            {
                var browseData = await TwinService.NodeBrowseAsync(endpointId, model);
                jsonTree.Add(new { id = browseData.Node.NodeId, text = browseData.Node.DisplayName, children = (browseData.References?.Count != 0) });

                return Json(jsonTree, JsonRequestBehavior.AllowGet);
            }
            catch (Exception exception)
            {
                Session["EndpointId"] = null;
                return Content(CreateOpcExceptionActionString(exception)); 
            } 
        }

        /// <summary>
        /// Post method to read information on the children of the given node in connected OPC UA server.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken(Order = 1)]
        [RequirePermission(Permission.BrowseOpcServer)]
        public async Task<ActionResult> GetChildren(string jstreeNode)
        {
            // This delimiter is used to allow the storing of the OPC UA parent node ID together with the OPC UA child node ID in jstree data structures and provide it as parameter to 
            // Ajax calls.
            string[] delimiter = { "__$__" };
            string[] jstreeNodeSplit = jstreeNode.Split(delimiter, 3, StringSplitOptions.None);
            string node;
            if (jstreeNodeSplit.Length == 1)
            {
                node = jstreeNodeSplit[0];
            }
            else
            {
                node = jstreeNodeSplit[1];
            }

            List<object> jsonTree = new List<object>();
            string endpointId = Session["EndpointId"].ToString();
            string endpointUrl = Session["EndpointUrl"].ToString();
            string ProductUri = Session["ProductUri"].ToString();

            // read the currently published nodes
            PublishedItemListResponseApiModel publishedNodes = new PublishedItemListResponseApiModel();
            try
            {
                PublishedItemListRequestApiModel publishModel = new PublishedItemListRequestApiModel();
                publishedNodes = await TwinService.GetPublishedNodesAsync(endpointId, publishModel);

            }
            catch (Exception e)
            {
                // do nothing, since we still want to show the tree
                Trace.TraceWarning("Can not read published nodes for endpoint '{0}'.", endpointUrl);
                string errorMessage = string.Format(Strings.BrowserOpcException, e.Message,
                    e.InnerException?.Message ?? "--", e?.StackTrace ?? "--");
                Trace.TraceWarning(errorMessage);
            }

            BrowseResponseApiModel browseData = new BrowseResponseApiModel();

            try
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                try
                {
                    BrowseRequestApiModel model = new BrowseRequestApiModel();
                    model.NodeId = node;
                    model.TargetNodesOnly = false;

                    browseData =  TwinService.NodeBrowse(endpointId, model);
                }
                catch (Exception e)
                {
                    // skip this node
                    Trace.TraceError("Can not browse node '{0}'", node);
                    string errorMessage = string.Format(Strings.BrowserOpcException, e.Message,
                        e.InnerException?.Message ?? "--", e?.StackTrace ?? "--");
                    Trace.TraceError(errorMessage);
                }

                Trace.TraceInformation("Browsing node '{0}' data took {0} ms", node.ToString(), stopwatch.ElapsedMilliseconds);

                if (browseData.References != null)
                {
                    var idList = new List<string>();
                    foreach (var nodeReference in browseData.References)
                    {
                        bool idFound = false;
                        foreach (var id in idList)
                        {
                            if (id == nodeReference.Target.NodeId.ToString())
                            {
                                idFound = true;
                            }
                        }
                        if (idFound == true)
                        {
                            continue;
                        }

                        Trace.TraceInformation("Browse '{0}' count: {1}", nodeReference.Target.NodeId, jsonTree.Count);

                        NodeApiModel currentNode = nodeReference.Target;

                        currentNode = nodeReference.Target;

                        byte currentNodeAccessLevel = 0;
                        byte currentNodeEventNotifier = 0;
                        bool currentNodeExecutable = false;

                        switch (currentNode.NodeClass)
                        {
                            case NodeClass.Variable:
                                currentNodeAccessLevel = currentNode.UserAccessLevel != null ? (byte)currentNode.UserAccessLevel : (byte)0;
                                if (!PermsChecker.HasPermission(Permission.ControlOpcServer))
                                {
                                    currentNodeAccessLevel = (byte)((uint)currentNodeAccessLevel & ~0x2);
                                }
                                break;

                            case NodeClass.Object:
                                currentNodeEventNotifier = currentNode.EventNotifier != null ? (byte)currentNode.EventNotifier : (byte)0;
                                break;

                            case NodeClass.View:
                                currentNodeEventNotifier = currentNode.EventNotifier != null ? (byte)currentNode.EventNotifier : (byte)0;
                                break;

                            case NodeClass.Method:
                                if (PermsChecker.HasPermission(Permission.ControlOpcServer))
                                {
                                    currentNodeExecutable = currentNode.UserExecutable != null ? (bool)currentNode.UserExecutable : false;
                                }
                                break;

                            default:
                                break;
                        }

                        var isPublished = false;
                        var isRelevant = false;
                        if (publishedNodes.Items != null)
                        {
                            foreach (var item in publishedNodes.Items)
                            {
                                if (item.NodeId == nodeReference.Target.NodeId.ToString())
                                {
                                    isPublished = true;
                                    ContosoOpcUaNode contosoOpcUaNode = Startup.Topology.GetOpcUaNode(ProductUri, item.NodeId);
                                    if (contosoOpcUaNode?.Relevance != null)
                                    {
                                        isRelevant = true;
                                    }
                                }
                            }
                        }

                        jsonTree.Add(new
                        {
                            id = ("__" + node + delimiter[0] + nodeReference.Target.NodeId.ToString()),
                            text = nodeReference.Target.DisplayName.ToString(),
                            nodeClass = nodeReference.Target.NodeClass.ToString(),
                            accessLevel = currentNodeAccessLevel.ToString(),
                            eventNotifier = currentNodeEventNotifier.ToString(),
                            executable = currentNodeExecutable.ToString(),
                            children = nodeReference.Target.HasChildren,
                            publishedNode = isPublished,
                            relevantNode = isRelevant
                        });
                        idList.Add(nodeReference.Target.NodeId.ToString());
                    }
                }

                stopwatch.Stop();
                Trace.TraceInformation("Browing all childeren info of node '{0}' took {0} ms", node, stopwatch.ElapsedMilliseconds);

                return Json(jsonTree, JsonRequestBehavior.AllowGet);
            }
            catch (Exception exception)
            {
                return Content(CreateOpcExceptionActionString(exception));
            }
         
        }

        /// <summary>
        /// Post method to read the value of a variable identified by the given node.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken(Order = 1)]
        [RequirePermission(Permission.BrowseOpcServer)]
        public async Task<ActionResult> VariableRead(string jstreeNode)
        {
            string[] delimiter = { "__$__" };
            string[] jstreeNodeSplit = jstreeNode.Split(delimiter, 3, StringSplitOptions.None);
            string node;
            string actionResult = string.Empty;
            string endpointId = Session["EndpointId"].ToString();

            if (jstreeNodeSplit.Length == 1)
            {
                node = jstreeNodeSplit[0];
            }
            else
            {
                node = jstreeNodeSplit[1];
            }

            try
            {
                ValueReadRequestApiModel model = new ValueReadRequestApiModel();
                model.NodeId = node;
                var data = await TwinService.ReadNodeValueAsync(endpointId, model);

                string value = "";

                if (data.Value != null)
                {
                    value = data.Value.ToString();
                }
                // We return the HTML formatted content, which is shown in the context panel.
                actionResult = Strings.BrowserOpcDataValueLabel + ": " + value + @"<br/>" +
                                (data.ErrorInfo == null ? "" : (Strings.BrowserOpcDataStatusLabel + ": " + data.ErrorInfo.StatusCode + @"<br/>")) +
                                Strings.BrowserOpcDataSourceTimestampLabel + ": " + data.SourceTimestamp + @"<br/>" +
                                Strings.BrowserOpcDataServerTimestampLabel + ": " + data.ServerTimestamp;
                return Content(actionResult);
            }
            catch (Exception exception)
            {               
                return Content(CreateOpcExceptionActionString(exception));
            }
        }

       
        /// <summary>
        /// Post method to publish or unpublish the value of a variable identified by the given node.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken(Order = 1)]
        [RequirePermission(Permission.PublishOpcNode)]
        public async Task<ActionResult> VariablePublishUnpublish(string jstreeNode, string method)
        {
            string[] delimiter = { "__$__" };
            string[] jstreeNodeSplit = jstreeNode.Split(delimiter, 3, StringSplitOptions.None);
            string node;
            string actionResult = "";
            string endpointId = Session["EndpointId"].ToString();
           
            if (jstreeNodeSplit.Length == 1)
            {
                node = jstreeNodeSplit[0];
            }
            else
            {
                node = jstreeNodeSplit[1];
            }
           
            try
            {
                if (method == "unpublish")
                {
                    PublishStopRequestApiModel publishModel = new PublishStopRequestApiModel();
                    publishModel.NodeId = node;
                    var data = await TwinService.UnPublishNodeValuesAsync(endpointId, publishModel);

                    if (data.ErrorInfo != null)
                    {
                        actionResult = Strings.BrowserOpcUnPublishFailed + @"<br/><br/>" +
                                        Strings.BrowserOpcMethodStatusCode + ": " + data.ErrorInfo.StatusCode;
                        if (data.ErrorInfo.Diagnostics != null)
                        {
                            actionResult += @"<br/><br/>" + Strings.BrowserOpcDataDiagnosticInfoLabel + ": " + data.ErrorInfo.Diagnostics;
                        }
                    }
                    else
                    {
                        actionResult = Strings.BrowserOpcUnPublishSucceeded;
                    }

                }
                else
                {
                    PublishStartRequestApiModel publishModel = new PublishStartRequestApiModel();
                    PublishedItemApiModel item = new PublishedItemApiModel();
                    item.NodeId = node;
                    publishModel.Item = item;
                    var data = await TwinService.PublishNodeValuesAsync(endpointId, publishModel);

                    if (data.ErrorInfo != null)
                    {
                        actionResult = Strings.BrowserOpcPublishFailed + @"<br/><br/>" +
                                        Strings.BrowserOpcMethodStatusCode + ": " + data.ErrorInfo.StatusCode;
                        if (data.ErrorInfo.Diagnostics != null)
                        {
                            actionResult += @"<br/><br/>" + Strings.BrowserOpcDataDiagnosticInfoLabel + ": " + data.ErrorInfo.Diagnostics;
                        }
                    }
                    else
                    {
                        actionResult = Strings.BrowserOpcPublishSucceeded;
                    }
                }
                return Content(actionResult);
            }
            catch (Exception exception)
            {
                return Content(CreateOpcExceptionActionString(exception));
            }        
        }

        /// <summary>
        /// Post method to fetch the value of a variable identified by the given node, which should be updated.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken(Order = 1)]
        [RequirePermission(Permission.ControlOpcServer)]
        public async Task<ActionResult> VariableWriteFetch(string jstreeNode)
        {
            string[] delimiter = { "__$__" };
            string[] jstreeNodeSplit = jstreeNode.Split(delimiter, 3, StringSplitOptions.None);
            string node;
            string actionResult = string.Empty;
            string endpointId = Session["EndpointId"].ToString();

            if (jstreeNodeSplit.Length == 1)
            {
                node = jstreeNodeSplit[0];
            }
            else
            {
                node = jstreeNodeSplit[1];
            }

            try
            {
                ValueReadRequestApiModel model = new ValueReadRequestApiModel();
                model.NodeId = node;
                var data = await TwinService.ReadNodeValueAsync(endpointId, model);

                if (data.Value != null)
                {
                    if (data.Value.ToString().Length > 30)
                    {
                        actionResult = data.Value.ToString().Substring(0, 30);
                        actionResult += "...";
                    }
                    else
                    {
                        actionResult = data.Value.ToString();
                    }
                }
                return Content(actionResult);
            }
            catch (Exception exception)
            {   
                return Content(CreateOpcExceptionActionString(exception));
            }
        }

        /// <summary>
        /// Post method to update the values of a variable identified by the given node with the given value.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken(Order = 1)]
        [RequirePermission(Permission.ControlOpcServer)]
        public async Task<ActionResult> VariableWriteUpdate(string jstreeNode, string newValue)
        {
            string[] delimiter = { "__$__" };
            string[] jstreeNodeSplit = jstreeNode.Split(delimiter, 3, StringSplitOptions.None);
            string node;
            string endpointId = Session["EndpointId"].ToString();

            if (jstreeNodeSplit.Length == 1)
            {
                node = jstreeNodeSplit[0];
            }
            else
            {
                node = jstreeNodeSplit[1];
            }

            try
            {
                ValueWriteRequestApiModel model = new ValueWriteRequestApiModel();
                model.NodeId = node;
                model.Value = newValue;
                var data = await TwinService.WriteNodeValueAsync(endpointId, model);
                if (data.ErrorInfo == null)
                {
                    return Content(Strings.WriteSuccesfully);
                }
                else
                {
                    return Content(data.ErrorInfo.ErrorMessage);
                }
            }
            catch (Exception exception)
            {
                return Content(CreateOpcExceptionActionString(exception));
            }
        }

        /// <summary>
        /// Post method to get the parameters of a method call indentified by the given node.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken(Order = 1)]
        [RequirePermission(Permission.ControlOpcServer)]
        public async Task<ActionResult> MethodCallGetParameter(string jstreeNode)
        {
            string[] delimiter = { "__$__" };
            string[] jstreeNodeSplit = jstreeNode.Split(delimiter, 3, StringSplitOptions.None);
            string node;
            List<object> jsonParameter = new List<object>();
            int parameterCount = 0;
            string endpointId = Session["EndpointId"].ToString();

            if (jstreeNodeSplit.Length == 1)
            {
                node = jstreeNodeSplit[0];
            }
            else
            {
                node = jstreeNodeSplit[1];
            }
           
            try
            {
                MethodMetadataRequestApiModel model = new MethodMetadataRequestApiModel();
                model.MethodId = node;
                var data = await TwinService.NodeMethodGetMetadataAsync(endpointId, model);

                if (data.InputArguments == null)
                {
                    parameterCount = 0;
                    jsonParameter.Add(new { data.ObjectId });
                }
                else
                {
                    if (data.InputArguments.Count > 0)
                    {
                        foreach(var item in data.InputArguments)
                        {
                            jsonParameter.Add(new
                            {
                                objectId = data.ObjectId,
                                name = item.Name,
                                value = item.DefaultValue,
                                valuerank = item.ValueRank,
                                arraydimentions = item.ArrayDimensions,
                                description = item.Description,
                                datatype = item.Type.DataType,
                                typename = item.Type.DisplayName
                            });
                        }
                    }
                    else
                    {
                        jsonParameter.Add(new { data.ObjectId });
                    }

                    parameterCount = data.InputArguments.Count;
                }
                
                return Json(new { count = parameterCount, parameter = jsonParameter }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception exception)
            {        
                return Content(CreateOpcExceptionActionString(exception));
            }
        }

        /// <summary>
        /// Post method to call an OPC UA method in the server.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken(Order = 1)]
        [RequirePermission(Permission.ControlOpcServer)]
        public async Task<ActionResult> MethodCall(string jstreeNode, string parameterData, string parameterValues, Session session = null)
        {
            string[] delimiter = { "__$__" };
            string[] jstreeNodeSplit = jstreeNode.Split(delimiter, 3, StringSplitOptions.None);
            string node;
            string parentNode = null;
            string endpointId = Session["EndpointId"].ToString();

            if (jstreeNodeSplit.Length == 1)
            {
                node = jstreeNodeSplit[0];
                parentNode = null;
            }
            else
            {
                node = jstreeNodeSplit[1];
                parentNode = (jstreeNodeSplit[0].Replace(delimiter[0], "")).Replace("__", "");
            }

            string actionResult = "";
            List<MethodCallParameterData> originalData = JsonConvert.DeserializeObject<List<MethodCallParameterData>>(parameterData);
            List<Variant> values = JsonConvert.DeserializeObject<List<Variant>>(parameterValues);
            List<MethodCallArgumentApiModel> argumentsList = new List<MethodCallArgumentApiModel>();

            try
            {
                MethodCallRequestApiModel model = new MethodCallRequestApiModel();
                model.MethodId = node;
                model.ObjectId = originalData[0].ObjectId;

                if (originalData.Count > 1)
                {
                    int count = 0;
                    foreach (var item in originalData)
                    {
                        MethodCallArgumentApiModel argument = new MethodCallArgumentApiModel();
                        argument.Value = values[count].Value != null ? values[count].Value.ToString() : string.Empty;
                        argument.DataType = item.Datatype;
                        argumentsList.Add(argument);
                        count++;
                    }
                    model.Arguments = argumentsList;
                }

                var data = await TwinService.NodeMethodCallAsync(endpointId, model);
    
                if (data.ErrorInfo != null)
                {
                    actionResult = Strings.BrowserOpcMethodCallFailed + @"<br/><br/>" +
                                    Strings.BrowserOpcMethodStatusCode + ": " + data.ErrorInfo.StatusCode;
                    if (data.ErrorInfo.Diagnostics != null)
                    {
                        actionResult += @"<br/><br/>" + Strings.BrowserOpcDataDiagnosticInfoLabel + ": " + data.ErrorInfo.Diagnostics;
                    }
                }
                else
                {
                    if (data.Results.Count == 0)
                    {
                        actionResult = Strings.BrowserOpcMethodCallSucceeded;
                    }
                    else
                    {
                        actionResult = Strings.BrowserOpcMethodCallSucceededWithResults + @"<br/><br/>";
                        for (int ii = 0; ii < data.Results.Count; ii++)
                        {
                            actionResult += data.Results[ii].Value + @"<br/>";
                        }
                    }
                }
                return Content(actionResult);
            }
            catch (Exception exception)
            {
                return Content(CreateOpcExceptionActionString(exception));
            }
        }

        /// <summary>
        /// Writes an error message to the trace and generates an HTML encoded string to be sent to the client in case of an error.
        /// </summary>
        private string CreateOpcExceptionActionString(Exception exception)
        {
            // Generate an error response, to be shown in the error view.
            string errorMessage = string.Format(Strings.BrowserOpcException, exception.Message,
                exception.InnerException?.Message ?? "--", exception?.StackTrace ?? "--");
            Trace.TraceError(errorMessage);
            string stringError = getSubstring(exception.Message, "message", ".");
            if (stringError != string.Empty)
            {
                errorMessage = Regex.Replace(stringError, "[^A-Za-z0-9 ]", "");
            }
            else
            {
                errorMessage = exception.Message;
            }          
            string actionResult = HttpUtility.HtmlEncode(errorMessage);
            Response.StatusCode = 1;

            return actionResult;
        }

        private string getSubstring(string source, string start, string end)
        {
            int startIndex;
            int endIndex;
            string result = string.Empty;
            if (source.Contains(start) && source.Contains(end))
            {
                startIndex = source.IndexOf(start, 0) + start.Length;
                endIndex = source.IndexOf(end, startIndex);
                result = source.Substring(startIndex, endIndex - startIndex);
            }
            return result;
        }
    }
}
