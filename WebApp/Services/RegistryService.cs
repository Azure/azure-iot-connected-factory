using Microsoft.AspNet.SignalR;
using Microsoft.Azure.IIoT.OpcUa.Api;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Registry
{
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Clients;
    using System.Threading.Tasks;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using System.Collections.Generic;
    using Microsoft.Azure.IIoT.Http.Auth;
    using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Services;
    using Serilog;

    public class RegistryServiceConfig : IRegistryConfig
    {
        public string OpcUaRegistryServiceUrl { get; }
        public string OpcUaRegistryServiceResourceId { get; }
        public RegistryServiceConfig(string url, string resourceId)
        {
            OpcUaRegistryServiceUrl = url;
            OpcUaRegistryServiceResourceId = resourceId;
        }
    }

    public class RegistryService
    {
        RegistryServiceClient _registryServiceHandler = null;
        IHttpClient _httpClient = null;
        ILogger _logger = null;
        IRegistryConfig _config = null;

        public RegistryService(string serviceUrl, string resource)
        {
            _logger = LogEx.Trace();

            _httpClient = new HttpClient(new HttpClientFactory(new HttpHandlerFactory(new List<IHttpHandler> {
                new HttpBearerAuthentication(new BehalfOfTokenProvider(), _logger)
            }, _logger), _logger), _logger);

            _config = new RegistryServiceConfig(serviceUrl, resource);
            
            _registryServiceHandler = new RegistryServiceClient(_httpClient, _config, _logger);
        }

        public async Task<IEnumerable<SupervisorApiModel>> ListSupervisorsAsync()
        {
            var applications = await _registryServiceHandler.ListAllSupervisorsAsync(true).ConfigureAwait(false);
            return applications;
        }

        public IEnumerable<SupervisorApiModel> ListSupervisors()
        {
            Task<IEnumerable<SupervisorApiModel>> t = Task.Run(() => _registryServiceHandler.ListAllSupervisorsAsync(true));
            return t.Result;
        }

        public async Task<IEnumerable<ApplicationInfoApiModel>> ListApplicationsAsync()
        {
            var applications = await _registryServiceHandler.ListAllApplicationsAsync().ConfigureAwait(false);
            return applications;
        }

        public IEnumerable<ApplicationInfoApiModel> ListApplications()
        {
            Task<IEnumerable<ApplicationInfoApiModel>> t = Task.Run(() => _registryServiceHandler.ListAllApplicationsAsync());
            return t.Result;
        }

        public async Task<ApplicationRegistrationApiModel> GetApplicationAsync(string applicationId)
        {
            var applicationRecord = await _registryServiceHandler.GetApplicationAsync(applicationId).ConfigureAwait(false);
            return applicationRecord;
        }

        public ApplicationRegistrationApiModel GetApplication(string applicationId)
        {
            Task<ApplicationRegistrationApiModel> t = Task.Run(() => _registryServiceHandler.GetApplicationAsync(applicationId));
            return t.Result;
        }

        public async Task UnregisterApplicationAsync(string applicationId)
        {
            await _registryServiceHandler.UnregisterApplicationAsync(applicationId).ConfigureAwait(false);
        }

        public void UnregisterApplication(string applicationId)
        {
            Task t = Task.Run(() => _registryServiceHandler.UnregisterApplicationAsync(applicationId));
            t.Wait();
        }

        public async Task<IEnumerable<EndpointInfoApiModel>> ListEndpointsAsync()
        {
            var endpoints = await _registryServiceHandler.ListAllEndpointsAsync(true).ConfigureAwait(false);
            return endpoints;
        }

        public IEnumerable<EndpointInfoApiModel> ListEndpoints()
        {
            Task<IEnumerable<EndpointInfoApiModel>> t = Task.Run(() => _registryServiceHandler.ListAllEndpointsAsync(true));
            return t.Result;
        }


        public async Task ActivateEndpointAsync(string endpointId)
        {
            await _registryServiceHandler.ActivateEndpointAsync(endpointId).ConfigureAwait(false);
        }

        public void ActivateEndpoint(string endpointId)
        {
            Task t = Task.Run(() => _registryServiceHandler.ActivateEndpointAsync(endpointId));
        }

        public async Task DeActivateEndpointAsync(string endpointId)
        {
            await _registryServiceHandler.DeactivateEndpointAsync(endpointId).ConfigureAwait(false);
        }

        public void DeActivateEndpoint(string endpointId)
        {
            Task t = Task.Run(() => _registryServiceHandler.DeactivateEndpointAsync(endpointId));
        }

        public async Task UpdateSupervisorAsync(string supervisorId, SupervisorUpdateApiModel model)
        {
            await _registryServiceHandler.UpdateSupervisorAsync(supervisorId, model).ConfigureAwait(false);
        }

        public void UpdateSupervisor(string supervisorId, SupervisorUpdateApiModel model)
        {
            Task t = Task.Run(() => _registryServiceHandler.UpdateSupervisorAsync(supervisorId, model));
        }

        public async Task<EndpointInfoApiModel> GetEndpointAsync(string endpointId)
        {
            var endpointModel = await _registryServiceHandler.GetEndpointAsync(endpointId, true).ConfigureAwait(false);
            return endpointModel;
        }

        public EndpointInfoApiModel GetEndpoint(string endpointId)
        {
            Task<EndpointInfoApiModel> t = Task.Run(() => _registryServiceHandler.GetEndpointAsync(endpointId, true));
            return t.Result;
        }

        //public SupervisorListApiModel QuerySupervisor(SupervisorQueryApiModel query)
        //{
        //    Task<SupervisorListApiModel> t = Task.Run(() => _registryServiceHandler.QuerySupervisorsAsync(query, true, 1));
        //}
    }
}