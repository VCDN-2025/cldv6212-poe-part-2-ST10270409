using System;
using Azure.Core;
using Azure.Identity;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Files.Shares;

public static class ClientFactory
{
    // If a connection string is present, prefer it (local).
    // In Azure, omit this setting and the code will use MSI/AAD via DefaultAzureCredential.
    private static string? ConnStr => Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");

    public static TableServiceClient TableService(string account, TokenCredential cred) =>
        string.IsNullOrWhiteSpace(ConnStr)
            ? new TableServiceClient(new Uri($"https://{account}.table.core.windows.net"), cred)
            : new TableServiceClient(ConnStr);

    public static BlobServiceClient BlobService(string account, TokenCredential cred) =>
        string.IsNullOrWhiteSpace(ConnStr)
            ? new BlobServiceClient(new Uri($"https://{account}.blob.core.windows.net"), cred)
            : new BlobServiceClient(ConnStr);

    public static QueueServiceClient QueueService(string account, TokenCredential cred) =>
        string.IsNullOrWhiteSpace(ConnStr)
            ? new QueueServiceClient(new Uri($"https://{account}.queue.core.windows.net"), cred)
            : new QueueServiceClient(ConnStr);

    public static ShareServiceClient ShareService(string account, TokenCredential cred) =>
        string.IsNullOrWhiteSpace(ConnStr)
            ? new ShareServiceClient(new Uri($"https://{account}.file.core.windows.net"), cred)
            : new ShareServiceClient(ConnStr);
}
