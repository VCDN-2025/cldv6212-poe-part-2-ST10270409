using System.Net;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace AbcRetail.Functions;

public class UploadBlobFunction
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<UploadBlobFunction> _logger;

    public UploadBlobFunction(BlobServiceClient blobServiceClient, ILogger<UploadBlobFunction> logger)
    {
        _blobServiceClient = blobServiceClient;
        _logger = logger;
    }

    // POST /api/UploadBlob?container=images&filename=pic.jpg
    [Function("UploadBlob")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        try
        {
            var q = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var containerName = q["container"] ?? StorageNames.BlobContainer;
            var fileName = q["filename"];

            if (string.IsNullOrWhiteSpace(fileName))
                return await Bad(req, "Provide ?filename= and send the file as binary in the request body.");

            var container = _blobServiceClient.GetBlobContainerClient(containerName);
            await container.CreateIfNotExistsAsync(PublicAccessType.None);

            using var mem = new MemoryStream();
            await req.Body.CopyToAsync(mem);
            mem.Position = 0;

            var blob = container.GetBlobClient(fileName);
            await blob.UploadAsync(mem, overwrite: true);

            // Set Content-Type based on header if present
            string contentType = req.Headers.TryGetValues("Content-Type", out var vals)
                ? vals.FirstOrDefault() ?? "application/octet-stream"
                : "application/octet-stream";
            await blob.SetHttpHeadersAsync(new BlobHttpHeaders { ContentType = contentType });

            return await Ok(req, new { ok = true, container = containerName, blobName = fileName, url = blob.Uri.ToString() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Blob upload failed.");
            return await Fail(req, HttpStatusCode.InternalServerError, new { ok = false, message = ex.Message });
        }
    }

    // helpers
    private static async Task<HttpResponseData> Ok(HttpRequestData req, object obj)
    {
        var resp = req.CreateResponse(HttpStatusCode.OK);
        await resp.WriteAsJsonAsync(obj);
        return resp;
    }
    private static async Task<HttpResponseData> Bad(HttpRequestData req, string message)
    {
        var resp = req.CreateResponse(HttpStatusCode.BadRequest);
        await resp.WriteAsJsonAsync(new { ok = false, message });
        return resp;
    }
    private static async Task<HttpResponseData> Fail(HttpRequestData req, HttpStatusCode code, object obj)
    {
        var resp = req.CreateResponse(code);
        await resp.WriteAsJsonAsync(obj);
        return resp;
    }
}
