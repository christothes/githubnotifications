using Azure.Core.Pipeline;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;


namespace GitHubNotifications
{
    public class IncludeRequestCredentialsMessageHandler : DelegatingHandler
    {
        private readonly TokenProvider tokenProvider;

        public IncludeRequestCredentialsMessageHandler(TokenProvider tokenProvider)
        {
            InnerHandler = new HttpClientHandler()
            {
                Credentials = CredentialCache.DefaultNetworkCredentials,
                UseDefaultCredentials = true,
                PreAuthenticate = true
            };
            this.tokenProvider = tokenProvider;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // The following SetBrowserRequestCredentials(...) api is not available for Blazor Server Side.
           // request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
            request.Headers.Authorization =new AuthenticationHeaderValue("Bearer", tokenProvider.AccessToken);

            return base.SendAsync(request, cancellationToken);
        }
    }
}