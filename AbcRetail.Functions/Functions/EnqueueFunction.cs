using System.Net;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace AbcRetail.Functions;

public class EnqueueFunction
{
    private readonly QueueServiceClient _queueServiceClient;
    private readonly ILogger<EnqueueFunction> _logger;

    public EnqueueFunction(QueueServiceClient queueServiceClient, ILogger<EnqueueFunction> logger)
    {
        _queueServiceClient = queueServiceClient;
        _logger = logger;
    }

    // POST /api/Enqueue?queue=orders
    // Body: raw text or JSON (it will be enqueued as-is)
    [Function("Enqueue")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        try
        {
            var q = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var queueName = q["queue"] ?? StorageNames.QueueName;

            var queue = _queueServiceClient.GetQueueClient(queueName);
            await queue.CreateIfNotExistsAsync();

            string content;
            using (var sr = new StreamReader(req.Body))
                content = await sr.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(content))
                content = "{}"; // avoid empty message

            await queue.SendMessageAsync(content);

            var resp = req.CreateResponse(HttpStatusCode.OK);
            await resp.WriteAsJsonAsync(new { ok = true, queue = queueName, length = content.Length });
            return resp;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Enqueue failed.");
            var resp = req.CreateResponse(HttpStatusCode.InternalServerError);
            await resp.WriteAsJsonAsync(new { ok = false, message = ex.Message });
            return resp;
        }
    }
}
