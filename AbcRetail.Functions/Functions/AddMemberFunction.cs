using System.Net;
using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace AbcRetail.Functions;

public class AddMemberFunction
{
    private readonly TableServiceClient _tableServiceClient;
    private readonly ILogger<AddMemberFunction> _logger;

    public AddMemberFunction(TableServiceClient tableServiceClient, ILogger<AddMemberFunction> logger)
    {
        _tableServiceClient = tableServiceClient;
        _logger = logger;
    }

    // POST /api/AddMember
    // Body (application/json), e.g.:
    // { "partitionKey":"members", "rowKey":"123", "name":"Wabo", "email":"wabo@example.com" }
    [Function("AddMember")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        try
        {
            var table = _tableServiceClient.GetTableClient(StorageNames.TableName);
            await table.CreateIfNotExistsAsync();

            using var doc = await JsonDocument.ParseAsync(req.Body);
            var root = doc.RootElement;

            string partitionKey = root.TryGetProperty("partitionKey", out var pkEl) ? pkEl.GetString() ?? "members" : "members";
            string rowKey = root.TryGetProperty("rowKey", out var rkEl) ? rkEl.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString();

            var entity = new TableEntity(partitionKey, rowKey);

            if (root.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in root.EnumerateObject())
                {
                    var name = prop.Name;
                    if (string.Equals(name, "partitionKey", StringComparison.OrdinalIgnoreCase)) continue;
                    if (string.Equals(name, "rowKey", StringComparison.OrdinalIgnoreCase)) continue;

                    entity[name] = prop.Value.ValueKind switch
                    {
                        JsonValueKind.String => prop.Value.GetString(),
                        JsonValueKind.Number => prop.Value.TryGetInt64(out var l) ? l : (object?)prop.Value.GetDouble(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        _ => prop.Value.ToString()
                    };
                }
            }

            await table.UpsertEntityAsync(entity, TableUpdateMode.Replace);

            var resp = req.CreateResponse(HttpStatusCode.OK);
            await resp.WriteAsJsonAsync(new { ok = true, table = StorageNames.TableName, partitionKey, rowKey });
            return resp;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Table upsert failed: {Code} {Msg}", ex.ErrorCode, ex.Message);
            var resp = req.CreateResponse(HttpStatusCode.BadRequest);
            await resp.WriteAsJsonAsync(new { ok = false, azureError = ex.ErrorCode, message = ex.Message });
            return resp;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in AddMember");
            var resp = req.CreateResponse(HttpStatusCode.InternalServerError);
            await resp.WriteAsJsonAsync(new { ok = false, message = ex.Message });
            return resp;
        }
    }
}
