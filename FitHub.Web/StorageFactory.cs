// StorageFactory.cs
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Data.Tables;
using Azure.Storage.Files.Shares;

namespace FitHub.Web
{
    public class StorageFactory
    {
        private readonly string _conn;
        public StorageFactory(IConfiguration cfg)
        {
            _conn = cfg.GetConnectionString("AzureStorage")
                    ?? cfg["AzureStorage"]
                    ?? throw new Exception("AzureStorage connection string not found.");
        }

        // Blobs
        public BlobContainerClient Blob(string container)
        {
            var c = new BlobContainerClient(_conn, container);
            c.CreateIfNotExists();
            return c;
        }

        // Tables
        public TableClient Table(string table)
        {
            var svc = new TableServiceClient(_conn);
            var t = svc.GetTableClient(table);
            t.CreateIfNotExists();
            return t;
        }

        // Queues
        public QueueClient Queue(string name)
        {
            var q = new QueueClient(_conn, name);
            q.CreateIfNotExists();
            return q;
        }

        // File Share  👇
        public ShareClient Share(string name)
        {
            var s = new ShareClient(_conn, name);
            s.CreateIfNotExists();
            return s;
        }
    }
}
