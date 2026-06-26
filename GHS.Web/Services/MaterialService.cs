using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using GHS.Web.Data;
using GHS.Web.Models;
using GHS.Web.Services.Core;

namespace GHS.Web.Services;

public class MaterialService
{
    private readonly IDbContextFactory<GHSDbContext> _dbFactory;

    public MaterialService(IDbContextFactory<GHSDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<MaterialTypeInfo>> GetMaterialTreeAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var types = await db.MaterialTypes
            .OrderBy(t => t.ProjectId).ThenBy(t => t.TypeName)
            .ToListAsync();
        var subs = await db.MaterialSubTypes.ToListAsync();
        var projects = await db.Projects.ToListAsync();
        var equipments = await db.Equipments.ToListAsync();

        var eqMap = equipments.ToDictionary(e => e.Id, e => e.EquipmentId);
        var projectMap = projects.ToDictionary(p => p.Id, p => p.Name);

        return types.Select(t => new MaterialTypeInfo
        {
            TypeName = t.TypeName,
            SubTypes = subs
                .Where(s => s.MaterialTypeId == t.Id)
                .Select(s => new MaterialSubTypeInfo
                {
                    SubTypeName = s.SubTypeName,
                    AssociatedMachineName = eqMap.GetValueOrDefault(s.EquipmentId, ""),
                    EquipmentId = s.EquipmentId
                })
                .ToList()
        }).ToList();
    }

    public async Task<List<MaterialTypeEntity>> GetTypesByProjectAsync(int projectId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.MaterialTypes
            .Where(t => t.ProjectId == projectId)
            .OrderBy(t => t.TypeName)
            .ToListAsync();
    }

    public async Task<List<MaterialSubTypeEntity>> GetSubTypesWithInfoAsync(int projectId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.MaterialSubTypes
            .Where(s => db.MaterialTypes.Any(t => t.Id == s.MaterialTypeId && t.ProjectId == projectId))
            .OrderBy(s => s.SubTypeName)
            .Select(s => new MaterialSubTypeEntity
            {
                Id = s.Id,
                MaterialTypeId = s.MaterialTypeId,
                SubTypeName = s.SubTypeName,
                EquipmentId = s.EquipmentId,
                TypeName = db.MaterialTypes.Where(t => t.Id == s.MaterialTypeId).Select(t => t.TypeName).FirstOrDefault() ?? "",
                ProjectName = db.MaterialTypes.Where(t => t.Id == s.MaterialTypeId).Select(t => db.Projects.Where(p => p.Id == t.ProjectId).Select(p => p.Name).FirstOrDefault()).FirstOrDefault() ?? "",
                AssociatedMachineName = db.Equipments.Where(e => e.Id == s.EquipmentId).Select(e => e.EquipmentId).FirstOrDefault() ?? ""
            })
            .ToListAsync();
    }

    public async Task AddTypeAsync(int projectId, string typeName)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.MaterialTypes.Add(new MaterialTypeEntity
        {
            ProjectId = projectId,
            TypeName = typeName.Trim()
        });
        await db.SaveChangesAsync();
    }

    public async Task AddSubTypeAsync(int materialTypeId, string subTypeName, int equipmentId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.MaterialSubTypes.Add(new MaterialSubTypeEntity
        {
            MaterialTypeId = materialTypeId,
            SubTypeName = subTypeName.Trim(),
            EquipmentId = equipmentId
        });
        await db.SaveChangesAsync();
    }

    public async Task<int> ImportFromExcelAsync(Stream excelStream)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var projects = await db.Projects.ToListAsync();
        var equipments = await db.Equipments.ToListAsync();
        int importCount = 0;

        using var workbook = new XLWorkbook(excelStream);
        var worksheet = workbook.Worksheet(1);
        var rows = worksheet.RangeUsed().RowsUsed().Skip(1);

        foreach (var row in rows)
        {
            string typeName = row.Cell(1).GetValue<string>().Trim();
            string subTypeName = row.Cell(2).GetValue<string>().Trim();
            string machineName = row.Cell(3).GetValue<string>().Trim();
            string projectName = row.Cell(4).GetValue<string>().Trim();

            if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(subTypeName) || string.IsNullOrEmpty(projectName))
                continue;

            var project = projects.FirstOrDefault(p => p.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase));
            if (project == null)
            {
                project = new Project { Name = projectName };
                db.Projects.Add(project);
                await db.SaveChangesAsync();
                projects.Add(project);
            }

            var eq = equipments.FirstOrDefault(e => e.EquipmentId.Equals(machineName, StringComparison.OrdinalIgnoreCase));
            int eqId = eq?.Id ?? equipments.FirstOrDefault()?.Id ?? 0;

            var type = await db.MaterialTypes
                .FirstOrDefaultAsync(t => t.ProjectId == project.Id && t.TypeName == typeName);
            if (type == null)
            {
                type = new MaterialTypeEntity { ProjectId = project.Id, TypeName = typeName };
                db.MaterialTypes.Add(type);
                await db.SaveChangesAsync();
            }

            bool exists = await db.MaterialSubTypes
                .AnyAsync(s => s.MaterialTypeId == type.Id && s.SubTypeName == subTypeName);
            if (!exists)
            {
                db.MaterialSubTypes.Add(new MaterialSubTypeEntity
                {
                    MaterialTypeId = type.Id,
                    SubTypeName = subTypeName,
                    EquipmentId = eqId
                });
                importCount++;
            }
        }

        await db.SaveChangesAsync();
        return importCount;
    }

    public async Task DeleteTypeAsync(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var type = await db.MaterialTypes.FindAsync(id);
        if (type != null)
        {
            db.MaterialTypes.Remove(type);
            await db.SaveChangesAsync();
        }
    }

    public async Task DeleteSubTypeAsync(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var sub = await db.MaterialSubTypes.FindAsync(id);
        if (sub != null)
        {
            db.MaterialSubTypes.Remove(sub);
            await db.SaveChangesAsync();
        }
    }
}
