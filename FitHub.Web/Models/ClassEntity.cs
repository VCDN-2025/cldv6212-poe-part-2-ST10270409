using Azure;
using Azure.Data.Tables;

namespace FitHub.Web.Models
{
    public class ClassEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = "Class";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string TrainerName { get; set; } = "";
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = ""; // Blob URL
    }
}
