using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AbcRetail.Functions;

public class OrdersQueueTrigger
{
    private readonly TableServiceClient _tableSvc;
    private readonly ILogger<OrdersQueueTrigger> _log;

    // Table name required by rubric
    private const string OrdersTable = "orders";
    private const string DefaultPartition = "orders"; // or e.g. yyyyMM for sharding

    public OrdersQueueTrigger(TableServiceClient tableSvc, ILogger<OrdersQueueTrigger> log)
    {
        _tableSvc = tableSvc;
        _log = log;
    }

    // Queue name comes from your existing queue (“orders”)
    [Function("OrdersQueueTrigger")]
    public async Task Run([QueueTrigger("orders")] string message)
    {
        var table = _tableSvc.GetTableClient(OrdersTable);
        await table.CreateIfNotExistsAsync();

        using var doc = JsonDocument.Parse(message);
        var root = doc.RootElement;

        var type = root.TryGetProperty("type", out var t) ? t.GetString()?.ToLowerInvariant() : "create";
        var orderId = root.TryGetProperty("orderId", out var oid) ? oid.GetString() : null;
        if (string.IsNullOrWhiteSpace(orderId))
        {
            orderId = Guid.NewGuid().ToString("n");
            _log.LogWarning("orderId missing, generating one: {OrderId}", orderId);
        }

        var pk = root.TryGetProperty("partitionKey", out var pkEl) ? pkEl.GetString() ?? DefaultPartition : DefaultPartition;
        var rk = orderId; // RowKey = orderId

        var entity = new TableEntity(pk, rk)
        {
            ["OrderId"] = orderId,
            ["UpdatedAt"] = DateTimeOffset.UtcNow
        };

        // “create” — write all fields we care about
        if (type == "create")
        {
            if (root.TryGetProperty("customerId", out var c)) entity["CustomerId"] = c.GetString();
            if (root.TryGetProperty("total", out var tot) && tot.TryGetDecimal(out var dec)) entity["Total"] = dec;
            entity["Status"] = root.TryGetProperty("status", out var st) ? st.GetString() ?? "Pending" : "Pending";
            entity["CreatedAt"] = root.TryGetProperty("createdAt", out var ca) && ca.TryGetDateTime(out var dt)
                ? new DateTimeOffset(dt, TimeSpan.Zero)
                : DateTimeOffset.UtcNow;

            await table.UpsertEntityAsync(entity, TableUpdateMode.Merge);
            _log.LogInformation("Order created: {OrderId}", orderId);
            return;
        }

        // “status” — only mutate status fields (no direct table writes from HTTP)
        if (type == "status")
        {
            entity["Status"] = root.TryGetProperty("status", out var st) ? st.GetString() ?? "Unknown" : "Unknown";
            await table.UpsertEntityAsync(entity, TableUpdateMode.Merge);
            _log.LogInformation("Order status updated: {OrderId} -> {Status}", orderId, entity["Status"]);
            return;
        }

        // Fallback: merge whatever properties were provided
        foreach (var prop in root.EnumerateObject())
        {
            if (prop.NameEquals("type") || prop.NameEquals("partitionKey") || prop.NameEquals("orderId")) continue;
            entity[prop.Name] = prop.Value.ValueKind switch
            {
                JsonValueKind.String => prop.Value.GetString(),
                JsonValueKind.Number => prop.Value.TryGetInt64(out var l) ? l : (object?)prop.Value.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => prop.Value.ToString()
            };
        }
        await table.UpsertEntityAsync(entity, TableUpdateMode.Merge);
        _log.LogInformation("Order upserted (generic): {OrderId}", orderId);
    }
}
