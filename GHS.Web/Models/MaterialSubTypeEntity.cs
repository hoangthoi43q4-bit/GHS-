namespace GHS.Web.Models;

public class MaterialSubTypeEntity
{
    public int Id { get; set; }
    public int MaterialTypeId { get; set; }
    public string SubTypeName { get; set; } = string.Empty;
    public int EquipmentId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation helpers
    public string TypeName { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string AssociatedMachineName { get; set; } = string.Empty;
}
