using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using GlobalResources;
using Microsoft.Azure.Devices.Proxy;
using Microsoft.Azure.Devices.Proxy.Provider;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Configuration;
using Opc.Ua;
using Opc.Ua.Bindings.Proxy;
using Opc.Ua.Client;
using Opc.Ua.Configuration;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Helpers
{
    public class OpcSessionCacheData
    {
        public bool Trusted { get; set; }

        public Session OPCSession { get; set; }

        public string CertThumbprint { get; set; }

        public string EndpointURL { get; set; }

        public OpcSessionCacheData()
        {
            Trusted = false;
            EndpointURL = string.Empty;
            CertThumbprint = string.Empty;
            OPCSession = null;
        }
    }

    public class OpcSessionHelper
    {
        public ConfiguredEndpointCollection OpcCachedEndpoints = null;
        public ConcurrentDictionary<string, OpcSessionCacheData> OpcSessionCache = new ConcurrentDictionary<string, OpcSessionCacheData>();
        
        private ApplicationInstance _application = null;
        private ApplicationConfiguration _configuration = null;
        private string _initResult = "Good";

        private static OpcSessionHelper _instance = null;
        private static Object _instanceLock = new Object();

        public static OpcSessionHelper Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_instanceLock)
                    {
                        if (_instance == null)
                        {
                            _instance = new OpcSessionHelper();
                        }
                    }
                }

                return _instance;
            }
        }

        /// <summary>
        /// Load the OPC UA stack application configuration.
        /// </summary>
        public async Task LoadApplicationConfiguration()
        {
            try
            {
                _application = new ApplicationInstance();

                // map the web server directories
                Utils.DefaultLocalFolder = HttpContext.Current.Server.MapPath("~/");
                string appConfig = HttpContext.Current.Server.MapPath("~/bin/Application.Config.xml");

                // load the application configuration
                _configuration = await _application.LoadApplicationConfiguration(appConfig, false);
                _configuration.CertificateValidator.CertificateValidation += new CertificateValidationEventHandler(CertificateValidator_CertificateValidation);

                // check the application certificate
                await _application.CheckApplicationInstanceCertificate(false, 0);

#if !DIRECT_TCP_CONNECT
                // initialize our custom transport via the proxy
                Socket.Provider = new DefaultProvider(ConfigurationProvider.GetConfigurationSettingValue("IotHubOwnerConnectionString"));
                WcfChannelBase.g_CustomTransportChannel = new ProxyTransportChannelFactory();
#endif

                // load cached endpoints
                OpcCachedEndpoints = _configuration.LoadCachedEndpoints(true);
            }
            catch (Exception exception)
            {
                _initResult = string.Format(Strings.OpcStackInitError, exception.Message,
                exception.InnerException?.Message ?? "--", exception?.StackTrace ?? "--");
                Trace.TraceError(_initResult);
            }
        }

        public string InitResult
        {
            get { return _initResult; }
        }

        /// <summary>
        /// Retries the OPC UA application certificate of this instance
        /// </summary>
        public async Task<X509Certificate2> GetApplicationCertificate()
        {
            return await _configuration.SecurityConfiguration.ApplicationCertificate.Find();
        }

        /// <summary>
        /// Action to disconnect from the currently connected OPC UA server.
        /// </summary>
        public void Disconnect(string sessionID)
        {
            OpcSessionCacheData entry;
            if (OpcSessionCache.TryRemove(sessionID, out entry))
            {
                try
                {
                    if (entry.OPCSession != null)
                    {
                        entry.OPCSession.Close();
                    }
                }
                catch
                {
                    // do nothing
                }
            }
        }

        /// <summary>
        /// Ensures session is closed when server does not reply.
        /// </summary>
        private static void StandardClient_KeepAlive(Session sender, KeepAliveEventArgs e)
        {
            if (e != null)
            {
                if (ServiceResult.IsBad(e.Status))
                {
                    e.CancelKeepAlive = true;

                    sender.Close();
                }
            }
        }

        /// <summary>
        /// Checks if there is an active OPC UA session for the provided browser session. If the persisted OPC UA session does not exist,
        /// a new OPC UA session to the given endpoint URL is established.
        /// </summary>
        public async Task<Session> GetSessionAsync(string sessionID, string endpointURL)
        {
            if (string.IsNullOrEmpty(sessionID) || string.IsNullOrEmpty(endpointURL))
            {
                return null;
            }

            OpcSessionCacheData entry;
            if (OpcSessionCache.TryGetValue(sessionID, out entry))
            {
                if (entry.OPCSession != null)
                {
                    if (entry.OPCSession.Connected)
                    {
                        return entry.OPCSession;
                    }

                    try
                    {
                        entry.OPCSession.Close(500);
                    }
                    catch
                    {
                    }
                    entry.OPCSession = null;
                }
            }
            else
            {
                // create a new entry
                OpcSessionCacheData newEntry = new OpcSessionCacheData { EndpointURL = endpointURL };
                OpcSessionCache.TryAdd(sessionID, newEntry);
            }

            Uri endpointURI = new Uri(endpointURL);
            EndpointDescriptionCollection endpointCollection = DiscoverEndpoints(_configuration, endpointURI, 10);
            EndpointDescription selectedEndpoint = SelectUaTcpEndpoint(endpointCollection);
            EndpointConfiguration endpointConfiguration = EndpointConfiguration.Create(_configuration);
            ConfiguredEndpoint endpoint = new ConfiguredEndpoint(null, selectedEndpoint, endpointConfiguration);

            // Check if we have a cached endpoint with the same URL and use that one instead
            foreach (ConfiguredEndpoint cachedEndpoint in OpcCachedEndpoints)
            {
                if (cachedEndpoint.EndpointUrl.AbsoluteUri.Equals(endpointURL, StringComparison.InvariantCultureIgnoreCase))
                {
                    endpoint = cachedEndpoint;
                    break;
                }
            }

            Session session = await Session.Create(
                _configuration,
                endpoint,
                true,
                false,
                sessionID,
                60000,
                new UserIdentity(new AnonymousIdentityToken()),
                null);

            if (session != null)
            {
                session.KeepAlive += new KeepAliveEventHandler(StandardClient_KeepAlive);

                // Update our cache data
                if (OpcSessionCache.TryGetValue(sessionID, out entry))
                {
                    if (string.Equals(entry.EndpointURL, endpointURL, StringComparison.InvariantCultureIgnoreCase))
                    {
                        OpcSessionCacheData newValue = new OpcSessionCacheData
                        {
                            CertThumbprint = entry.CertThumbprint,
                            EndpointURL = entry.EndpointURL,
                            Trusted = entry.Trusted,
                            OPCSession = session
                        };
                        OpcSessionCache.TryUpdate(sessionID, newValue, entry);
                    }
                }
            }

            return session;
        }

        /// <summary>
        /// Write a given OPC node with the given value.
        /// </summary>
        public string WriteOpcNode(string nodeID, string newValue, Session session)
        {
            if (string.IsNullOrEmpty(nodeID) || string.IsNullOrEmpty(newValue) || (session == null))
            {
                return null;
            }

            string[] delimiter = { "__$__" };
            string[] nodeIDSplit = nodeID.Split(delimiter, 3, StringSplitOptions.None);

            string node;
            if (nodeIDSplit.Length == 1)
            {
                node = nodeIDSplit[0];
            }
            else
            {
                node = nodeIDSplit[1];
            }

            // Read the variable
            DataValueCollection values = null;
            DiagnosticInfoCollection readDiagnosticInfos = null;
            ReadValueIdCollection nodesToRead = new ReadValueIdCollection();
            foreach (uint attributeId in Attributes.GetIdentifiers())
            {
                ReadValueId valueId = new ReadValueId();
                valueId.NodeId = new NodeId(node);
                valueId.AttributeId = attributeId;
                valueId.IndexRange = null;
                valueId.DataEncoding = null;
                nodesToRead.Add(valueId);
            }

            ResponseHeader responseHeader = session.Read(null, 0, TimestampsToReturn.Neither, nodesToRead,
                out values, out readDiagnosticInfos);

            // get DataType of the node
            Node dataTypeIdNode = null;
            NodeId dataTypeId = null;
            int valueRank = 0;
            for (int ii = 0; ii < nodesToRead.Count; ii++)
            {
                // check if node supports attribute
                if (values[ii].StatusCode == StatusCodes.BadAttributeIdInvalid)
                {
                    continue;
                }

                switch (nodesToRead[ii].AttributeId)
                {
                    case Attributes.DataType:
                        dataTypeId = values[ii].Value as NodeId;
                        if (dataTypeId != null)
                        {
                            dataTypeIdNode = session.ReadNode(dataTypeId);
                        }
                        break;

                    case Attributes.ValueRank:
                        valueRank = (int)values[ii].Value;
                        break;

                    default:
                        break;
                }
            }

            var nodesToWrite = new WriteValueCollection();

            WriteValue writeValue = new WriteValue();
            writeValue.NodeId = new NodeId(node);
            writeValue.AttributeId = Attributes.Value;
            writeValue.IndexRange = null;

            Variant value = new Variant();
            BuildDataValue(ref session, ref value, dataTypeId, valueRank, newValue);
            writeValue.Value.Value = value;
            nodesToWrite.Add(writeValue);

            StatusCodeCollection results;
            DiagnosticInfoCollection writeDiagnosticInfos;
            session.Write(null, nodesToWrite, out results, out writeDiagnosticInfos);

            return results[0].ToString();
        }

        /// <summary>
        /// Builds a variant for the data value.
        /// </summary>
        public void BuildDataValue(ref Session session, ref Variant value, NodeId dataTypeId, int valueRank, string newValue)
        {
            BuiltInType builtinType = Opc.Ua.TypeInfo.GetBuiltInType(dataTypeId, session.TypeTree);
            Type valueType;
            char[] separator = { ',' };
            string[] newValueStrings;
            if (valueRank == ValueRanks.Scalar)
            {
                newValue = newValue.Trim();
                switch (builtinType)
                {
                    case BuiltInType.Byte:
                    case BuiltInType.SByte:
                    case BuiltInType.Int16:
                    case BuiltInType.UInt16:
                    case BuiltInType.Int32:
                    case BuiltInType.UInt32:
                    case BuiltInType.Int64:
                    case BuiltInType.UInt64:
                    case BuiltInType.Float:
                    case BuiltInType.Double:
                        {
                            valueType = Opc.Ua.TypeInfo.GetSystemType(builtinType, valueRank);
                            if (valueType == typeof(SByte))
                            {
                                value = new Variant(Convert.ToSByte(newValue, CultureInfo.InvariantCulture));
                            }

                            if (valueType == typeof(Byte))
                            {
                                value = new Variant(Convert.ToByte(newValue, CultureInfo.InvariantCulture));
                            }

                            if (valueType == typeof(Int16))
                            {
                                value = new Variant(Convert.ToInt16(newValue, CultureInfo.InvariantCulture));
                            }

                            if (valueType == typeof(UInt16))
                            {
                                value = new Variant(Convert.ToUInt16(newValue, CultureInfo.InvariantCulture));
                            }

                            if (valueType == typeof(Int32))
                            {
                                value = new Variant(Convert.ToInt32(newValue, CultureInfo.InvariantCulture));
                            }

                            if (valueType == typeof(UInt32))
                            {
                                value = new Variant(Convert.ToUInt32(newValue, CultureInfo.InvariantCulture));
                            }

                            if (valueType == typeof(Int64))
                            {
                                value = new Variant(Convert.ToInt64(newValue, CultureInfo.InvariantCulture));
                            }

                            if (valueType == typeof(UInt64))
                            {
                                value = new Variant(Convert.ToUInt64(newValue, CultureInfo.InvariantCulture));
                            }

                            if (valueType == typeof(Single))
                            {
                                value = new Variant(Convert.ToSingle(newValue, CultureInfo.InvariantCulture));
                            }

                            if (valueType == typeof(Double))
                            {
                                value = new Variant(Convert.ToDouble(newValue, CultureInfo.InvariantCulture));
                            }
                        }
                        break;

                    case BuiltInType.Boolean:
                        {
                            value = Convert.ToBoolean(newValue, CultureInfo.InvariantCulture);
                        }
                        break;

                    case BuiltInType.String:
                        {
                            value = newValue;
                        }
                        break;

                    case BuiltInType.Guid:
                        {
                            value = new Guid(newValue);
                        }
                        break;

                    case BuiltInType.DateTime:
                        {
                            value = new Variant(Convert.ToDateTime(newValue, CultureInfo.InvariantCulture));

                        }
                        break;

                    case BuiltInType.ByteString:
                        {
                            if (newValue.Length % 2 == 1)
                            {
                                Exception e = new Exception("ByteString must have even length.");
                                throw e;
                            }
                            Byte[] byteArray = new Byte[newValue.Length / 2];
                            for (int ii = 0; ii < newValue.Length; ii += 2)
                            {
                                string byteValue = newValue.Substring(ii, 2);
                                byteArray[ii / 2] = Convert.ToByte(byteValue, 16);
                            }
                            value = new Variant(byteArray);
                        }
                        break;

                    case BuiltInType.XmlElement:
                        {
                            string newValueDecoded = HttpUtility.HtmlDecode(newValue);
                            value = (XmlElement)Opc.Ua.TypeInfo.Cast(newValueDecoded, BuiltInType.XmlElement);
                        }
                        break;

                    case BuiltInType.NodeId:
                        {
                            NodeId nodeId = new NodeId(newValue.ToString());
                            value = nodeId;
                        }
                        break;

                    case BuiltInType.ExpandedNodeId:
                        {
                            ExpandedNodeId nodeId = new ExpandedNodeId(newValue.ToString());
                            value = nodeId;
                        }
                        break;

                    case BuiltInType.QualifiedName:
                        {
                            QualifiedName name = new QualifiedName(newValue.ToString());
                            value = name;
                        }
                        break;

                    case BuiltInType.LocalizedText:
                        {
                            LocalizedText text = new LocalizedText(newValue.ToString());
                            value = text;
                        }
                        break;

                    case BuiltInType.StatusCode:
                        {
                            StatusCode status = new StatusCode(Convert.ToUInt32(newValue, 16));
                            value = status;
                        }
                        break;

                    case BuiltInType.Variant:
                        {
                            value = (Variant)Opc.Ua.TypeInfo.Cast(newValue, BuiltInType.Variant);
                        }
                        break;

                    case BuiltInType.Enumeration:
                        {
                            value = new Variant(Opc.Ua.TypeInfo.Cast(newValue, BuiltInType.Enumeration));
                        }
                        break;

                    case BuiltInType.ExtensionObject:
                        {
                            value = new ExtensionObject(newValue);
                        }
                        break;

                    case BuiltInType.Number:
                    case BuiltInType.Integer:
                    case BuiltInType.UInteger:
                        {
                            value = new Variant(Opc.Ua.TypeInfo.Cast(newValue, builtinType));
                        }
                        break;
                }
            }
            else if (valueRank == ValueRanks.OneDimension)
            {
                switch (builtinType)
                {
                    case BuiltInType.Byte:
                    case BuiltInType.SByte:
                    case BuiltInType.Int16:
                    case BuiltInType.UInt16:
                    case BuiltInType.Int32:
                    case BuiltInType.UInt32:
                    case BuiltInType.Int64:
                    case BuiltInType.UInt64:
                    case BuiltInType.Float:
                    case BuiltInType.Double:
                        {
                            valueType = Opc.Ua.TypeInfo.GetSystemType(builtinType, valueRank);
                            if (valueType == typeof(SByte[]))
                            {
                                List<SByte> valList = new List<SByte>();
                                newValueStrings = newValue.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                                for (int i = 0; i < newValueStrings.Length; i++)
                                {
                                    valList.Add(Convert.ToSByte(newValueStrings[i], CultureInfo.InvariantCulture));
                                }
                                value = new Variant(valList);
                            }

                            if (valueType == typeof(Byte[]))
                            {
                                List<Byte> valList = new List<Byte>();
                                newValueStrings = newValue.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                                for (int i = 0; i < newValueStrings.Length; i++)
                                {
                                    valList.Add(Convert.ToByte(newValueStrings[i], CultureInfo.InvariantCulture));
                                }
                                value = new Variant(valList);
                            }

                            if (valueType == typeof(Int16[]))
                            {
                                List<Int16> valList = new List<Int16>();
                                newValueStrings = newValue.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                                for (int i = 0; i < newValueStrings.Length; i++)
                                {
                                    valList.Add(Convert.ToInt16(newValueStrings[i], CultureInfo.InvariantCulture));
                                }
                                value = new Variant(valList);
                            }

                            if (valueType == typeof(UInt16[]))
                            {
                                List<UInt16> valList = new List<UInt16>();
                                newValueStrings = newValue.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                                for (int i = 0; i < newValueStrings.Length; i++)
                                {
                                    valList.Add(Convert.ToUInt16(newValueStrings[i], CultureInfo.InvariantCulture));
                                }
                                value = new Variant(valList);
                            }

                            if (valueType == typeof(Int32[]))
                            {
                                List<Int32> valList = new List<Int32>();
                                newValueStrings = newValue.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                                for (int i = 0; i < newValueStrings.Length; i++)
                                {
                                    valList.Add(Convert.ToInt32(newValueStrings[i], CultureInfo.InvariantCulture));
                                }
                                value = new Variant(valList);
                            }

                            if (valueType == typeof(UInt32[]))
                            {
                                List<UInt32> valList = new List<UInt32>();
                                newValueStrings = newValue.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                                for (int i = 0; i < newValueStrings.Length; i++)
                                {
                                    valList.Add(Convert.ToUInt32(newValueStrings[i], CultureInfo.InvariantCulture));
                                }
                                value = new Variant(valList);
                            }

                            if (valueType == typeof(Int64[]))
                            {
                                List<Int64> valList = new List<Int64>();
                                newValueStrings = newValue.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                                for (int i = 0; i < newValueStrings.Length; i++)
                                {
                                    valList.Add(Convert.ToInt64(newValueStrings[i], CultureInfo.InvariantCulture));
                                }
                                value = new Variant(valList);
                            }

                            if (valueType == typeof(UInt64[]))
                            {
                                List<UInt64> valList = new List<UInt64>();
                                newValueStrings = newValue.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                                for (int i = 0; i < newValueStrings.Length; i++)
                                {
                                    valList.Add(Convert.ToUInt64(newValueStrings[i], CultureInfo.InvariantCulture));
                                }
                                value = new Variant(valList);
                            }

                            if (valueType == typeof(Single[]))
                            {
                                List<Single> valList = new List<Single>();
                                newValueStrings = newValue.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                                for (int i = 0; i < newValueStrings.Length; i++)
                                {
                                    valList.Add(Convert.ToSingle(newValueStrings[i], CultureInfo.InvariantCulture));
                                }
                                value = new Variant(valList);
                            }

                            if (valueType == typeof(Double[]))
                            {
                                List<Double> valList = new List<Double>();
                                newValueStrings = newValue.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                                for (int i = 0; i < newValueStrings.Length; i++)
                                {
                                    valList.Add(Convert.ToDouble(newValueStrings[i], CultureInfo.InvariantCulture));
                                }
                                value = new Variant(valList);
                            }

                        }
                        break;

                    case BuiltInType.Boolean:
                        {
                            List<Boolean> valList = new List<Boolean>();
                            newValueStrings = newValue.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                            for (int i = 0; i < newValueStrings.Length; i++)
                            {
                                newValueStrings[i] = newValueStrings[i].Trim();
                                valList.Add(Convert.ToBoolean(newValueStrings[i], CultureInfo.InvariantCulture));
                            }
                            value = new Variant(valList);
                        }
                        break;

                    case BuiltInType.String:
                        {
                            List<String> valList = new List<String>();
                            newValueStrings = newValue.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                            for (int i = 0; i < newValueStrings.Length; i++)
                            {
                                valList.Add(Convert.ToString(newValueStrings[i], CultureInfo.InvariantCulture));
                            }
                            value = new Variant(valList);
                        }
                        break;

                    case BuiltInType.Guid:
                        {
                            List<Guid> valList = new List<Guid>();
                            newValueStrings = newValue.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                            for (int i = 0; i < newValueStrings.Length; i++)
                            {
                                newValueStrings[i] = newValueStrings[i].Trim();
                                valList.Add(new Guid(newValueStrings[i]));
                            }
                            Guid[] valArray = valList.ToArray();
                            value = new Variant(valArray);
                        }
                        break;

                    case BuiltInType.DateTime:
                        {
                            List<DateTime> valList = new List<DateTime>();
                            newValueStrings = newValue.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                            for (int i = 0; i < newValueStrings.Length; i++)
                            {
                                newValueStrings[i] = newValueStrings[i].Trim();
                                valList.Add(Convert.ToDateTime(newValueStrings[i], CultureInfo.InvariantCulture));
                            }
                            value = new Variant(valList);
                        }
                        break;

                    case BuiltInType.ByteString:
                        {
                            List<Byte[]> valList = new List<Byte[]>();
                            newValueStrings = newValue.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                            for (int i = 0; i < newValueStrings.Length; i++)
                            {
                                newValueStrings[i] = newValueStrings[i].Trim();
                                if (newValueStrings[i].Length % 2 == 1)
                                {
                                    Exception e = new Exception("ByteString must have even length.");
                                    throw e;
                                }
                                Byte[] byteArray = new Byte[newValueStrings[i].Length / 2];
                                for (int ii = 0; ii < newValueStrings[i].Length; ii += 2)
                                {
                                    string byteValue = newValueStrings[i].Substring(ii, 2);
                                    byteArray[ii / 2] = Convert.ToByte(byteValue, 16);
                                }
                                valList.Add(byteArray);
                            }
                            value = new Variant(valList);
                        }
                        break;

                    case BuiltInType.XmlElement:
                        {
                            List<XmlElement> valList = new List<XmlElement>();
                            newValueStrings = newValue.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                            for (int i = 0; i < newValueStrings.Length; i++)
                            {
                                newValueStrings[i] = newValueStrings[i].Trim();
                                string newValueDecoded = HttpUtility.HtmlDecode(newValueStrings[i]);
                                valList.Add((XmlElement)Opc.Ua.TypeInfo.Cast(newValueDecoded, BuiltInType.XmlElement));
                            }
                            value = new Variant(valList);
                        }
                        break;

                    case BuiltInType.NodeId:
                        {
                            List<NodeId> valList = new List<NodeId>();
                            newValueStrings = newValue.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                            for (int i = 0; i < newValueStrings.Length; i++)
                            {
                                newValueStrings[i] = newValueStrings[i].Trim();
                                valList.Add((NodeId)Opc.Ua.TypeInfo.Cast(newValueStrings[i], BuiltInType.NodeId));
                            }
                            value = new Variant(valList);
                        }
                        break;

                    case BuiltInType.ExpandedNodeId:
                        {
                            List<ExpandedNodeId> valList = new List<ExpandedNodeId>();
                            newValueStrings = newValue.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                            for (int i = 0; i < newValueStrings.Length; i++)
                            {
                                newValueStrings[i] = newValueStrings[i].Trim();
                                valList.Add((ExpandedNodeId)Opc.Ua.TypeInfo.Cast(newValueStrings[i], BuiltInType.ExpandedNodeId));
                            }
                            value = new Variant(valList);
                        }
                        break;

                    case BuiltInType.QualifiedName:
                        {
                            List<QualifiedName> valList = new List<QualifiedName>();
                            newValueStrings = newValue.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                            for (int i = 0; i < newValueStrings.Length; i++)
                            {
                                valList.Add(new QualifiedName(newValueStrings[i]));
                            }
                            value = new Variant(valList);
                        }
                        break;

                    case BuiltInType.LocalizedText:
                        {
                            List<LocalizedText> valList = new List<LocalizedText>();
                            newValueStrings = newValue.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                            for (int i = 0; i < newValueStrings.Length; i++)
                            {
                                valList.Add(new LocalizedText(newValueStrings[i]));
                            }
                            value = new Variant(valList);
                        }
                        break;

                    case BuiltInType.StatusCode:
                        {
                            List<StatusCode> valList = new List<StatusCode>();
                            newValueStrings = newValue.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                            for (int i = 0; i < newValueStrings.Length; i++)
                            {
                                newValueStrings[i] = newValueStrings[i].Trim();
                                valList.Add(new StatusCode(Convert.ToUInt32(newValueStrings[i], 16)));
                            }
                            value = new Variant(valList);
                        }
                        break;

                    case BuiltInType.Variant:
                        {
                            List<Variant> valList = new List<Variant>();
                            newValueStrings = newValue.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                            for (int i = 0; i < newValueStrings.Length; i++)
                            {
                                valList.Add((Variant)Opc.Ua.TypeInfo.Cast(newValueStrings[i], BuiltInType.Variant));
                            }
                            value = new Variant(valList);
                        }
                        break;

                    case BuiltInType.Enumeration:
                        {
                            List<Enumeration> valList = new List<Enumeration>();
                            newValueStrings = newValue.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                            for (int i = 0; i < newValueStrings.Length; i++)
                            {
                                valList.Add((Enumeration)Opc.Ua.TypeInfo.Cast(newValueStrings[i], BuiltInType.Enumeration));
                            }
                            value = new Variant(valList);
                        }
                        break;

                    case BuiltInType.ExtensionObject:
                        {
                            List<ExtensionObject> valList = new List<ExtensionObject>();
                            newValueStrings = newValue.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                            for (int i = 0; i < newValueStrings.Length; i++)
                            {
                                valList.Add((ExtensionObject)new ExtensionObject(newValueStrings[i]));
                            }
                            value = new Variant(valList);
                        }
                        break;

                    case BuiltInType.Number:
                    case BuiltInType.Integer:
                    case BuiltInType.UInteger:
                        {
                            List<Variant> valList = new List<Variant>();
                            newValueStrings = newValue.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                            for (int i = 0; i < newValueStrings.Length; i++)
                            {
                                newValueStrings[i] = newValueStrings[i].Trim();
                                Variant v = new Variant(Opc.Ua.TypeInfo.Cast(newValueStrings[i], builtinType));
                                valList.Add(v);
                            }
                            value = new Variant(valList);
                        }
                        break;
                }
            }
            else
            {
                Exception e = new Exception("Value rank " + valueRank.ToString(CultureInfo.CurrentCulture) + " is not supported");
                throw e;
            }
        }

        /// <summary>
        /// Establishes an OPC UA session to the given OPC UA server with implicit trust.
        /// </summary>
        public async Task<Session> GetSessionWithImplicitTrust(string sessionID, string appUri)
        {
            // Try to connect to Server
            Session session = null;
            try
            {
                session = await GetSessionAsync(sessionID, appUri);
            }
            catch (ServiceResultException exception)
            when ((exception.InnerResult != null)
               && (exception.InnerResult.StatusCode == StatusCodes.BadCertificateUntrusted))
            {
                // Check that there is a session already in our cache data
                OpcSessionCacheData entry;
                if (OpcSessionCache.TryGetValue(sessionID, out entry))
                {
                    if (string.Equals(entry.EndpointURL, appUri, StringComparison.InvariantCultureIgnoreCase))
                    {
                        // We always trust the Server for which we have generated the URL ourselves
                        OpcSessionCacheData newValue = new OpcSessionCacheData
                        {
                            CertThumbprint = entry.CertThumbprint,
                            OPCSession = entry.OPCSession,
                            EndpointURL = entry.EndpointURL,
                            Trusted = true
                        };
                        OpcSessionCache.TryUpdate(sessionID, newValue, entry);

                        // try again
                        session = await GetSessionAsync(sessionID, appUri);
                    }
                }
            }
            catch (ServiceResultException exception)
            when ((exception.InnerResult != null)
               && (exception.InnerResult.StatusCode != StatusCodes.BadCertificateUntrusted))
            {
                // any other reason than untrusted certificate
                Trace.TraceError($"ServiceResultException occured: '{exception.InnerException.Message}'");
                throw;
            }

            return session;
        }

        /// <summary>
        /// Uses a discovery client to discover the endpoint description of a given server 
        /// </summary>
        private EndpointDescriptionCollection DiscoverEndpoints(ApplicationConfiguration config, Uri discoveryUrl, int timeout)
        {
            EndpointConfiguration configuration = EndpointConfiguration.Create(config);
            configuration.OperationTimeout = timeout;

            using (DiscoveryClient client = DiscoveryClient.Create(
                discoveryUrl,
                EndpointConfiguration.Create(config)))
            {
                try
                {
                    EndpointDescriptionCollection endpoints = client.GetEndpoints(null);
                    return ReplaceLocalHostWithRemoteHost(endpoints, discoveryUrl);
                }
                catch (Exception e)
                {
                    Trace.TraceError("Could not fetch endpoints from url: {0}", discoveryUrl);
                    Trace.TraceError("Reason = {0}", e.Message);
                    throw;
                }
            }
        }

        /// <summary>
        /// Selects the UA TCP endpoint with the highest security level
        /// </summary>
        private EndpointDescription SelectUaTcpEndpoint(EndpointDescriptionCollection endpointCollection)
        {
            EndpointDescription bestEndpoint = null;
            foreach (EndpointDescription endpoint in endpointCollection)
            {
                if (endpoint.TransportProfileUri == Profiles.UaTcpTransport)
                {
                    if ((bestEndpoint == null) ||
                        (endpoint.SecurityLevel > bestEndpoint.SecurityLevel))
                    {
                        bestEndpoint = endpoint;
                    }
                }
            }

            return bestEndpoint;
        }

        /// <summary>
        /// Replaces all instances of "LocalHost" in a collection of endpoint description with the real host name
        /// </summary>
        private EndpointDescriptionCollection ReplaceLocalHostWithRemoteHost(EndpointDescriptionCollection endpoints, Uri discoveryUrl)
        {
            EndpointDescriptionCollection updatedEndpoints = endpoints;

            foreach (EndpointDescription endpoint in updatedEndpoints)
            {
                endpoint.EndpointUrl = Utils.ReplaceLocalhost(endpoint.EndpointUrl, discoveryUrl.DnsSafeHost);

                StringCollection updatedDiscoveryUrls = new StringCollection();
                foreach (string url in endpoint.Server.DiscoveryUrls)
                {
                    updatedDiscoveryUrls.Add(Utils.ReplaceLocalhost(url, discoveryUrl.DnsSafeHost));
                }

                endpoint.Server.DiscoveryUrls = updatedDiscoveryUrls;
            }

            return updatedEndpoints;
        }
        
        /// <summary>
        /// Check for untrusted certificates and only accept them if the user has accepted them
        /// </summary>
        private void CertificateValidator_CertificateValidation(CertificateValidator validator, CertificateValidationEventArgs e)
        {
            if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted)
            {
                // Session is not accessible from here so we need to iterate through all key-value-pairs
                foreach (KeyValuePair<string, OpcSessionCacheData> pair in OpcSessionCache.ToArray())
                {
                    // try processing each entry
                    try
                    {
                        string hostName = pair.Value.EndpointURL.Substring("opc.tcp://".Length);
                        if (hostName.Contains(":"))
                        {
                            hostName = hostName.Substring(0, hostName.IndexOf(':'));
                        }

                        // Look up by cert thumbprint
                        if (string.Equals(pair.Value.CertThumbprint, e.Certificate.Thumbprint, StringComparison.InvariantCultureIgnoreCase))
                        {
                            // check if the current session user has confirmed trust
                            if (pair.Value.Trusted)
                            {
                                // In this case, we accept the cert
                                e.Accept = true;
                                break;
                            }
                        }

                        // Update our cache data
                        if (e.Certificate.Subject.Contains(hostName))
                        {
                            OpcSessionCacheData newValue = new OpcSessionCacheData
                            {
                                OPCSession = pair.Value.OPCSession,
                                EndpointURL = pair.Value.EndpointURL,
                                Trusted = pair.Value.Trusted,
                                CertThumbprint = e.Certificate.Thumbprint
                            };
                            OpcSessionCache.TryUpdate(pair.Key, newValue, pair.Value);
                            break;
                        }
                    }
                    catch (Exception)
                    {
                        // do nothing
                    }
                }
            }
        }
    }
}