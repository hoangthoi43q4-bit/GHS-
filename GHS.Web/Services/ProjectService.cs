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
        return await db.Projects.OrderBy(p => p.Name).ToListAsync();
    }

    public async Task<Project> AddAsync(string name)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var project = new Project { Name = name.Trim() };
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
