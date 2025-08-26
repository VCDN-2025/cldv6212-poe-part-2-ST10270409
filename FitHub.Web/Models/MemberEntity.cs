using Azure;
using Azure.Data.Tables;

namespace FitHub.Web.Models
{
    public class MemberEntity : ITableEntity
    {
        // Partition/Row required by Table Storage
        public string PartitionKey { get; set; } = "Member";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();

        // Standard audit fields (Table service fills ETag/Timestamp)
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Your data
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public DateTime JoinDate { get; set; } = DateTime.UtcNow;
    }
}
