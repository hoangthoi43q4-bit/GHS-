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
        return await db.Projects
            .OrderBy(p => p.Name)
            .Select(p => new Project
            {
                Id = p.Id,
                Name = p.Name,
                LineId = p.LineId,
                LineName = db.Lines.Where(l => l.Id == p.LineId).Select(l => l.Name).FirstOrDefault() ?? ""
            })
            .ToListAsync();
    }

    public async Task<List<Project>> GetByLineAsync(int lineId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Projects
            .Where(p => p.LineId == lineId)
            .OrderBy(p => p.Name)
            .Select(p => new Project
            {
                Id = p.Id,
                Name = p.Name,
                LineId = p.LineId,
                LineName = db.Lines.Where(l => l.Id == p.LineId).Select(l => l.Name).FirstOrDefault() ?? ""
            })
            .ToListAsync();
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
