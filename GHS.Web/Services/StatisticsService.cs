using Microsoft.EntityFrameworkCore;
using GHS.Web.Data;

namespace GHS.Web.Services;

public class StatisticsService
{
    private readonly IDbContextFactory<GHSDbContext> _dbFactory;

    public StatisticsService(IDbContextFactory<GHSDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<StatisticsSummary> GetSummaryAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var total = await db.UploadHistory.CountAsync();
        var success = await db.UploadHistory.CountAsync(e => e.IsAckSuccess);
        var today = DateTime.UtcNow.Date;
        var todayTotal = await db.UploadHistory.CountAsync(e => e.CreatedAt >= today);
        var todaySuccess = await db.UploadHistory.CountAsync(e => e.CreatedAt >= today && e.IsAckSuccess);

        return new StatisticsSummary
        {
            TotalUploads = total,
            SuccessCount = success,
            SuccessRate = total > 0 ? Math.Round((double)success / total * 100, 1) : 0,
            TodayUploads = todayTotal,
            TodaySuccess = todaySuccess
        };
    }

    public async Task<List<EquipmentStats>> GetEquipmentStatsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.UploadHistory
            .Where(e => e.EquipmentId != null)
            .GroupBy(e => e.EquipmentId!)
            .Select(g => new EquipmentStats
            {
                EquipmentId = g.Key,
                Total = g.Count(),
                Success = g.Count(e => e.IsAckSuccess)
            })
            .OrderByDescending(s => s.Total)
            .ToListAsync();
    }

    public async Task<List<DailyStats>> GetDailyStatsAsync(int days = 7)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var start = DateTime.UtcNow.Date.AddDays(-days + 1);
        var entries = await db.UploadHistory
            .Where(e => e.CreatedAt >= start)
            .ToListAsync();

        return Enumerable.Range(0, days).Select(offset =>
        {
            var date = start.AddDays(offset);
            var dayEntries = entries.Where(e => e.CreatedAt.Date == date).ToList();
            return new DailyStats
            {
                Date = date.ToString("MM-dd"),
                Total = dayEntries.Count,
                Success = dayEntries.Count(e => e.IsAckSuccess)
            };
        }).ToList();
    }
}

public class StatisticsSummary
{
    public int TotalUploads { get; set; }
    public int SuccessCount { get; set; }
    public double SuccessRate { get; set; }
    public int TodayUploads { get; set; }
    public int TodaySuccess { get; set; }
}

public class EquipmentStats
{
    public string EquipmentId { get; set; } = "";
    public int Total { get; set; }
    public int Success { get; set; }
}

public class DailyStats
{
    public string Date { get; set; } = "";
    public int Total { get; set; }
    public int Success { get; set; }
}
