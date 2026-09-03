using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using GHS.Web.Data;
using GHS.Web.Models;

namespace GHS.Web.Services.Core;

/// <summary>
/// 读取上位机的 LoadMaterialBufferPreserve.xml（当前在料快照）。
/// 最小验证版：路径写死，用于验证 GHSWeb 服务账号能否读到上位机共享。
/// XML 结构：<RawMaterials><RawMaterial Position="1" Id="物料号@批次@UID"/> ... </RawMaterials>
/// </summary>
public class BufferMaterialService
{
    private readonly IDbContextFactory<GHSDbContext> _dbFactory;

    public BufferMaterialService(IDbContextFactory<GHSDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    // 最小验证：写死一条已确认可读的 UNC 路径
    private const string TestPath =
        @"\\10.103.32.58\D\ContiPRiME\Lines\RA\01_GHP\Line1\FAM101_AG020_1\LoadMaterialBufferPreserve.xml";

    /// <summary>
    /// 按选定项目读取该项目下所有站位设备的当前在料。
    /// 数据链：project -> material_types(ProjectId) -> material_sub_types.EquipmentId -> equipments
    /// 每个设备用其 BufferXmlPath 读取 LoadMaterialBufferPreserve.xml。
    /// </summary>
    public async Task<(List<BufferMaterialItem> Items, string Error)> ReadByProjectAsync(int projectId)
    {
        var items = new List<BufferMaterialItem>();
        var errors = new List<string>();

        List<Equipment> equipments;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var typeIds = await db.MaterialTypes
                .Where(t => t.ProjectId == projectId)
                .Select(t => t.Id)
                .ToListAsync();
            var eqIds = await db.MaterialSubTypes
                .Where(s => typeIds.Contains(s.MaterialTypeId))
                .Select(s => s.EquipmentId)
                .Distinct()
                .ToListAsync();
            equipments = await db.Equipments
                .Where(e => eqIds.Contains(e.Id))
                .OrderBy(e => e.EquipmentId)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            return (items, $"查询站位设备失败: {ex.Message}");
        }

        if (equipments.Count == 0)
            return (items, "该项目下未找到关联的站位设备。");

        foreach (var eq in equipments)
        {
            if (string.IsNullOrWhiteSpace(eq.BufferXmlPath))
            {
                errors.Add($"{eq.EquipmentId}: 未配置当前料 XML 路径");
                continue;
            }
            try
            {
                if (!File.Exists(eq.BufferXmlPath))
                {
                    errors.Add($"{eq.EquipmentId}: 文件不存在或无访问权限");
                    continue;
                }
                var doc = XDocument.Load(eq.BufferXmlPath);
                foreach (var el in doc.Descendants("RawMaterial"))
                {
                    var pos = el.Attribute("Position")?.Value ?? "";
                    var rawId = el.Attribute("Id")?.Value ?? "";
                    items.Add(new BufferMaterialItem
                    {
                        EquipmentDbId = eq.Id,
                        EquipmentId = eq.EquipmentId,
                        ServerIp = eq.ServerIp,
                        ServerPort = eq.ServerPort,
                        Position = pos,
                        RawId = rawId,
                        MaterialNumber = ExtractMaterialNumber(rawId)
                    });
                }
            }
            catch (Exception ex)
            {
                errors.Add($"{eq.EquipmentId}: 读取/解析失败 {ex.Message}");
            }
        }

        return (items, string.Join("；", errors));
    }

    public Task<(List<BufferMaterialItem> Items, string Error)> ReadCurrentMaterialsAsync()
    {
        var items = new List<BufferMaterialItem>();

        try
        {
            if (!File.Exists(TestPath))
                return Task.FromResult((items, $"文件不存在或无访问权限: {TestPath}"));

            var doc = XDocument.Load(TestPath);
            foreach (var el in doc.Descendants("RawMaterial"))
            {
                var pos = el.Attribute("Position")?.Value ?? "";
                var rawId = el.Attribute("Id")?.Value ?? "";
                items.Add(new BufferMaterialItem
                {
                    Position = pos,
                    RawId = rawId,
                    MaterialNumber = ExtractMaterialNumber(rawId)
                });
            }

            return Task.FromResult((items, ""));
        }
        catch (Exception ex)
        {
            return Task.FromResult((items, $"读取/解析失败: {ex.Message}"));
        }
    }

    /// <summary>
    /// 从 Id (000001514330022100@0700050156@UCSC000121034) 取物料号：
    /// 取 @ 前第一段，并去掉前导零。
    /// </summary>
    private static string ExtractMaterialNumber(string rawId)
    {
        if (string.IsNullOrEmpty(rawId)) return "";
        var first = rawId.Split('@')[0];
        var trimmed = first.TrimStart('0');
        return string.IsNullOrEmpty(trimmed) ? first : trimmed;
    }
}

public class BufferMaterialItem
{
    public int EquipmentDbId { get; set; }
    public string EquipmentId { get; set; } = "";
    public string ServerIp { get; set; } = "";
    public int ServerPort { get; set; }
    public string Position { get; set; } = "";
    public string MaterialNumber { get; set; } = "";
    public string RawId { get; set; } = "";
}
