using System.Globalization;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Configuration;
using Owin;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp
{
    public partial class Startup
    {
        public void ConfigureAuth(IAppBuilder app)
        {

            string aadClientId = ConfigurationProvider.GetConfigurationSettingValue("AadClientId");
            string aadInstance = ConfigurationProvider.GetConfigurationSettingValue("AadInstance");
            string aadTenant = ConfigurationProvider.GetConfigurationSettingValue("AadTenant");
            string authority = string.Format(CultureInfo.InvariantCulture, aadInstance, aadTenant);

            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions());

            app.UseOpenIdConnectAuthentication(
                new OpenIdConnectAuthenticationOptions
                {
                    ClientId = aadClientId,
                    Authority = authority,
                    Notifications = new OpenIdConnectAuthenticationNotifications
                    {
                        AuthenticationFailed = context =>
                        {
                            string appBaseUrl = context.Request.Scheme + "://" + context.Request.Host + context.Request.PathBase;
                            context.ProtocolMessage.RedirectUri = appBaseUrl + "/";
                            context.HandleResponse();
                            context.Response.Redirect(context.ProtocolMessage.RedirectUri);
                            Trace.TraceInformation("ConfigureAuth: AuthenticationFailed - ClientId: {0}, RedirectUir: {1}", context.ProtocolMessage.ClientId, context.ProtocolMessage.RedirectUri);
                            return Task.FromResult(0);
                        },
                        AuthorizationCodeReceived = context =>
                        {
                            Trace.TraceInformation("ConfigureAuth: AuthorizationCodeReceived");
                            return Task.FromResult(0);
                        },
                        MessageReceived = context =>
                        {
                            Trace.TraceInformation("ConfigureAuth: MessageReceived");
                            return Task.FromResult(0);
                        },
                        RedirectToIdentityProvider = context =>
                        {
                            Trace.TraceInformation("ConfigureAuth: RedirectToIdentityProvider - ClientId: {0}", context.ProtocolMessage.ClientId);
                            return Task.FromResult(0);
                        },
                        SecurityTokenReceived = context =>
                        {
                            Trace.TraceInformation("ConfigureAuth: SecurityTokenReceived");
                            return Task.FromResult(0);
                        },
                        SecurityTokenValidated = context =>
                        {
                            Trace.TraceInformation("ConfigureAuth: SecurityTokenValidated");
                            return Task.FromResult(0);
                        }
                    }
                });
        }
    }
}