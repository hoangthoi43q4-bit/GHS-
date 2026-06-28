using Microsoft.EntityFrameworkCore;
using GHS.Web.Models;

namespace GHS.Web.Data;

public class GHSDbContext : DbContext
{
    public GHSDbContext(DbContextOptions<GHSDbContext> options) : base(options) { }

    public DbSet<Line> Lines => Set<Line>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Equipment> Equipments => Set<Equipment>();
    public DbSet<MaterialTypeEntity> MaterialTypes => Set<MaterialTypeEntity>();
    public DbSet<MaterialSubTypeEntity> MaterialSubTypes => Set<MaterialSubTypeEntity>();
    public DbSet<UploadHistoryEntry> UploadHistory => Set<UploadHistoryEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Line>(entity =>
        {
            entity.ToTable("lines");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("projects");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasOne<Line>().WithMany().HasForeignKey(e => e.LineId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.LineId, e.Name }).IsUnique();
        });

        modelBuilder.Entity<Equipment>(entity =>
        {
            entity.ToTable("equipments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EquipmentId).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.EquipmentId).IsUnique();
            entity.Property(e => e.ServerIp).IsRequired().HasMaxLength(45);
        });

        modelBuilder.Entity<MaterialTypeEntity>(entity =>
        {
            entity.ToTable("material_types");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TypeName).IsRequired().HasMaxLength(10);
            entity.HasOne<Project>().WithMany().HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.ProjectId, e.TypeName }).IsUnique();
        });

        modelBuilder.Entity<MaterialSubTypeEntity>(entity =>
        {
            entity.ToTable("material_sub_types");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SubTypeName).IsRequired().HasMaxLength(200);
            entity.HasOne<MaterialTypeEntity>().WithMany().HasForeignKey(e => e.MaterialTypeId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Equipment>().WithMany().HasForeignKey(e => e.EquipmentId);
            entity.HasIndex(e => new { e.MaterialTypeId, e.SubTypeName }).IsUnique();
        });

        modelBuilder.Entity<UploadHistoryEntry>(entity =>
        {
            entity.ToTable("upload_history");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.LineName).HasMaxLength(100);
            entity.Property(e => e.ProjectName).HasMaxLength(100);
            entity.Property(e => e.TypeName).HasMaxLength(10);
            entity.Property(e => e.SubTypeName).HasMaxLength(200);
            entity.Property(e => e.EquipmentId).HasMaxLength(100);
            entity.HasIndex(e => e.LineName);
            entity.HasIndex(e => e.ProjectName);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.SubTypeName);
        });
    }
}
