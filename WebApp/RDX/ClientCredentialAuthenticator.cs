
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rdx.SystemExtensions;

namespace Microsoft.Rdx.Client.Authentication
{
    public class ClientCredentialAuthenticator : BaseClientAuthenticator
    {
        private readonly string _clientId;
        private readonly string _clientSecret;

        /// <summary>
        /// Authenticator that uses a client secret to authenticat a web app against a web api.
        /// </summary>
        public ClientCredentialAuthenticator(string tenantId, string clientId, string clientSecret, string resource, string logOnUrl = AzureActiveDirectoryLogOnUrl) 
            : base(tenantId, resource, logOnUrl)
        {
            clientId.CheckArgumentNotNullOrWhiteSpace(nameof(clientId));
            clientId.CheckArgumentNotNullOrWhiteSpace(nameof(clientSecret));

            _clientId = clientId;
            _clientSecret = clientSecret;
        }

        protected override async Task<AuthorizationHeaderValue> GetAuthorizationHeaderValueStructAsync(AuthenticationContext authenticationContext, string resource)
        {
            ClientCredential cc = new ClientCredential(_clientId, _clientSecret);
            var authenticationResult = await authenticationContext.AcquireTokenAsync(resource, cc);
            return new AuthorizationHeaderValue("Bearer", authenticationResult.AccessToken);
        }
    }
}
