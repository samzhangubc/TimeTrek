using Microsoft.EntityFrameworkCore;

namespace TimeTrek.Infrastructure.Persistence;

public sealed class TimeTrekDbContext(DbContextOptions<TimeTrekDbContext> options) : DbContext(options)
{
    internal DbSet<StreamRow> Streams => Set<StreamRow>();

    internal DbSet<CategoryRow> Categories => Set<CategoryRow>();

    internal DbSet<ProjectRow> Projects => Set<ProjectRow>();

    internal DbSet<SessionRow> Sessions => Set<SessionRow>();

    internal DbSet<SessionCategoryRow> SessionCategories => Set<SessionCategoryRow>();

    internal DbSet<TimingRootRow> TimingRoots => Set<TimingRootRow>();

    internal DbSet<TimingSegmentRow> TimingSegments => Set<TimingSegmentRow>();

    internal DbSet<AdjustmentRow> Adjustments => Set<AdjustmentRow>();

    internal DbSet<AdjustmentCategoryRow> AdjustmentCategories => Set<AdjustmentCategoryRow>();

    internal DbSet<ForegroundApplicationRow> ForegroundApplications => Set<ForegroundApplicationRow>();

    internal DbSet<PaletteRow> Palettes => Set<PaletteRow>();

    internal DbSet<SettingRow> Settings => Set<SettingRow>();

    internal DbSet<DeletionBatchRow> DeletionBatches => Set<DeletionBatchRow>();

    internal DbSet<SchemaStateRow> SchemaStates => Set<SchemaStateRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StreamRow>(entity =>
        {
            entity.ToTable("Streams");
            entity.HasKey(row => row.Id);
            entity.Property(row => row.Name).HasMaxLength(200);
            entity.Property(row => row.Color).HasMaxLength(7);
            entity.HasIndex(row => new { row.IsDeleted, row.IsArchived, row.SortOrder });
        });

        modelBuilder.Entity<CategoryRow>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasKey(row => row.Id);
            entity.Property(row => row.Name).HasMaxLength(200);
            entity.Property(row => row.CurrencyCode).HasMaxLength(3);
            entity.HasIndex(row => new { row.StreamId, row.IsDeleted, row.IsArchived });
            entity.HasOne<StreamRow>().WithMany().HasForeignKey(row => row.StreamId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProjectRow>(entity =>
        {
            entity.ToTable("Projects");
            entity.HasKey(row => row.Id);
            entity.Property(row => row.Name).HasMaxLength(200);
            entity.HasIndex(row => new { row.StreamId, row.IsDeleted, row.IsArchived });
            entity.HasOne<StreamRow>().WithMany().HasForeignKey(row => row.StreamId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SessionRow>(entity =>
        {
            entity.ToTable("Sessions");
            entity.HasKey(row => row.Id);
            entity.Property(row => row.TimeZoneId).HasMaxLength(200);
            entity.Property(row => row.CurrencyCode).HasMaxLength(3);
            entity.Property(row => row.Description).HasMaxLength(16_384);
            entity.Property(row => row.StreamOriginalName).HasMaxLength(200);
            entity.Property(row => row.ProjectOriginalName).HasMaxLength(200);
            entity.HasIndex(row => new { row.IsDraft, row.IsDeleted, row.StartUtcMilliseconds });
            entity.HasIndex(row => new { row.StreamId, row.StartUtcMilliseconds });
            entity.HasIndex(row => new { row.ProjectId, row.StartUtcMilliseconds });
            entity.HasOne<StreamRow>().WithMany().HasForeignKey(row => row.StreamId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ProjectRow>().WithMany().HasForeignKey(row => row.ProjectId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SessionCategoryRow>(entity =>
        {
            entity.ToTable("SessionCategories");
            entity.HasKey(row => new { row.SessionId, row.CategoryId });
            entity.Property(row => row.OriginalName).HasMaxLength(200);
            entity.HasOne(row => row.Session).WithMany(row => row.Categories).HasForeignKey(row => row.SessionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<CategoryRow>().WithMany().HasForeignKey(row => row.CategoryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(row => new { row.CategoryId, row.SessionId });
        });

        modelBuilder.Entity<TimingRootRow>(entity =>
        {
            entity.ToTable("TimingRoots", table => table.HasCheckConstraint("CK_TimingRoots_Singleton", "SingletonKey = 1"));
            entity.HasKey(row => row.Id);
            entity.HasIndex(row => row.SingletonKey).IsUnique();
            entity.Property(row => row.CategoryIdsJson).HasMaxLength(4096);
            entity.Property(row => row.CurrencyCode).HasMaxLength(3);
        });

        modelBuilder.Entity<TimingSegmentRow>(entity =>
        {
            entity.ToTable("TimingSegments");
            entity.HasKey(row => row.Id);
            entity.HasIndex(row => new { row.SessionId, row.StartUtcMilliseconds });
            entity.HasOne<SessionRow>().WithMany().HasForeignKey(row => row.SessionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AdjustmentRow>(entity =>
        {
            entity.ToTable("Adjustments");
            entity.HasKey(row => row.Id);
            entity.Property(row => row.Description).HasMaxLength(16_384);
            entity.Property(row => row.TimeZoneId).HasMaxLength(200);
            entity.Property(row => row.CurrencyCode).HasMaxLength(3);
            entity.HasIndex(row => new { row.IsDeleted, row.LocalDateUnixDays });
        });

        modelBuilder.Entity<AdjustmentCategoryRow>(entity =>
        {
            entity.ToTable("AdjustmentCategories");
            entity.HasKey(row => new { row.AdjustmentId, row.CategoryId });
            entity.Property(row => row.OriginalName).HasMaxLength(200);
            entity.HasOne(row => row.Adjustment).WithMany(row => row.Categories).HasForeignKey(row => row.AdjustmentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<CategoryRow>().WithMany().HasForeignKey(row => row.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ForegroundApplicationRow>(entity =>
        {
            entity.ToTable("ForegroundApplications");
            entity.HasKey(row => row.Id);
            entity.Property(row => row.DisplayName).HasMaxLength(200);
            entity.Property(row => row.ExecutableFileName).HasMaxLength(260);
            entity.HasIndex(row => new { row.SessionId, row.ExecutableFileName }).IsUnique();
            entity.HasOne<SessionRow>().WithMany().HasForeignKey(row => row.SessionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PaletteRow>(entity =>
        {
            entity.ToTable("Palettes");
            entity.HasKey(row => row.Id);
            entity.Property(row => row.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<SettingRow>(entity =>
        {
            entity.ToTable("Settings");
            entity.HasKey(row => row.Key);
            entity.Property(row => row.Key).HasMaxLength(200);
            entity.Property(row => row.JsonValue).HasMaxLength(65_536);
        });

        modelBuilder.Entity<DeletionBatchRow>(entity =>
        {
            entity.ToTable("DeletionBatches");
            entity.HasKey(row => row.Id);
            entity.Property(row => row.RootName).HasMaxLength(200);
            entity.HasIndex(row => row.PurgeAfterUtcMilliseconds);
        });

        modelBuilder.Entity<SchemaStateRow>(entity =>
        {
            entity.ToTable("SchemaState", table => table.HasCheckConstraint("CK_SchemaState_Singleton", "Id = 1"));
            entity.HasKey(row => row.Id);
        });
    }
}
