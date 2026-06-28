using System.ComponentModel.DataAnnotations.Schema;

namespace GHS.Web.Models;

public class MaterialSubTypeEntity
{
    public int Id { get; set; }
    public int MaterialTypeId { get; set; }
    public string SubTypeName { get; set; } = string.Empty;
    public int EquipmentId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [NotMapped] public string TypeName { get; set; } = string.Empty;
    [NotMapped] public string ProjectName { get; set; } = string.Empty;
    [NotMapped] public string AssociatedMachineName { get; set; } = string.Empty;
}
