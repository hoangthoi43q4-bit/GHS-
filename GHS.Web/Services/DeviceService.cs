using Microsoft.EntityFrameworkCore;
using GHS.Web.Data;
using GHS.Web.Models;

namespace GHS.Web.Services;

public class DeviceService
{
    private readonly IDbContextFactory<GHSDbContext> _dbFactory;

    public DeviceService(IDbContextFactory<GHSDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<Equipment>> GetAllAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Equipments.OrderBy(e => e.EquipmentId).ToListAsync();
    }

    public async Task<Equipment?> GetByIdAsync(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Equipments.FindAsync(id);
    }

    public async Task<Equipment> AddAsync(string equipmentId, string serverIp, int serverPort, string? bufferXmlPath, string? description)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var eq = new Equipment
        {
            EquipmentId = equipmentId.Trim(),
            ServerIp = serverIp.Trim(),
            ServerPort = serverPort,
            BufferXmlPath = string.IsNullOrWhiteSpace(bufferXmlPath) ? null : bufferXmlPath.Trim(),
            Description = description?.Trim()
        };
        db.Equipments.Add(eq);
        await db.SaveChangesAsync();
        return eq;
    }

    public async Task UpdateAsync(int id, string equipmentId, string serverIp, int serverPort, string? bufferXmlPath, string? description)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var eq = await db.Equipments.FindAsync(id);
        if (eq == null) return;
        eq.EquipmentId = equipmentId.Trim();
        eq.ServerIp = serverIp.Trim();
        eq.ServerPort = serverPort;
        eq.BufferXmlPath = string.IsNullOrWhiteSpace(bufferXmlPath) ? null : bufferXmlPath.Trim();
        eq.Description = description?.Trim();
        eq.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var eq = await db.Equipments.FindAsync(id);
        if (eq != null)
        {
            db.Equipments.Remove(eq);
            await db.SaveChangesAsync();
        }
    }
}
