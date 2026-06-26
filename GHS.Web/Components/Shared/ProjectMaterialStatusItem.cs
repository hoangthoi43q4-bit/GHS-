namespace GHS.Web.Components.Shared;

/// <summary>
/// View model for material upload status display in the Send page grid.
/// </summary>
public class ProjectMaterialStatusItem
{
    public string TypeName { get; set; } = string.Empty;
    public string SubTypeName { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public string UploadStatus { get; set; } = "未上传";
}
