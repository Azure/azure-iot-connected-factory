using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Rdx.Client;
using Microsoft.Rdx.Client.Authentication;
using Microsoft.Rdx.Client.Events;
using Microsoft.Rdx.Client.Query;
using Microsoft.Rdx.Client.Query.Expressions;
using Microsoft.Rdx.Client.Query.ObjectModel.Aggregates;
using Microsoft.Rdx.Client.Query.ObjectModel.LimitExpressions;
using Microsoft.Rdx.Logging;
using Microsoft.Rdx.SystemExtensions;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Configuration;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.RDX
{
    public static class RDXQueryClient
    {
        /// <summary>
        /// Singleton which is used for all queries
        /// </summary>
        private static RdxGlobalQueryClient _rdxGlobalQueryClient;
        private static RdxEnvironmentQueryClient _rdxEnvironmentQueryClient;
        private static IccString _rdxEnvironmentId;
        private static IccString _rdxEnvironmentFqdn;
        private static string _rdxEnvironmentName;
        private static string _rdxDNSName;

        public static string EnvironmentId { get { return _rdxEnvironmentId.ToString(); } }
        public static string EnvironmentName { get { return _rdxEnvironmentName; } }
        public static string DNSName { get { return _rdxDNSName; } }


        // describe how the RDX configuration strings map to web.config

        /// <summary>
        /// The RDX configuration strings in web.config
        /// mandatory fields
        /// </summary>
        const string rdxAuthenticationTenantId = "AadTenant";
        const string rdxAuthenticationClientId = "AadClientId";
        const string rdxDnsName = "RdxDnsName";
        const string rdxEnvironmentId = "RdxEnvironmentId";
        const string rdxApplicationName = "SolutionName";

        /// <summary>
        /// for authentication, one of the groups must be specified in the web.config
        /// ApplicationCertificateClientAuthenticator
        /// </summary>
        const string rdxAuthenticationClientApplicationId = "RdxAuthenticationClientApplicationId";
        const string rdxAuthenticationClientCertificateThumbprint = "RdxAuthenticationClientCertificateThumbprint";

        /// <summary>
        /// Parameter for ClientCredentialAuthenticator
        /// </summary>
        const string rdxAuthenticationClientSecret = "RdxAuthenticationClientSecret";

        /// <summary>
        /// Parameter for UserClientAuthenticator
        /// </summary>
        const string rdxAuthenticationRedirectUri = "RdxAuthenticationRedirectUri";

        /// <summary>
        /// Create the singleton instance for the RDX C# client SDK
        /// Deletes singleton if the access to the specified environment fails 
        /// to prevent subsequent access of worker threads
        /// </summary>
        public static async void Create()
        {
            while (true)
            {

                RDXTrace.TraceInformation("Start RDX Query Client");

                try
                {
                    ServicePointManager.DefaultConnectionLimit = 1000;
                    CommonLogger.SetWriter(TraceLogWriter.Instance);

                    // Authenticate with the RDX service, depending on configuration one option is used
                    // to create the OAuth2 bearer tokens
                    IClientAuthenticator clientAuthenticator;
                    string tenantId = ConfigurationProvider.GetConfigurationSettingValue(rdxAuthenticationTenantId);
                    IccString clientCertificateThumbprint = new IccString(ConfigurationProvider.GetConfigurationSettingValue(rdxAuthenticationClientCertificateThumbprint));
                    if (clientCertificateThumbprint.IsNullOrWhiteSpace())
                    {
                        string clientId = ConfigurationProvider.GetConfigurationSettingValue(rdxAuthenticationClientId);
                        string clientSecret = ConfigurationProvider.GetConfigurationSettingValue(rdxAuthenticationClientSecret);
                        if (String.IsNullOrWhiteSpace(clientSecret))
                        {
                            Uri redirectUri = new Uri(ConfigurationProvider.GetConfigurationSettingValue(rdxAuthenticationRedirectUri));
                            clientAuthenticator = new UserClientAuthenticator(tenantId, clientId, redirectUri, BaseClientAuthenticator.AzureTimeSeriesResource);
                        }
                        else
                        {
                            clientAuthenticator = new ClientCredentialAuthenticator(tenantId, clientId, clientSecret, BaseClientAuthenticator.AzureTimeSeriesResource);
                        }
                    }
                    else
                    {
                        string applicationClientId = ConfigurationProvider.GetConfigurationSettingValue(rdxAuthenticationClientApplicationId);
                        clientAuthenticator = new ApplicationCertificateClientAuthenticator(applicationClientId, clientCertificateThumbprint, tenantId, BaseClientAuthenticator.AzureTimeSeriesResource);
                    }

                    // Create the RDX client with authenticator and DNS resolver
                    _rdxDNSName = ConfigurationProvider.GetConfigurationSettingValue(rdxDnsName);
                    IccString rdxIccDnsName = new IccString("api." + _rdxDNSName);
                    string solutionName = ConfigurationProvider.GetConfigurationSettingValue(rdxApplicationName);
                    _rdxGlobalQueryClient = new RdxGlobalQueryClient(rdxIccDnsName, solutionName, clientAuthenticator);

                    // Test if our environment exists and is accessible
                    _rdxEnvironmentId = new IccString(ConfigurationProvider.GetConfigurationSettingValue(rdxEnvironmentId));
                    GetEnvironmentsOutput environments = await GetEnvironmentsAsync(CancellationToken.None);

                    Trace.TraceInformation("Got {0} environments: ", environments.Environments.Count);
                    bool foundEnvironment = false;
                    foreach (var env in environments.Environments)
                    {
                        Trace.TraceInformation("  {0} {1}", env.EnvironmentId, env.DisplayName);
                        if (env.EnvironmentId == _rdxEnvironmentId)
                        {
                            foundEnvironment = true;
                            _rdxEnvironmentName = env.DisplayName;
                            _rdxEnvironmentFqdn = env.EnvironmentFqdn;
                            break;
                        }
                    }

                    if (!foundEnvironment)
                    {
                        throw new Exception(String.Format("RDX Environment {0} not found.", _rdxEnvironmentId.ToString()));
                    }

                    _rdxEnvironmentQueryClient = new RdxEnvironmentQueryClient(
                        _rdxEnvironmentFqdn,
                        solutionName,
                        clientAuthenticator);

                    Trace.TraceInformation("..... RDXQueryClient started .....");

                    return;
                }
                catch (Exception e)
                {
                    RDXTrace.TraceError("RDX CreateQueryClient failed: {0}", e.ExceptionToString());
                    _rdxGlobalQueryClient = null;
                    _rdxEnvironmentQueryClient = null;
                }

                RDXTrace.TraceError("Fatal: RDX environment not found. Retry in 60s.");
                await Task.Delay(60000);

            }
        }

        /// <summary>
        /// Get a list of all accessible environments 
        /// </summary>
        /// <param name="token">CancellationToken</param>
        /// <returns>List of environments</returns>
        public static async Task<GetEnvironmentsOutput> GetEnvironmentsAsync(CancellationToken token)
        {
            return await _rdxGlobalQueryClient.GetEnvironmentsAsync(token);
        }

        /// <summary>
        /// Get a list of available time ranges in the environment
        /// </summary>
        /// <param name="token">CancellationToken</param>
        /// <returns>List of time ranges</returns>
        public static async Task<GetAvailabilityOutput> GetAvailabilityAsync(CancellationToken token)
        {
            return await _rdxEnvironmentQueryClient.GetAvailabilityAsync(token);
        }

        /// <summary>
        /// Issue a query for aggregates
        /// </summary>
        /// <param name="searchSpan">Date and time span</param>
        /// <param name="predicate">Predicate definition</param>
        /// <param name="aggregates">Aggregate definition</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>The aggregates</returns>
        public static async Task<AggregatesResult> GetAggregatesAsync(
            DateTimeRange searchSpan,
            Expression predicate,
            IReadOnlyCollection<Aggregate> aggregates,
            CancellationToken cancellationToken)
        {
            return await _rdxEnvironmentQueryClient.GetAggregatesAsync(searchSpan, predicate, aggregates, cancellationToken);
        }

        /// <summary>
        /// Issue a query for events
        /// </summary>
        /// <param name="searchSpan">Date and time span</param>
        /// <param name="predicate">Predicate definition</param>
        /// <param name="limitClause">Limit clause definition</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>The events</returns>
        public static async Task<IEnumerable<IEvent>> GetEventsAsync(
            DateTimeRange searchSpan,
            Expression predicate,
            BaseLimitClause limitClause,
            CancellationToken cancellationToken)
        {
            return await _rdxEnvironmentQueryClient.GetEventsAsync(searchSpan, predicate, limitClause, cancellationToken);
        }

        /// <summary>
        /// Implementation of trace writer for RDX
        /// </summary>
        private sealed class TraceLogWriter : ICommonLogWriter
        {
            public static readonly ICommonLogWriter Instance = new TraceLogWriter();

            private TraceLogWriter()
            {
            }

            public void Info(string message, params object[] args)
            {
                Trace.TraceInformation(message, args);
            }

            public void Error(string message, params object[] args)
            {
                Trace.TraceError(message, args);
            }
        }
    }

    /// <summary>
    /// Helper class to control RDX related traces 
    /// </summary>
    public static class RDXTrace
    {
        /// <summary>
        /// Control information and error tracing
        /// </summary>
        static bool traceInfo = false;
        static bool traceError = true;

        public static void TraceInformation(string format, params object[] args)
        {
            if (traceInfo)
            {
                Trace.TraceInformation(format, args);
            }
        }

        public static void TraceError(string format, params object[] args)
        {
            if (traceError)
            {
                Trace.TraceInformation(format, args);
            }
        }
    }

}


