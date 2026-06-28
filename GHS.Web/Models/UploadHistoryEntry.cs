namespace GHS.Web.Models;

public class UploadHistoryEntry
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? LineName { get; set; }
    public string? ProjectName { get; set; }
    public string? TypeName { get; set; }
    public string? SubTypeName { get; set; }
    public string? EquipmentId { get; set; }
    public string? ScanContent { get; set; }
    public string? CommandSent { get; set; }
    public string? Response { get; set; }
    public bool IsAckSuccess { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
