
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
    public class StatusHub : Hub { }

    [OutputCache(CacheProfile = "NoCacheProfile")]
    public class BrowserController : Controller
    {
        private List<ListItem> _prepopulatedEndpoints = new List<ListItem>();

        public BrowserController()
        {
            if (OpcSessionHelper.Instance.InitResult == "Good")
            {
                // populate endpoints
                foreach (ConfiguredEndpoint endpoint in OpcSessionHelper.Instance.OpcCachedEndpoints)
                {
                    ListItem item = new ListItem();
                    item.Value = endpoint.EndpointUrl.AbsoluteUri;
                    item.Text = item.Value;
                    _prepopulatedEndpoints.Add(item);
                }
            }
        }

        private class MethodCallParameterData
        {
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
        /// Default action of the controller.
        /// </summary>
        [HttpGet]
        [RequirePermission(Permission.BrowseOpcServer)]
        public ActionResult Index()
        {
            OpcSessionModel sessionModel = new OpcSessionModel();

            if (OpcSessionHelper.Instance.InitResult != "Good")
            {
                sessionModel.ErrorMessage = OpcSessionHelper.Instance.InitResult;
                return View("Error", sessionModel);
            }
            
            OpcSessionCacheData entry = null;
            if (OpcSessionHelper.Instance.OpcSessionCache.TryGetValue(Session.SessionID, out entry))
            {
                sessionModel.EndpointUrl = entry.EndpointURL;
                Session["EndpointUrl"] = entry.EndpointURL;
                return View("Browse", sessionModel);
            }

            sessionModel.PrepopulatedEndpoints = new SelectList(_prepopulatedEndpoints, "Value", "Text");
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
        /// Post form method to connect to an OPC UA server.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken(Order = 1)]
        [RequirePermission(Permission.BrowseOpcServer)]
        public async Task<ActionResult> Connect(string endpointUrl)
        {
            OpcSessionModel sessionModel = new OpcSessionModel { EndpointUrl = endpointUrl } ;

            Session session = null;
            try
            {
                session = await OpcSessionHelper.Instance.GetSessionAsync(Session.SessionID, endpointUrl);
            }
            catch (Exception exception)
            {
                // Check for untrusted certificate
                ServiceResultException ex = exception as ServiceResultException;
                if ((ex != null) && (ex.InnerResult != null) && (ex.InnerResult.StatusCode == StatusCodes.BadCertificateUntrusted))
                {
                    sessionModel.ErrorHeader = Strings.UntrustedCertificate;
                    return Json(sessionModel);
                }

                // Generate an error to be shown in the error view and trace.
                string errorMessageTrace = string.Format(Strings.BrowserConnectException, exception.Message,
                exception.InnerException?.Message ?? "--", exception?.StackTrace ?? "--"); 
                Trace.TraceError(errorMessageTrace);
                sessionModel.ErrorHeader = Strings.BrowserConnectErrorHeader;

                return Json(sessionModel);
            }

            Session["EndpointUrl"] = endpointUrl;

            return View("Browse", sessionModel);
        }

        /// <summary>
        /// Post form method to connect to an OPC UA server.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken(Order = 1)]
        [RequirePermission(Permission.BrowseOpcServer)]
        public async Task<ActionResult> ConnectWithTrust(string endpointURL)
        {
            OpcSessionModel sessionModel = new OpcSessionModel { EndpointUrl = endpointURL };

            // Check that there is a session already in our cache data
            OpcSessionCacheData entry;
            if (OpcSessionHelper.Instance.OpcSessionCache.TryGetValue(Session.SessionID, out entry))
            {
                if (string.Equals(entry.EndpointURL, endpointURL, StringComparison.InvariantCultureIgnoreCase))
                {
                    OpcSessionCacheData newValue = new OpcSessionCacheData
                    {
                        CertThumbprint = entry.CertThumbprint,
                        OPCSession = entry.OPCSession,
                        EndpointURL = entry.EndpointURL,
                        Trusted = true
                    };
                    OpcSessionHelper.Instance.OpcSessionCache.TryUpdate(Session.SessionID, newValue, entry);

                    return await Connect(endpointURL);
                }
            }

            // Generate an error to be shown in the error view.
            // Since we should only get here when folks are trying to hack the site,
            // make the error generic so not to reveal too much about the internal workings of the site.
            Trace.TraceError(Strings.BrowserConnectErrorHeader);
            sessionModel.ErrorHeader = Strings.BrowserConnectErrorHeader;

            return Json(sessionModel);
        }

        /// <summary>
        /// Get method to disconnect from the currently connected OPC UA server.
        /// </summary>
        [HttpGet]
        [RequirePermission(Permission.BrowseOpcServer)]
        public ActionResult Disconnect(string backUrl)
        {
            return Disconnect(null, backUrl);
        }

        /// <summary>
        /// Post method to disconnect from the currently connected OPC UA server.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken(Order = 1)] 
        [RequirePermission(Permission.BrowseOpcServer)]
        public ActionResult Disconnect(FormCollection form, string backUrl)
        {
            try
            {
                OpcSessionHelper.Instance.Disconnect(Session.SessionID);
                Session["EndpointUrl"] = "";
            }
            catch (Exception exception)
            {
                // Trace an error and return to the connect view.
                var errorMessage = string.Format(Strings.BrowserDisconnectException, exception.Message,
                    exception.InnerException?.Message ?? "--", exception?.StackTrace ?? "--");
                Trace.TraceError(errorMessage);
            }

            if (OpcSessionHelper.Instance.InitResult != "Good")
            {
                return RedirectToAction("Index", "Dashboard");
            }
            else
            {
                if ((backUrl != null) && (backUrl.Contains("AddOpcServer")))
                {
                    return RedirectToAction("Index", "AddOpcServer");
                }
                else
                {
                    OpcSessionModel sessionModel = new OpcSessionModel();
                    sessionModel.PrepopulatedEndpoints = new SelectList(_prepopulatedEndpoints, "Value", "Text");
                    return View("Index", sessionModel);
                }
            }
        }

        /// <summary>
        /// Post method to read information of the root node of connected OPC UA server.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken(Order = 1)]
        [RequirePermission(Permission.BrowseOpcServer)]
        public async Task<ActionResult> GetRootNode()
        {
            ReferenceDescriptionCollection references;
            Byte[] continuationPoint;
            var jsonTree = new List<object>();

            bool retry = true;
            while (true)
            { 
                try
                {
                    Session session = await OpcSessionHelper.Instance.GetSessionAsync(Session.SessionID, (string)Session["EndpointUrl"]);

                    session.Browse(
                        null,
                        null,
                        ObjectIds.ObjectsFolder,
                        0u,
                        BrowseDirection.Forward,
                        ReferenceTypeIds.HierarchicalReferences,
                        true,
                        0,
                        out continuationPoint,
                        out references);
                    jsonTree.Add(new { id = ObjectIds.ObjectsFolder.ToString(), text = Strings.BrowserRootNodeName, children = (references?.Count != 0) });

                    return Json(jsonTree, JsonRequestBehavior.AllowGet);
                }
                catch (Exception exception)
                {
                    OpcSessionHelper.Instance.Disconnect(Session.SessionID);
                    if (!retry)
                    {
                        return Content(CreateOpcExceptionActionString(exception));
                    }
                    retry = false;
                }
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

            ReferenceDescriptionCollection references;
            Byte[] continuationPoint;
            var jsonTree = new List<object>();

            // read the currently published nodes
            Session session = null;
            string[] publishedNodes = null;
            string endpointUrl = null;
            try
            {
                session = await OpcSessionHelper.Instance.GetSessionAsync(Session.SessionID, (string)Session["EndpointUrl"]);
                endpointUrl = session.ConfiguredEndpoint.EndpointUrl.AbsoluteUri;
                publishedNodes = await GetPublishedNodes(endpointUrl);
                Trace.TraceInformation("Currently is/are {0} node(s) published on endpoint '{1}'.", publishedNodes.Length, endpointUrl);
            }
            catch (Exception e)
            {
                // do nothing, since we still want to show the tree
                Trace.TraceWarning("Could not read published nodes for endpoint '{0}'.", endpointUrl);
            }

            bool retry = true;
            while (true)
            {
                try
                {
                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();
                    session.Browse(
                        null,
                        null,
                        node,
                        0u,
                        BrowseDirection.Forward,
                        ReferenceTypeIds.HierarchicalReferences,
                        true,
                        0,
                        out continuationPoint,
                        out references);

                    Trace.TraceInformation("Browse {0} ms", stopwatch.ElapsedMilliseconds);

                    if (references != null)
                    {
                        var idList = new List<string>();
                        foreach (var nodeReference in references)
                        {
                            bool idFound = false;
                            foreach (var id in idList)
                            {
                                if (id == nodeReference.NodeId.ToString())
                                {
                                    idFound = true;
                                }
                            }
                            if (idFound == true)
                            {
                                continue;
                            }

                            ReferenceDescriptionCollection childReferences = null;
                            Byte[] childContinuationPoint;
                        
                            session.Browse(
                                null,
                                null,
                                ExpandedNodeId.ToNodeId(nodeReference.NodeId, session.NamespaceUris),
                                0u,
                                BrowseDirection.Forward,
                                ReferenceTypeIds.HierarchicalReferences,
                                true,
                                0,
                                out childContinuationPoint,
                                out childReferences);

                            INode currentNode = null;
                            try
                            {
                                currentNode = session.ReadNode(ExpandedNodeId.ToNodeId(nodeReference.NodeId, session.NamespaceUris));
                            }
                            catch (Exception)
                            {
                                // skip this node
                                continue;
                            }

                            byte currentNodeAccessLevel = 0;
                            byte currentNodeEventNotifier = 0;
                            bool currentNodeExecutable = false;

                            VariableNode variableNode = currentNode as VariableNode;
                            if (variableNode != null)
                            {
                                currentNodeAccessLevel = variableNode.UserAccessLevel;
                                if (!PermsChecker.HasPermission(Permission.ControlOpcServer))
                                {
                                    currentNodeAccessLevel = (byte)((uint)currentNodeAccessLevel & ~0x2);
                                }
                            }

                            ObjectNode objectNode = currentNode as ObjectNode;
                            if (objectNode != null)
                            {
                                currentNodeEventNotifier = objectNode.EventNotifier;
                            }

                            ViewNode viewNode = currentNode as ViewNode;
                            if (viewNode != null)
                            {
                                currentNodeEventNotifier = viewNode.EventNotifier;
                            }

                            MethodNode methodNode = currentNode as MethodNode;
                            if (methodNode != null && PermsChecker.HasPermission(Permission.ControlOpcServer))
                            {
                                currentNodeExecutable = methodNode.UserExecutable;
                            }

                            var isPublished = false;
                            var isRelevant = false;
                            if (publishedNodes != null)
                            {
                                Session stationSession = await OpcSessionHelper.Instance.GetSessionAsync(Session.SessionID, (string)Session["EndpointUrl"]);
                                string urn = stationSession.ServerUris.GetString(0);
                                foreach (var nodeId in publishedNodes)
                                {
                                    if (nodeId == nodeReference.NodeId.ToString())
                                    {
                                        isPublished = true;
                                        ContosoOpcUaNode contosoOpcUaNode = Startup.Topology.GetOpcUaNode(urn, nodeId);
                                        if (contosoOpcUaNode?.Relevance != null)
                                        {
                                            isRelevant = true;
                                        }
                                    }
                                }
                            }

                            jsonTree.Add(new
                            {
                                id = ("__" + node + delimiter[0] + nodeReference.NodeId.ToString()),
                                text = nodeReference.DisplayName.ToString(),
                                nodeClass = nodeReference.NodeClass.ToString(),
                                accessLevel = currentNodeAccessLevel.ToString(),
                                eventNotifier = currentNodeEventNotifier.ToString(),
                                executable = currentNodeExecutable.ToString(),
                                children = (childReferences.Count == 0) ? false : true,
                                publishedNode = isPublished,
                                relevantNode = isRelevant
                            });
                            idList.Add(nodeReference.NodeId.ToString());
                        }

                        // If there are no children, then this is a call to read the properties of the node itself.
                        if (jsonTree.Count == 0)
                        {
                            INode currentNode = session.ReadNode(new NodeId(node));

                            byte currentNodeAccessLevel = 0;
                            byte currentNodeEventNotifier = 0;
                            bool currentNodeExecutable = false;

                            VariableNode variableNode = currentNode as VariableNode;

                            if (variableNode != null)
                            {
                                currentNodeAccessLevel = variableNode.UserAccessLevel;
                                if (!PermsChecker.HasPermission(Permission.ControlOpcServer))
                                {
                                    currentNodeAccessLevel = (byte)((uint)currentNodeAccessLevel & ~0x2);
                                }
                            }

                            ObjectNode objectNode = currentNode as ObjectNode;

                            if (objectNode != null)
                            {
                                currentNodeEventNotifier = objectNode.EventNotifier;
                            }

                            ViewNode viewNode = currentNode as ViewNode;

                            if (viewNode != null)
                            {
                                currentNodeEventNotifier = viewNode.EventNotifier;
                            }

                            MethodNode methodNode = currentNode as MethodNode;

                            if (methodNode != null && PermsChecker.HasPermission(Permission.ControlOpcServer))
                            {
                                currentNodeExecutable = methodNode.UserExecutable;
                            }

                            jsonTree.Add(new
                            {
                                id = jstreeNode,
                                text = currentNode.DisplayName.ToString(),
                                nodeClass = currentNode.NodeClass.ToString(),
                                accessLevel = currentNodeAccessLevel.ToString(),
                                eventNotifier = currentNodeEventNotifier.ToString(),
                                executable = currentNodeExecutable.ToString(),
                                children = false
                            });
                        }
                    }

                    stopwatch.Stop();
                    Trace.TraceInformation("GetChildren took {0} ms", stopwatch.ElapsedMilliseconds);

                    return Json(jsonTree, JsonRequestBehavior.AllowGet);
                }
                catch (Exception exception)
                {
                    OpcSessionHelper.Instance.Disconnect(Session.SessionID);
                    if (!retry)
                    {
                        return Content(CreateOpcExceptionActionString(exception));
                    }
                    retry = false;
                }
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
            var actionResult = "";

            if (jstreeNodeSplit.Length == 1)
            {
                node = jstreeNodeSplit[0];
            }
            else
            {
                node = jstreeNodeSplit[1];
            }

            bool retry = true;
            while (true)
            { 
                try
                {
                    DataValueCollection values = null;
                    DiagnosticInfoCollection diagnosticInfos = null;
                    ReadValueIdCollection nodesToRead = new ReadValueIdCollection();
                    ReadValueId valueId = new ReadValueId();
                    valueId.NodeId = new NodeId(node);
                    valueId.AttributeId = Attributes.Value;
                    valueId.IndexRange = null;
                    valueId.DataEncoding = null;
                    nodesToRead.Add(valueId);
                    Session session = await OpcSessionHelper.Instance.GetSessionAsync(Session.SessionID, (string)Session["EndpointUrl"]);
                    ResponseHeader responseHeader = session.Read(null, 0, TimestampsToReturn.Both, nodesToRead, out values, out diagnosticInfos);
                    string value = "";
                    if (values[0].Value != null)
                    {
                        if (values[0].WrappedValue.ToString().Length > 40)
                        {
                            value = values[0].WrappedValue.ToString().Substring(0, 40);
                            value += "...";
                        }
                        else
                        {
                            value = values[0].WrappedValue.ToString();
                        }
                    }
                    // We return the HTML formatted content, which is shown in the context panel.
                    actionResult = Strings.BrowserOpcDataValueLabel + ": " + value + @"<br/>" +
                                    Strings.BrowserOpcDataStatusLabel + ": " + values[0].StatusCode + @"<br/>" +
                                    Strings.BrowserOpcDataSourceTimestampLabel + ": " + values[0].SourceTimestamp + @"<br/>" +
                                    Strings.BrowserOpcDataServerTimestampLabel + ": " + values[0].ServerTimestamp;
                    return Content(actionResult);
                }
                catch (Exception exception)
                {
                    if (!retry)
                    {
                        return Content(CreateOpcExceptionActionString(exception));
                    }
                    retry = false;
                }
            }
        }

        public async Task<Session> ConnectToPublisher(string sessionId)
        {
            uint publisherServerPort = 62222;
            string publisherAbsolutePath = "/UA/Publisher";

            // Build Publisher URL from the OPC server URL we're currently browsing
            Session stationSession = await OpcSessionHelper.Instance.GetSessionAsync(Session.SessionID, (string)Session["EndpointUrl"]);
            string domainName = stationSession.Endpoint.EndpointUrl.Substring("opc.tcp://".Length);
            string stationName = domainName;
            Uri stationUri;
            Uri publisherUri;
            try
            {
                stationUri = new Uri(stationSession.Endpoint.EndpointUrl);
                string dnsLabels = stationUri.DnsSafeHost.Contains(".") ? stationUri.DnsSafeHost.Substring(stationUri.DnsSafeHost.IndexOf(".")) : "";
                publisherUri = new Uri($"{stationUri.Scheme}://publisher{dnsLabels}:{publisherServerPort}{publisherAbsolutePath}");
            }
            catch
            {
                throw;
            }

            // Try to connect to publisher
            return await OpcSessionHelper.Instance.GetSessionWithImplicitTrust(sessionId, publisherUri.ToString());
        }

        /// <summary>
        /// Returns the list of published nodes of the OPC UA server with the given endpointUrl
        /// </summary>
        private async Task<string[]> GetPublishedNodes(string endpointUrl)
        {
            List<string> publishedNodes = new List<string>();
            Session publisherSession = null;
            string publisherSessionId = Guid.NewGuid().ToString();

            // read the published nodes from the publisher
            try
            {
                try
                {
                    publisherSession = await ConnectToPublisher(publisherSessionId);
                }
                catch
                {
                    return null;
                }

                if (publisherSession != null)
                {
                    VariantCollection inputArguments = new VariantCollection();
                    Variant endpointUrlValue;
                    try
                    {
                        OpcSessionHelper.Instance.BuildDataValue(ref publisherSession, ref endpointUrlValue, TypeInfo.GetDataTypeId(string.Empty), ValueRanks.Scalar, endpointUrl);
                    }
                    catch (Exception e)
                    {
                        return null;
                    }
                    inputArguments.Add(endpointUrlValue);

                    bool retry = true;
                    while (true)
                    {
                        try
                        {
                            CallMethodRequestCollection requests = new CallMethodRequestCollection();
                            CallMethodResultCollection results;
                            DiagnosticInfoCollection diagnosticInfos = null;
                            CallMethodRequest request = new CallMethodRequest();
                            request.ObjectId = new NodeId("Methods", 2);
                            request.MethodId = new NodeId("GetPublishedNodes", 2);
                            request.InputArguments = inputArguments;
                            requests.Add(request);
                            ResponseHeader responseHeader = publisherSession.Call(null, requests, out results, out diagnosticInfos);
                            if (StatusCode.IsBad(results[0].StatusCode))
                            {
                                return null;
                            }
                            else
                            {
                                if (results?[0]?.OutputArguments.Count == 1)
                                {
                                    string stringResult = results[0].OutputArguments[0].ToString();

                                    int jsonStartIndex = stringResult.IndexOf("[");
                                    int jsonEndIndex = stringResult.IndexOf("]");
                                    PublishedNodesCollection nodelist = JsonConvert.DeserializeObject<PublishedNodesCollection>(stringResult.Substring(jsonStartIndex, jsonEndIndex - jsonStartIndex + 1));
                                    foreach (NodeLookup node in nodelist)
                                    {
                                        publishedNodes.Add(node.NodeID.ToString());
                                    }

                                    return publishedNodes.ToArray();
                                }
                                else
                                {
                                    return null;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            if (!retry)
                            {
                                return null;
                            }
                            retry = false;
                        }
                    }
                }
                else
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                return null;
            }
            finally
            {
                if (publisherSession != null)
                {
                    OpcSessionHelper.Instance.Disconnect(publisherSessionId);
                }
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
            Session publisherSession;
            string publisherSessionId = Guid.NewGuid().ToString();

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
                try
                {
                    publisherSession = await ConnectToPublisher(publisherSessionId);
                }
                catch
                {
                    return Content(CreateOpcExceptionActionString(new Exception(Strings.Error)));
                }

                if (publisherSession != null)
                {
                    VariantCollection inputArguments = new VariantCollection();
                    Variant nodeIdValue;
                    Variant endpointUrlValue;
                    try
                    {
                        OpcSessionHelper.Instance.BuildDataValue(ref publisherSession, ref nodeIdValue, TypeInfo.GetDataTypeId(string.Empty), ValueRanks.Scalar, node);
                        OpcSessionHelper.Instance.BuildDataValue(ref publisherSession, ref endpointUrlValue, TypeInfo.GetDataTypeId(string.Empty), ValueRanks.Scalar, (string)Session["EndpointUrl"]);
                    }
                    catch (Exception exception)
                    {
                        actionResult = string.Format(Strings.BrowserOpcWriteConversionProblem, exception.Message);
                        return Content(actionResult);
                    }
                    inputArguments.Add(nodeIdValue);
                    inputArguments.Add(endpointUrlValue);

                    bool retry = true;
                    while (true)
                    {
                        try
                        {
                            CallMethodRequestCollection requests = new CallMethodRequestCollection();
                            CallMethodResultCollection results;
                            DiagnosticInfoCollection diagnosticInfos = null;
                            CallMethodRequest request = new CallMethodRequest();
                            request.ObjectId = new NodeId("Methods", 2);
                            if (method == "unpublish")
                            {
                                request.MethodId = new NodeId("UnpublishNode", 2);
                            }
                            else
                            {
                                request.MethodId = new NodeId("PublishNode", 2);
                            }
                            request.InputArguments = inputArguments;
                            requests.Add(request);
                            ResponseHeader responseHeader = publisherSession.Call(null, requests, out results, out diagnosticInfos);
                            if (StatusCode.IsBad(results[0].StatusCode))
                            {
                                actionResult = Strings.BrowserOpcMethodCallFailed + @"<br/><br/>" +
                                                Strings.BrowserOpcMethodStatusCode + ": " + results[0].StatusCode;
                                if (diagnosticInfos.Count > 0)
                                {
                                    actionResult += @"<br/><br/>" + Strings.BrowserOpcDataDiagnosticInfoLabel + ": " + diagnosticInfos;
                                }
                            }
                            else
                            {
                                if (results[0].OutputArguments.Count == 0)
                                {
                                    actionResult = Strings.BrowserOpcMethodCallSucceeded;
                                }
                                else
                                {
                                    actionResult = Strings.BrowserOpcMethodCallSucceededWithResults + @"<br/><br/>";
                                    for (int ii = 0; ii < results[0].OutputArguments.Count; ii++)
                                    {
                                        actionResult += results[0].OutputArguments[ii] + "@<br/>";
                                    }
                                }
                            }
                            return Content(actionResult);
                        }
                        catch (Exception exception)
                        {
                            if (!retry)
                            {
                                return Content(CreateOpcExceptionActionString(exception));
                            }
                            retry = false;
                        }
                    }
                }
                else
                {
                    throw new Exception(Strings.SessionNull);
                }
            }
            catch (Exception exception)
            {
                return Content(CreateOpcExceptionActionString(exception));
            }
            finally
            {
                if (publisherSessionId != null)
                {
                    OpcSessionHelper.Instance.Disconnect(publisherSessionId);
                }
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
            var actionResult = "";

            if (jstreeNodeSplit.Length == 1)
            {
                node = jstreeNodeSplit[0];
            }
            else
            {
                node = jstreeNodeSplit[1];
            }

            bool retry = true;
            while (true)
            {
                try
                {
                    DataValueCollection values = null;
                    DiagnosticInfoCollection diagnosticInfos = null;
                    ReadValueIdCollection nodesToRead = new ReadValueIdCollection();
                    ReadValueId valueId = new ReadValueId();
                    valueId.NodeId = new NodeId(node);
                    valueId.AttributeId = Attributes.Value;
                    valueId.IndexRange = null;
                    valueId.DataEncoding = null;
                    nodesToRead.Add(valueId);
                    Session session = await OpcSessionHelper.Instance.GetSessionAsync(Session.SessionID, (string)Session["EndpointUrl"]);
                    ResponseHeader responseHeader = session.Read(null, 0, TimestampsToReturn.Both, nodesToRead, out values,
                        out diagnosticInfos);
                    if (values[0].Value != null)
                    {
                        if (values[0].WrappedValue.ToString().Length > 30)
                        {
                            actionResult = values[0].WrappedValue.ToString().Substring(0, 30);
                            actionResult += "...";
                        }
                        else
                        {
                            actionResult = values[0].WrappedValue.ToString();
                        }
                    }
                    return Content(actionResult);
                }
                catch (Exception exception)
                {
                    if (!retry)
                    {
                        return Content(CreateOpcExceptionActionString(exception));
                    }
                    retry = false;
                }
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
            Session session = await OpcSessionHelper.Instance.GetSessionAsync(Session.SessionID, (string)Session["EndpointUrl"]);
            bool retry = true;
            while (true)
            {
                try
                { 
                    return Content(OpcSessionHelper.Instance.WriteOpcNode(jstreeNode, newValue, session));
                }
                catch (Exception exception)
                {
                    if (!retry)
                    {
                        return Content(CreateOpcExceptionActionString(exception));
                    }
                    retry = false;
                }
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
            var jsonParameter = new List<object>();
            int parameterCount = 0;

            if (jstreeNodeSplit.Length == 1)
            {
                node = jstreeNodeSplit[0];
            }
            else
            {
                node = jstreeNodeSplit[1];
            }
            bool retry = true;
            while (true)
            {
                try
                {
                    QualifiedName browseName = null;
                    browseName = Opc.Ua.BrowseNames.InputArguments;

                    ReferenceDescriptionCollection references = null;
                    Byte[] continuationPoint;

                    Session session = await OpcSessionHelper.Instance.GetSessionAsync(Session.SessionID, (string)Session["EndpointUrl"]);
                    session.Browse(
                            null,
                            null,
                            node,
                            1u,
                            BrowseDirection.Forward,
                            ReferenceTypeIds.HasProperty,
                            true,
                            0,
                            out continuationPoint,
                            out references);
                    if (references.Count == 1)
                    {
                        var nodeReference = references[0];
                        VariableNode argumentsNode = session.ReadNode(ExpandedNodeId.ToNodeId(nodeReference.NodeId, session.NamespaceUris)) as VariableNode;
                        DataValue value = session.ReadValue(argumentsNode.NodeId);

                        ExtensionObject[] argumentsList = value.Value as ExtensionObject[];
                        for (int ii = 0; ii < argumentsList.Length; ii++)
                        {
                            Argument argument = (Argument)argumentsList[ii].Body;
                            NodeId nodeId = new NodeId(argument.DataType);
                            Node dataTypeIdNode = session.ReadNode(nodeId);
                            jsonParameter.Add(new { name = argument.Name, value = argument.Value, valuerank = argument.ValueRank, arraydimentions = argument.ArrayDimensions, description = argument.Description.Text, datatype = nodeId.Identifier, typename = dataTypeIdNode.DisplayName.Text });
                        }
                        parameterCount = argumentsList.Length;
                    }
                    else
                    {
                        parameterCount = 0;
                    }

                    return Json(new { count = parameterCount, parameter = jsonParameter }, JsonRequestBehavior.AllowGet);
                }
                catch (Exception exception)
                {
                    if (!retry)
                    {
                        return Content(CreateOpcExceptionActionString(exception));
                    }
                    retry = false;
                }
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
            int count = values.Count;
            VariantCollection inputArguments = new VariantCollection();

            bool retry = true;
            while (true)
            {
                try
                {
                    if (session == null)
                    {
                        session = await OpcSessionHelper.Instance.GetSessionAsync(Session.SessionID, (string)Session["EndpointUrl"]);
                    }

                    for (int i = 0; i < count; i++)
                    {
                        Variant value = new Variant();
                        NodeId dataTypeNodeId = "i=" + originalData[i].Datatype;
                        string dataTypeName = originalData[i].TypeName;
                        Int32 valueRank = Convert.ToInt32(originalData[i].ValueRank, CultureInfo.InvariantCulture);
                        string newValue = values[i].Value.ToString();

                        try
                        {
                            OpcSessionHelper.Instance.BuildDataValue(ref session, ref value, dataTypeNodeId, valueRank, newValue);
                            inputArguments.Add(value);
                        }
                        catch (Exception exception)
                        {
                            actionResult = string.Format(Strings.BrowserOpcWriteConversionProblem, newValue,
                                dataTypeName, exception.Message);
                            return Content(actionResult);
                        }
                    }

                    CallMethodRequestCollection requests = new CallMethodRequestCollection();
                    CallMethodResultCollection results;
                    DiagnosticInfoCollection diagnosticInfos = null;
                    CallMethodRequest request = new CallMethodRequest();
                    request.ObjectId = new NodeId(parentNode);
                    request.MethodId = new NodeId(node);
                    request.InputArguments = inputArguments;
                    requests.Add(request);
                    ResponseHeader responseHeader = session.Call(null, requests, out results, out diagnosticInfos);
                    if (StatusCode.IsBad(results[0].StatusCode))
                    {
                        actionResult = Strings.BrowserOpcMethodCallFailed + @"<br/><br/>" +
                                       Strings.BrowserOpcMethodStatusCode + ": " + results[0].StatusCode;
                        if (diagnosticInfos.Count > 0)
                        {
                            actionResult += @"<br/><br/>" + Strings.BrowserOpcDataDiagnosticInfoLabel + ": " + diagnosticInfos;
                        }
                    }
                    else
                    {
                        if (results[0].OutputArguments.Count == 0)
                        {
                            actionResult = Strings.BrowserOpcMethodCallSucceeded;
                        }
                        else
                        {
                            actionResult = Strings.BrowserOpcMethodCallSucceededWithResults + @"<br/><br/>";
                            for (int ii = 0; ii < results[0].OutputArguments.Count; ii++)
                            {
                                actionResult += results[0].OutputArguments[ii] + "@<br/>";
                            }
                        }
                    }
                    return Content(actionResult);
                }
                catch (Exception exception)
                {
                    if (!retry)
                    {
                        return Content(CreateOpcExceptionActionString(exception));
                    }
                    retry = false;
                }
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
            errorMessage = Strings.BrowserErrorUnknown;
            string actionResult = HttpUtility.HtmlEncode(errorMessage);
            Response.StatusCode = 1;

            return actionResult;
        }
    }
}
