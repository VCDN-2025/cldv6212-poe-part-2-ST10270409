using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace FitHub.Web.Services
{
    public class FunctionsClient
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _cfg;

        public FunctionsClient(HttpClient http, IConfiguration cfg)
        {
            _http = http;
            _cfg = cfg;
        }

        private string Url(string name) =>
            _cfg[$"AzureFunctions:{name}"] ?? throw new InvalidOperationException($"Missing AzureFunctions:{name} config");

        private HttpRequestMessage Build(string url, object body)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(body)
            };

            // Optional header approach if you store AzureFunctions:Key
            var key = _cfg["AzureFunctions:Key"];
            if (!string.IsNullOrWhiteSpace(key))
                req.Headers.Add("x-functions-key", key);

            return req;
        }

        public async Task<HttpResponseMessage> CreateProductAsync(object body)
            => await _http.SendAsync(Build(Url("Products_Create"), body));

        public async Task<HttpResponseMessage> UploadBlobFromUrlAsync(object body)
            => await _http.SendAsync(Build(Url("Blobs_UploadFromUrl"), body));

        public async Task<HttpResponseMessage> EnqueueAsync(object body)
            => await _http.SendAsync(Build(Url("Queue_Enqueue"), body));

        public async Task<HttpResponseMessage> WriteFileShareAsync(object body)
            => await _http.SendAsync(Build(Url("WriteFileShare"), body));
    }
}
