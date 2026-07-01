namespace GHS.Web.Services.Core;

/// <summary>
/// Matches a material sub-type name within the input and returns the corresponding type name.
/// Migrated from GHPHandShake.Services.MaterialClassifier.
/// </summary>
public class MaterialClassifier
{
    /// <summary>
    /// Find the material type (Pos) that contains the given sub-type name.
    /// </summary>
    public string? GetTypeBySubTypeMatch(string input, List<MaterialTypeInfo> config)
    {
        if (string.IsNullOrWhiteSpace(input) || config == null)
        {
            return null;
        }

        foreach (var type in config)
        {
            foreach (var sub in type.SubTypes)
            {
                if (!string.IsNullOrEmpty(sub.SubTypeName) && input.Contains(sub.SubTypeName))
                {
                    return type.TypeName;
                }
            }
        }

        return null;
    }
}

/// <summary>
/// Lightweight material type info for in-memory matching (avoids EF entity overhead).
/// </summary>
public class MaterialTypeInfo
{
    public int ProjectId { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public List<MaterialSubTypeInfo> SubTypes { get; set; } = new();
}

public class MaterialSubTypeInfo
{
    public string SubTypeName { get; set; } = string.Empty;
    public string AssociatedMachineName { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public int EquipmentId { get; set; }
}
