using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Files.Shares;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        string Fallback() => Environment.GetEnvironmentVariable("AzureWebJobsStorage")!;

        var blobConn = Environment.GetEnvironmentVariable("BlobStorageConnection") ?? Fallback();
        var tableConn = Environment.GetEnvironmentVariable("TableStorageConnection") ?? Fallback();
        var queueConn = Environment.GetEnvironmentVariable("QueueStorageConnection") ?? Fallback();
        var shareConn = Environment.GetEnvironmentVariable("FileShareConnection") ?? Fallback();

        services.AddSingleton(new BlobServiceClient(blobConn));
        services.AddSingleton(new TableServiceClient(tableConn));
        services.AddSingleton(new QueueServiceClient(queueConn));
        services.AddSingleton(new ShareServiceClient(shareConn));

        services.AddHttpClient(); // optional
    })
    .Build();

host.Run();
