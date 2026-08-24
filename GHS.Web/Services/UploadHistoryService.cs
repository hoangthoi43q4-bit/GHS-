using Microsoft.EntityFrameworkCore;
using GHS.Web.Data;
using GHS.Web.Models;

namespace GHS.Web.Services;

public class UploadHistoryService
{
    private readonly IDbContextFactory<GHSDbContext> _dbFactory;

    public UploadHistoryService(IDbContextFactory<GHSDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task AddEntryAsync(UploadHistoryEntry entry)
    {
        // 清洗字符串字段中的空字节(0x00)：PostgreSQL text 不允许存 0x00，否则抛 22021 异常
        static string? Clean(string? s) => s?.Replace("\0", "");
        entry.ScanContent = Clean(entry.ScanContent);
        entry.CommandSent = Clean(entry.CommandSent);
        entry.Response = Clean(entry.Response);
        entry.LineName = Clean(entry.LineName);
        entry.ProjectName = Clean(entry.ProjectName);
        entry.TypeName = Clean(entry.TypeName);
        entry.SubTypeName = Clean(entry.SubTypeName);
        entry.EquipmentId = Clean(entry.EquipmentId);

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            entry.CreatedAt = DateTime.UtcNow;
            db.UploadHistory.Add(entry);
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // 写历史失败不得崩掉页面（避免终止 Blazor circuit），仅记录
            Console.WriteLine($"[UploadHistory] 写入失败: {ex.Message}");
        }
    }

    public async Task<List<UploadHistoryGroupItem>> GetGroupedHistoryAsync(string? projectFilter)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var query = db.UploadHistory.AsQueryable();

        if (!string.IsNullOrWhiteSpace(projectFilter))
        {
            query = query.Where(e => e.ProjectName == projectFilter);
        }

        var entries = await query.OrderByDescending(e => e.Timestamp).ToListAsync();

        return entries
            .GroupBy(e => new { Project = e.ProjectName ?? "", Type = e.TypeName ?? "", Sub = e.SubTypeName ?? "" })
            .Select(g =>
            {
                var ordered = g.OrderBy(x => x.Timestamp).ToList();
                var lastSuccess = ordered.Where(x => x.IsAckSuccess && !string.IsNullOrWhiteSpace(x.ScanContent)).LastOrDefault();
                var last = ordered.Last();

                return new UploadHistoryGroupItem
                {
                    ProjectName = g.Key.Project,
                    TypeName = g.Key.Type,
                    SubTypeName = g.Key.Sub,
                    MachineName = last.EquipmentId ?? "",
                    LatestScanContent = lastSuccess?.ScanContent ?? "",
                    LatestScanPreview = string.IsNullOrEmpty(lastSuccess?.ScanContent)
                        ? "（暂无成功上料）"
                        : TruncatePreview(lastSuccess.ScanContent),
                    UploadCount = ordered.Count(x => x.IsAckSuccess),
                    LastTime = last.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")
                };
            })
            .OrderBy(x => x.ProjectName).ThenBy(x => x.TypeName).ThenBy(x => x.SubTypeName)
            .ToList();
    }

    public async Task<List<UploadHistoryDetailItem>> GetDetailHistoryAsync(
        string projectName, string typeName, string subTypeName)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entries = await db.UploadHistory
            .Where(e => e.ProjectName == projectName && e.TypeName == typeName && e.SubTypeName == subTypeName)
            .OrderBy(e => e.Timestamp)
            .ToListAsync();

        return entries.Select((e, i) => new UploadHistoryDetailItem
        {
            Index = i + 1,
            Timestamp = e.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
            ScanContent = e.ScanContent ?? "",
            ResultText = e.IsAckSuccess ? "ACK成功" : "未成功",
            IsAckSuccess = e.IsAckSuccess
        }).ToList();
    }

    public async Task<List<string>> GetProjectNamesAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.UploadHistory
            .Where(e => e.ProjectName != null)
            .Select(e => e.ProjectName!)
            .Distinct()
            .OrderBy(p => p)
            .ToListAsync();
    }

    private static string TruncatePreview(string text, int maxLength = 48)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;
        return text[..maxLength] + "...";
    }
}

public class UploadHistoryGroupItem
{
    public string ProjectName { get; set; } = "";
    public string TypeName { get; set; } = "";
    public string SubTypeName { get; set; } = "";
    public string MachineName { get; set; } = "";
    public string LatestScanContent { get; set; } = "";
    public string LatestScanPreview { get; set; } = "";
    public int UploadCount { get; set; }
    public string LastTime { get; set; } = "";
}

public class UploadHistoryDetailItem
{
    public int Index { get; set; }
    public string Timestamp { get; set; } = "";
    public string ScanContent { get; set; } = "";
    public string ResultText { get; set; } = "";
    public bool IsAckSuccess { get; set; }
}
