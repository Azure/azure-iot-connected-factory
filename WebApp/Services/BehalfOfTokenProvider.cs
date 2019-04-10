using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.IIoT.Auth;
using Microsoft.Azure.IIoT.Auth.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Security.Authentication;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Globalization;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Configuration;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Services
{
    public class BehalfOfTokenProvider : ITokenProvider
    {
        private string _aadClientId;
        private string _aadInstance;
        private string _aadTenant;
        private string _appSecret;
        private AuthenticationResult _result = null;
        private const string kGrantType = "urn:ietf:params:oauth:grant-type:jwt-bearer";

        public BehalfOfTokenProvider()
        {
            _aadClientId = ConfigurationProvider.GetConfigurationSettingValue("AadClientId");
            _aadInstance = ConfigurationProvider.GetConfigurationSettingValue("AadInstance");
            _aadTenant = ConfigurationProvider.GetConfigurationSettingValue("AadTenant");
            _appSecret = ConfigurationProvider.GetConfigurationSettingValue("ClientSecret");
        }

        public async Task<TokenResultModel> GetTokenForAsync(string resource, IEnumerable<string> scopes = null)
        {
            ClientCredential clientCred = new ClientCredential(_aadClientId, _appSecret);
            ClaimsPrincipal current = ClaimsPrincipal.Current;
            if (current == null)
            {
                throw new AuthenticationException("Missing claims principal.");
            }

            System.IdentityModel.Tokens.BootstrapContext bootstrapContext = new System.IdentityModel.Tokens.BootstrapContext(current.Identities.First().BootstrapContext.ToString());

            if (bootstrapContext == null)
            {
                throw new AuthenticationException("bootstrapContext is null.");
            }

            string userName = current.FindFirst(ClaimTypes.Upn) != null
                ? current.FindFirst(ClaimTypes.Upn).Value
                : current.FindFirst(ClaimTypes.Email).Value;
            string userAccessToken = bootstrapContext.Token;
            UserAssertion userAssertion = new UserAssertion(userAccessToken, kGrantType, userName);

            string authority = String.Format(CultureInfo.InvariantCulture, _aadInstance, _aadTenant);
            string userId = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
            AuthenticationContext authContext = new AuthenticationContext(authority);

            string userObjectID = (ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier"))?.Value;
            var user = new UserIdentifier(userObjectID, UserIdentifierType.UniqueId);

            try
            {
                _result = await authContext.AcquireTokenAsync(resource, clientCred, userAssertion);
                return _result.ToTokenResult();
            }
            catch (AdalException ex)
            {
                throw new AuthenticationException(
                    $"Failed to authenticate on behalf of {userName}", ex);
            }
        }

        public Task InvalidateAsync(string resource)
        {
            return Task.CompletedTask;
        }
    }
}