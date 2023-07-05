using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Dangl.ClockodoExport
{
    public class Exporter
    {
        private readonly ApiExportOptions _apiExportOptions;
        private readonly Dictionary<string, List<JObject>> _clockodoDataByModelName = new Dictionary<string, List<JObject>>();
        private string _basePath;
        private Func<HttpClient> _getHttpClient;
        private const string CLOCKODO_API_BASE_URL = "https://my.clockodo.com/api";

        public Exporter(ApiExportOptions apiExportOptions)
        {
            _apiExportOptions = apiExportOptions;
            var clientFactory = GetHttpClientServiceProvider(apiExportOptions);
            _getHttpClient = () => clientFactory.GetRequiredService<IHttpClientFactory>().CreateClient("Clockodo");
            SetExportPaths();
        }

        public async Task ExportClockodoDataAndWriteToDiskAsync()
        {
            // Customers
            var httpClient = _getHttpClient();

            var customersUrl = $"{CLOCKODO_API_BASE_URL}/v2/customers";
            var jsonObjectCustomers = await GetAllElementsFromPagedEndpointAsync(customersUrl);
            _clockodoDataByModelName.Add("customers", jsonObjectCustomers);

            // Services
            var servicesResponse = await httpClient.GetStringAsync($"{CLOCKODO_API_BASE_URL}/services");
            var jsonObjectServices = JObject.Parse(servicesResponse);
            _clockodoDataByModelName.Add("services", new List<JObject> { jsonObjectServices });

            // Users
            var usersResponse = await httpClient.GetStringAsync($"{CLOCKODO_API_BASE_URL}/users");
            var jsonObjectUsers = JObject.Parse(usersResponse);
            _clockodoDataByModelName.Add("users", new List<JObject> { jsonObjectUsers });

            // Entries
            var entriesResponse = await GetEntriesResponse();
            _clockodoDataByModelName.Add("entries", entriesResponse);

            // Projects
            var projectsUrl = $"{CLOCKODO_API_BASE_URL}/v2/projects";
            var jsonObjectProjects = await GetAllElementsFromPagedEndpointAsync(projectsUrl);
            _clockodoDataByModelName.Add("projects", jsonObjectProjects);

            foreach (var apiResult in _clockodoDataByModelName)
            {
                if (apiResult.Value.Count > 1)
                {
                    var currentResult = 1;
                    foreach (var resultValue in apiResult.Value)
                    {
                        var jsonFilePath = Path.Combine(_basePath, $"{apiResult.Key}_{currentResult++}.json");
                        using (var fs = File.CreateText(jsonFilePath))
                        {
                            var jsonResult = resultValue.ToString(Formatting.Indented);
                            await fs.WriteAsync(jsonResult).ConfigureAwait(false);
                        }
                    }
                }
                else
                {
                    var jsonFilePath = Path.Combine(_basePath, apiResult.Key + ".json");
                    using (var fs = File.CreateText(jsonFilePath))
                    {
                        var jsonResult = apiResult.Value.Single().ToString(Formatting.Indented);
                        await fs.WriteAsync(jsonResult).ConfigureAwait(false);
                    }
                }
            }
        }

        private async Task<List<JObject>> GetEntriesResponse()
        {
            var startDate = "2020-01-01T00:00:00Z";
            var endDate = $"{DateTime.Now.Year + 5:0000}-12-31T23:59:59Z";
            var entriesUrl = $"{CLOCKODO_API_BASE_URL}/v2/entries?time_since={startDate}&time_until={endDate}";
            var responses = await GetAllElementsFromPagedEndpointAsync(entriesUrl, true);
            return responses;
        }

        private async Task<List<JObject>> GetAllElementsFromPagedEndpointAsync(string endpoint,
            bool appendPagingAsFirstQueryParameter = false)
        {
            var responses = new List<JObject>();

            var currentPage = 1;
            var hasMoreData = true;
            while (hasMoreData)
            {
                var pagedUrl = appendPagingAsFirstQueryParameter
                    ? $"{endpoint}&page={currentPage++}"
                    : $"{endpoint}?page={currentPage++}";
                try
                {
                    var response = await _getHttpClient().GetStringAsync(pagedUrl);
                    var jObject = JObject.Parse(response);
                    responses.Add(jObject);
                    var returnedPageCount = jObject["paging"]["count_pages"].ToObject<int>();
                    hasMoreData = returnedPageCount >= currentPage;
                }
                catch (HttpRequestException)
                {
                    var rawResponse = await _getHttpClient().GetAsync(pagedUrl).ConfigureAwait(false);
                    var responseString = await rawResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                    Console.WriteLine($"Encountered a problem while fetching {pagedUrl}{Environment.NewLine}{responseString}");
                    throw;
                }
            }

            return responses;
        }

        private void SetExportPaths()
        {
            _basePath = string.IsNullOrWhiteSpace(_apiExportOptions.ExportBaseFolder)
                ? string.Empty
                : _apiExportOptions.ExportBaseFolder;
            _basePath = Path.Combine(_basePath, $"{DateTime.Now:yyyy-MM-dd HH-mm}");
            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
            }
        }

        public static IServiceProvider GetHttpClientServiceProvider(ApiExportOptions options)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient("Clockodo", httpClientOptions =>
                {
                    httpClientOptions.DefaultRequestHeaders.Add("User-Agent", "Dangl IT GmbH Clockodo Export www.dangl-it.com");
                    httpClientOptions.DefaultRequestHeaders.Add("X-ClockodoApiUser", options.UserEmail);
                    httpClientOptions.DefaultRequestHeaders.Add("X-ClockodoApiKey", options.ClockodoApiToken);
                    httpClientOptions.DefaultRequestHeaders.Add("X-Clockodo-External-Application", "Dangl IT GmbH Clockodo Export;info@dangl-it.com");
                });
            return serviceCollection.BuildServiceProvider();
        }
    }
}
