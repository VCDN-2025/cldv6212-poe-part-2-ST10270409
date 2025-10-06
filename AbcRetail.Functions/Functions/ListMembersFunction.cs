using System.Net;
using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace AbcRetail.Functions;

public class ListMembersFunction
{
    private readonly TableServiceClient _tableSvc;
    private readonly ILogger<ListMembersFunction> _log;

    public ListMembersFunction(TableServiceClient tableSvc, ILogger<ListMembersFunction> log)
    {
        _tableSvc = tableSvc;
        _log = log;
    }

    // GET /api/ListMembers?partition=members&search=wa&take=100
    [Function("ListMembers")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
    {
        var q = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var partition = q["partition"] ?? "members";
        var search = (q["search"] ?? "").Trim();
        var take = int.TryParse(q["take"], out var t) && t > 0 && t <= 500 ? t : 100;

        var table = _tableSvc.GetTableClient(StorageNames.TableName);
        await table.CreateIfNotExistsAsync();

        // Build filter. NOTE: Table Storage doesn't have 'contains', so we do a
        // lexicographic "starts with" using [ge 'term' and lt 'term\uffff'].
        string Esc(string s) => s.Replace("'", "''");
        string prefixHi(string s) => s + "\uffff"; // upper bound

        string filter = $"PartitionKey eq '{Esc(partition)}'";

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lo = Esc(search);
            var hi = Esc(prefixHi(search));
            // Search by Name or Email (case-sensitive at the storage level; we’ll do an extra
            // case-insensitive pass in-memory after retrieval)
            filter += $" and ((Name ge '{lo}' and Name lt '{hi}') or (Email ge '{lo}' and Email lt '{hi}'))";
        }

        var items = new List<object>();
        try
        {
            // Query and collect up to 'take'
            await foreach (var page in table.QueryAsync<TableEntity>(filter: filter, maxPerPage: take).AsPages(pageSizeHint: take))
            {
                foreach (var e in page.Values)
                {
                    items.Add(new
                    {
                        id = e.RowKey,
                        name = e.TryGetValue("Name", out var n) ? n?.ToString() : null,
                        email = e.TryGetValue("Email", out var m) ? m?.ToString() : null,
                        partitionKey = e.PartitionKey,
                        rowKey = e.RowKey,
                        timestamp = e.Timestamp
                    });
                    if (items.Count >= take) break;
                }
                if (items.Count >= take) break;
            }

            // Optional: case-insensitive client-side refine when 'search' provided
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLowerInvariant();
                items = items.Where(x =>
                {
                    dynamic d = x;
                    string? name = d.name;
                    string? email = d.email;
                    return (name != null && name.ToLowerInvariant().Contains(s))
                        || (email != null && email.ToLowerInvariant().Contains(s));
                }).Take(take).ToList();
            }

            var ok = req.CreateResponse(HttpStatusCode.OK);
            await ok.WriteAsJsonAsync(new { ok = true, count = items.Count, items });
            return ok;
        }
        catch (RequestFailedException ex)
        {
            _log.LogError(ex, "ListMembers query failed: {Code} {Msg}", ex.ErrorCode, ex.Message);
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteAsJsonAsync(new { ok = false, azureError = ex.ErrorCode, message = ex.Message });
            return bad;
        }
    }
}
