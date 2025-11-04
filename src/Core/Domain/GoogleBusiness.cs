namespace GoogleConnectorService.Core.Domain;

public class GoogleBusiness
{
    public string BusinessId { get; set; } = string.Empty; // Google Business Profile Location ID
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public Guid? ProfileBusinessId { get; set; } // Link to Business entity in profile service
    public DateTime? LastSyncDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}



