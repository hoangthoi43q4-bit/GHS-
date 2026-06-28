using Microsoft.EntityFrameworkCore;
using GHS.Web.Data;
using GHS.Web.Models;

namespace GHS.Web.Services;

public class LineService
{
    private readonly IDbContextFactory<GHSDbContext> _dbFactory;

    public LineService(IDbContextFactory<GHSDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<Line>> GetAllAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Lines.OrderBy(l => l.Name).ToListAsync();
    }

    public async Task<Line> AddAsync(string name)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var line = new Line { Name = name.Trim() };
        db.Lines.Add(line);
        await db.SaveChangesAsync();
        return line;
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var line = await db.Lines.FindAsync(id);
        if (line != null)
        {
            db.Lines.Remove(line);
            await db.SaveChangesAsync();
        }
    }
}
