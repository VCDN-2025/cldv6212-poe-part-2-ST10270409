// Models/BookingEntity.cs
using Azure;
using Azure.Data.Tables;

namespace FitHub.Web.Models
{
    public class BookingEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = "";           // memberId
        public string RowKey { get; set; } = Guid.NewGuid().ToString(); // bookingId

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string ClassId { get; set; } = "";
        public DateTime BookingDate { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Confirmed";
    }
}
