using Client.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Client.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ICodeFlowClient _client;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public HomeController(ILogger<HomeController> logger, 
            ICodeFlowClient client, 
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _client = client;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id 
                ?? HttpContext.TraceIdentifier });
        }

        public IActionResult InvokeApi() 
        {
            var redirectUri = Url.Action("InvokeApiCallback", "Home", null, "https");

            var endpoint = _client.GetAuthorizationEndpoint(redirectUri, 
                _configuration["Auth0:Audience"],
                _configuration["Auth0:Scope"]);

            return Redirect(endpoint);
        }

        public async Task<IActionResult> InvokeApiCallback(string code, string error, string error_description)
        {
            if(!string.IsNullOrWhiteSpace(error))
            {
                throw new Exception($"{error}:{error_description}");
            }

            var redirectUri = Url.Action("InvokeApiCallback", "Home", null, "https");

            var accessToken = await _client.GetAccessTokenAsync(code, redirectUri);

            var client = _httpClientFactory.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Get, _configuration["WeatherForecastApiUrl"]);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.SendAsync(request);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            var weatherForecast = JsonConvert.DeserializeObject<IEnumerable<WeatherForecast>>(json);

            return View("WeatherForecast", weatherForecast);
        }
    }
}
