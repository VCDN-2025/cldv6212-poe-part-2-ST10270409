using System.Net;
using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace AbcRetail.Functions;

public class WriteFileShareFunction
{
    private readonly ShareServiceClient _shareService;
    private readonly ILogger<WriteFileShareFunction> _logger;

    public WriteFileShareFunction(ShareServiceClient shareService, ILogger<WriteFileShareFunction> logger)
    {
        _shareService = shareService;
        _logger = logger;
    }

    // POST /api/WriteFileShare?share=docs&dir=invoices&filename=test.txt
    // Body: raw binary
    [Function("WriteFileShare")]
    public async Task<HttpResponseData> WriteFileShare(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        try
        {
            var q = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var shareName = q["share"] ?? StorageNames.FileShare;
            var dirPath = (q["dir"] ?? "").Trim().Trim('/').Replace("\\", "/");
            var fileName = q["filename"];

            if (string.IsNullOrWhiteSpace(fileName))
                return await Bad(req, "Provide ?filename= and send the file as binary in the request body.");

            // Ensure share & directories
            var share = _shareService.GetShareClient(shareName);
            await share.CreateIfNotExistsAsync();

            var dir = share.GetRootDirectoryClient();
            if (!string.IsNullOrEmpty(dirPath))
            {
                foreach (var part in dirPath.Split('/', StringSplitOptions.RemoveEmptyEntries))
                {
                    dir = dir.GetSubdirectoryClient(part);
                    await dir.CreateIfNotExistsAsync();
                }
            }

            // Read body to know final length
            using var mem = new MemoryStream();
            await req.Body.CopyToAsync(mem);
            var size = mem.Length;

            var fileClient = dir.GetFileClient(fileName);

            // Safe overwrite & upload
            await fileClient.DeleteIfExistsAsync();
            await fileClient.CreateAsync(size);
            mem.Position = 0;
            await fileClient.UploadRangeAsync(new HttpRange(0, size), mem);

            // Optional: set content type
            var contentType = GuessContentType(fileName);
            if (!string.IsNullOrEmpty(contentType))
            {
                await fileClient.SetHttpHeadersAsync(
                    new ShareFileSetHttpHeadersOptions
                    {
                        HttpHeaders = new ShareFileHttpHeaders
                        {
                            ContentType = contentType
                        }
                    });
            }

            return await Ok(req, new
            {
                ok = true,
                share = shareName,
                directory = dirPath,
                fileName,
                url = fileClient.Uri.ToString()
            });
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure Files error: {Code} {Msg}", ex.ErrorCode, ex.Message);
            return await Fail(req, HttpStatusCode.BadRequest, new { ok = false, azureError = ex.ErrorCode, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in WriteFileShare");
            return await Fail(req, HttpStatusCode.InternalServerError, new { ok = false, message = ex.Message });
        }
    }

    // GET /api/ListFileShare?share=docs&dir=invoices
    [Function("ListFileShare")]
    public async Task<HttpResponseData> ListFileShare(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
    {
        var q = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var shareName = q["share"] ?? StorageNames.FileShare;
        var dirPath = (q["dir"] ?? "").Trim().Trim('/').Replace("\\", "/");

        var share = _shareService.GetShareClient(shareName);
        var dir = share.GetRootDirectoryClient();
        if (!string.IsNullOrEmpty(dirPath))
        {
            foreach (var part in dirPath.Split('/', StringSplitOptions.RemoveEmptyEntries))
                dir = dir.GetSubdirectoryClient(part);
        }

        var items = new List<object>();
        await foreach (var item in dir.GetFilesAndDirectoriesAsync())
        {
            items.Add(new
            {
                name = item.Name,
                isDirectory = item.IsDirectory,
                url = item.IsDirectory ? null : dir.GetFileClient(item.Name).Uri.ToString()
            });
        }

        return await Ok(req, new { ok = true, share = shareName, directory = dirPath, items });
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
    private static string GuessContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".txt" => "text/plain",
            ".csv" => "text/csv",
            ".json" => "application/json",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            _ => "application/octet-stream"
        };
    }
}
