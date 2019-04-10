using Microsoft.AspNet.SignalR;
using Microsoft.Azure.IIoT.OpcUa.Api;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Twin
{
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.Azure.IIoT.Http.Auth;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models;
    using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Services;
    using Serilog;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class TwinServiceConfig : ITwinConfig
    {
        public string OpcUaTwinServiceUrl { get; }
        public string OpcUaTwinServiceResourceId { get; }
        public TwinServiceConfig(string url, string resourceId)
        {
            OpcUaTwinServiceUrl = url;
            OpcUaTwinServiceResourceId = resourceId;
        }
    }


    public class TwinService
    {
        private TwinServiceClient _twinServiceHandler = null;
        IHttpClient _httpClient = null;
        ILogger _logger = null;
        ITwinConfig _config = null;

        public TwinService(string serviceUrl, string resource)
        {
            _logger = LogEx.Trace();

            _httpClient = new HttpClient(new HttpClientFactory(new HttpHandlerFactory(new List<IHttpHandler> {
                new HttpBearerAuthentication(new BehalfOfTokenProvider(), _logger)
            }, _logger), _logger), _logger);

            _config = new TwinServiceConfig(serviceUrl, resource);
            _twinServiceHandler = new TwinServiceClient(_httpClient, _config, _logger);
        }

        public async Task<BrowseResponseApiModel> NodeBrowseAsync(string endpoint, BrowseRequestApiModel content)
        {
            var applications = await _twinServiceHandler.NodeBrowseAsync(endpoint, content).ConfigureAwait(false);
            return applications;
        }

        public BrowseResponseApiModel NodeBrowse(string endpoint, BrowseRequestApiModel content)
        {
            Task<BrowseResponseApiModel> t = Task.Run(() => _twinServiceHandler.NodeBrowseAsync(endpoint, content));
            return t.Result;
        }

        public async Task<PublishStartResponseApiModel> PublishNodeValuesAsync(string endpoint, PublishStartRequestApiModel content)
        {
            var applications = await _twinServiceHandler.NodePublishStartAsync(endpoint, content).ConfigureAwait(false);
            return applications;
        }

        public PublishStartResponseApiModel PublishNodeValues(string endpoint, PublishStartRequestApiModel content)
        {
            Task<PublishStartResponseApiModel> t = Task.Run(() => _twinServiceHandler.NodePublishStartAsync(endpoint, content));
            return t.Result;
        }

        public async Task<PublishStopResponseApiModel> UnPublishNodeValuesAsync(string endpoint, PublishStopRequestApiModel content)
        {
            var applications = await _twinServiceHandler.NodePublishStopAsync(endpoint, content).ConfigureAwait(false);
            return applications;
        }

        public PublishStopResponseApiModel UnPublishNodeValues(string endpoint, PublishStopRequestApiModel content)
        {
            Task<PublishStopResponseApiModel> t = Task.Run(() => _twinServiceHandler.NodePublishStopAsync(endpoint, content));
            return t.Result;
        }

        public async Task<PublishedItemListResponseApiModel> GetPublishedNodesAsync(string endpoint, PublishedItemListRequestApiModel content)
        {
            var applications = await _twinServiceHandler.NodePublishListAsync(endpoint, content).ConfigureAwait(false);
            return applications;
        }

        public PublishedItemListResponseApiModel GetPublishedNodes(string endpoint, PublishedItemListRequestApiModel content)
        {
            Task<PublishedItemListResponseApiModel> t = Task.Run(() => _twinServiceHandler.NodePublishListAsync(endpoint, content));
            return t.Result;
        }

        public async Task<ValueReadResponseApiModel> ReadNodeValueAsync(string endpoint, ValueReadRequestApiModel content)
        {
            var applications = await _twinServiceHandler.NodeValueReadAsync(endpoint, content).ConfigureAwait(false);
            return applications;
        }

        public ValueReadResponseApiModel ReadNodeValue(string endpoint, ValueReadRequestApiModel content)
        {
            Task<ValueReadResponseApiModel> t = Task.Run(() => _twinServiceHandler.NodeValueReadAsync(endpoint, content));
            return t.Result;
        }

        public async Task<ReadResponseApiModel> ReadNodeAsync(string endpoint, ReadRequestApiModel content)
        {
            var applications = await _twinServiceHandler.NodeReadAsync(endpoint, content).ConfigureAwait(false);
            return applications;
        }

        public ReadResponseApiModel ReadNode(string endpoint, ReadRequestApiModel content)
        {
            Task<ReadResponseApiModel> t = Task.Run(() => _twinServiceHandler.NodeReadAsync(endpoint, content));
            return t.Result;
        }

        public async Task<ValueWriteResponseApiModel> WriteNodeValueAsync(string endpoint, ValueWriteRequestApiModel content)
        {
            var applications = await _twinServiceHandler.NodeValueWriteAsync(endpoint, content).ConfigureAwait(false);
            return applications;
        }

        public ValueWriteResponseApiModel WriteNodeValue(string endpoint, ValueWriteRequestApiModel content)
        {
            Task<ValueWriteResponseApiModel> t = Task.Run(() => _twinServiceHandler.NodeValueWriteAsync(endpoint, content));
            return t.Result;
        }

        public async Task<MethodMetadataResponseApiModel> NodeMethodGetMetadataAsync(string endpoint, MethodMetadataRequestApiModel content)
        {
            var applications = await _twinServiceHandler.NodeMethodGetMetadataAsync(endpoint, content).ConfigureAwait(false);
            return applications;
        }

        public MethodMetadataResponseApiModel NodeMethodGetMetadata(string endpoint, MethodMetadataRequestApiModel content)
        {
            Task<MethodMetadataResponseApiModel> t = Task.Run(() => _twinServiceHandler.NodeMethodGetMetadataAsync(endpoint, content));
            return t.Result;
        }

        public async Task<MethodCallResponseApiModel> NodeMethodCallAsync(string endpoint, MethodCallRequestApiModel content)
        {
            var applications = await _twinServiceHandler.NodeMethodCallAsync(endpoint, content).ConfigureAwait(false);
            return applications;
        }

        public MethodCallResponseApiModel NodeMethodCall(string endpoint, MethodCallRequestApiModel content)
        {
            Task<MethodCallResponseApiModel> t = Task.Run(() => _twinServiceHandler.NodeMethodCallAsync(endpoint, content));
            return t.Result;
        }
    }
}