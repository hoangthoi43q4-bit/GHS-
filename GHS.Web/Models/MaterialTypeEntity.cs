namespace GHS.Web.Models;

public class MaterialTypeEntity
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string ProjectName { get; set; } = string.Empty; // navigation helper
}
