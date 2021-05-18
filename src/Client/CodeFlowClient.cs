using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public interface ICodeFlowClient
    {
        string GetAuthorizationEndpoint(string redirectUri, string audience, string scope);
        Task<string> GetAccessTokenAsync(string code, string redirectUri);
    }

    public class CodeFlowClient : ICodeFlowClient
    {
        private readonly string _auth0Domain;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly IHttpClientFactory _httpClientFactory;

        public CodeFlowClient(IConfiguration configuration, 
            IHttpClientFactory httpClientFactory)
        {
            _auth0Domain = configuration["Auth0:Domain"];
            _clientId = configuration["Auth0:ClientId"];
            _clientSecret = configuration["Auth0:ClientSecret"];
            _httpClientFactory = httpClientFactory;
        }

        public string GetAuthorizationEndpoint(string redirectUri, string audience, string scope)
        {
            return $"https://{_auth0Domain}/authorize?response_type=code" + 
                $"&client_id={_clientId}&scope={scope}&redirect_uri={redirectUri}&audience={audience}";
        }

        public async Task<string> GetAccessTokenAsync(string code, string redirectUri)
        {
            var client = _httpClientFactory.CreateClient();

            var basicAuth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_clientId}:{_clientSecret}"));

            var body = new Dictionary<string, string>();
            body.Add("grant_type", "authorization_code");
            body.Add("redirect_uri", redirectUri);
            body.Add("code", code);

            var request = new HttpRequestMessage(HttpMethod.Post, $"https://{_auth0Domain}/oauth/token");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", basicAuth);
            request.Content = new FormUrlEncodedContent(body);

            var response = await client.SendAsync(request);

            if(response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                var error = response.Content.ReadAsStringAsync();
                throw new Exception("Access token could not be exchanged. " + error);
            }

            var rawJson = await response.Content.ReadAsStringAsync();

            dynamic json = JObject.Parse(rawJson);

            var accessToken = (string)json.access_token;

            return accessToken;
        }
    }
}
