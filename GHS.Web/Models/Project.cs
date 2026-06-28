using System.ComponentModel.DataAnnotations.Schema;

namespace GHS.Web.Models;

public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int LineId { get; set; }

    [NotMapped]
    public string LineName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
