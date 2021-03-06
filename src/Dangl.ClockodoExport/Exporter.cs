﻿using Microsoft.Extensions.DependencyInjection;
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
            var customersResponse = await httpClient.GetStringAsync($"{CLOCKODO_API_BASE_URL}/customers");
            var jsonObjectCustomers = JObject.Parse(customersResponse);
            _clockodoDataByModelName.Add("customers", new List<JObject> { jsonObjectCustomers });

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

            // Tasks
            var tasksResponse = await httpClient.GetStringAsync($"{CLOCKODO_API_BASE_URL}/tasks");
            var jsonObjectTasks = JObject.Parse(tasksResponse);
            _clockodoDataByModelName.Add("tasks", new List<JObject> { jsonObjectTasks });

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
            var startDate = "2020-01-01 00:00:00";
            var endDate = $"{DateTime.Now.Year + 5:0000}-12-31 23:59:59";
            var entriesUrl = $"{CLOCKODO_API_BASE_URL}/entries?time_since={startDate}&time_until={endDate}";

            var responses = new List<JObject>();

            var currentPage = 1;
            var hasMoreData = true;
            while (hasMoreData)
            {
                var pagedUrl = $"{entriesUrl}&page={currentPage++}";
                var response = await _getHttpClient().GetStringAsync(pagedUrl);
                var jObject = JObject.Parse(response);
                responses.Add(jObject);

                var returnedPageCount = jObject["paging"]["count_pages"].ToObject<int>();
                hasMoreData = returnedPageCount >= currentPage;
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
                });
            return serviceCollection.BuildServiceProvider();
        }
    }
}
