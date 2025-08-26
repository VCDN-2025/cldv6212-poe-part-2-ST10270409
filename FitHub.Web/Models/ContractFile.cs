namespace FitHub.Web.Models
{
    public class ContractFile
    {
        public string Name { get; set; } = "";
        public long Length { get; set; }
        public DateTimeOffset? LastModified { get; set; }
    }
}
