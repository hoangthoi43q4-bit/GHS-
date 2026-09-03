namespace GHS.Web.Models;

public class Equipment
{
    public int Id { get; set; }
    public string EquipmentId { get; set; } = string.Empty;
    public string ServerIp { get; set; } = string.Empty;
    public int ServerPort { get; set; } = 2001;
    public string? BufferXmlPath { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
