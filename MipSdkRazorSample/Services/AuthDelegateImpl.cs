using Microsoft.Identity.Client;
using Microsoft.InformationProtection;
using Microsoft.InformationProtection.Exceptions;
using System.Security.Policy;

namespace MipSdkRazorSample.Services
{
    public class AuthDelegateImpl : IAuthDelegate
    {
        private readonly IConfiguration _configuration;
        private readonly string _redirectUri;
        private readonly string _tenantId;
        private readonly string _clientId;
        private readonly string _secret;

        public AuthDelegateImpl(IConfiguration configuration)
        {
            _configuration = configuration;
            
            _redirectUri = "https://localhost:7143" + _configuration.GetSection("AzureAd").GetValue<string>("CallbackPath");
            _tenantId = _configuration.GetSection("AzureAd").GetValue<string>("TenantId");
            _clientId = _configuration.GetSection("AzureAd").GetValue<string>("ClientId");
            _secret = _configuration["App:MipApiKey"];
        }

        public string AcquireToken(Identity identity, string authority, string resource, string claims)
        {
            IConfidentialClientApplication app;
            AuthenticationResult authResult;


            if (authority.ToLower().Contains("common"))
            {
                var authorityUri = new Uri(authority);
                authority = String.Format("https://{0}/{1}", authorityUri.Host, _tenantId);
            }

            Console.WriteLine("Performing client secret based auth.");
            app = ConfidentialClientApplicationBuilder.Create(_clientId)
            .WithClientSecret(_secret)
            .WithRedirectUri(_redirectUri)
            .Build();

            string[] scopes = new string[] { resource[resource.Length - 1].Equals('/') ? $"{resource}.default" : $"{resource}/.default" };

            authResult = app.AcquireTokenForClient(scopes)
                .WithAuthority(authority)
                .ExecuteAsync()
                .GetAwaiter()
                .GetResult();

            // Return the token. The token is sent to the resource.
            return authResult.AccessToken;
        }
    }
}
