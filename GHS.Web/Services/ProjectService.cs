using Microsoft.EntityFrameworkCore;
using GHS.Web.Data;
using GHS.Web.Models;

namespace GHS.Web.Services;

public class ProjectService
{
    private readonly IDbContextFactory<GHSDbContext> _dbFactory;

    public ProjectService(IDbContextFactory<GHSDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<Project>> GetAllAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var projects = await db.Projects.OrderBy(p => p.Name).ToListAsync();
        var lineIds = projects.Select(p => p.LineId).Distinct().ToList();
        var lines = await db.Lines.Where(l => lineIds.Contains(l.Id)).ToDictionaryAsync(l => l.Id, l => l.Name);
        foreach (var p in projects)
            p.LineName = lines.GetValueOrDefault(p.LineId, "");
        return projects;
    }

    public async Task<List<Project>> GetByLineAsync(int lineId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var projects = await db.Projects.Where(p => p.LineId == lineId).OrderBy(p => p.Name).ToListAsync();
        var line = await db.Lines.FindAsync(lineId);
        var lineName = line?.Name ?? "";
        foreach (var p in projects)
            p.LineName = lineName;
        return projects;
    }

    public async Task<Project> AddAsync(string name, int lineId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var project = new Project { Name = name.Trim(), LineId = lineId };
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        return project;
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var project = await db.Projects.FindAsync(id);
        if (project != null)
        {
            db.Projects.Remove(project);
            await db.SaveChangesAsync();
        }
    }
}
