using Savdonoma.Core.Entity;
using Microsoft.EntityFrameworkCore;
using Savdonoma.Core.Enums;
using System.Text.Json;

namespace Savdonoma.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Sale> Sales { get; set; } = null!;
        public DbSet<SaleItem> SaleItems { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;

        public override async Task<int> SaveChangesAsync(CancellationToken cancellation = default)
        {
            var auditLogs = new List<AuditLog>();

            var entries = ChangeTracker
                .Entries()
                .Where(x =>
                x.State == EntityState.Modified ||
                x.State == EntityState.Deleted).ToList();

            foreach (var item in entries)
            {
                if (item.Entity is AuditLog)
                    continue;

                var oldVlaues = new Dictionary<string, object>();
                var newVlaues = new Dictionary<string, object>();

                AuditAction auditAction;

                switch (item.State)
                {
                    case EntityState.Modified:
                        auditAction = AuditAction.UPDATE;

                        foreach (var property in item.Properties)
                        {
                            if (!property.IsModified)
                                continue;

                            if (property.Metadata.Name == "Price")
                            {
                                oldVlaues[property.Metadata.Name] = property.OriginalValue;
                                newVlaues[property.Metadata.Name] = property.CurrentValue;
                            }
                        }
                        break;

                    case EntityState.Deleted:
                        auditAction = AuditAction.DELETE;

                        foreach (var property in item.Properties)
                        {
                            if (!property.IsModified)
                                continue;

                            oldVlaues[property.Metadata.Name] = property.OriginalValue;

                            newVlaues[property.Metadata.Name] = property.CurrentValue;
                        }
                        break;

                    default: continue;
                }

                var log = new AuditLog
                {
                    EntityName = "Product",
                    OldValue = oldVlaues.Count == 0 ? null : JsonSerializer.Serialize(oldVlaues.Values),
                    NewValue = newVlaues.Count == 0 ? null : JsonSerializer.Serialize(newVlaues.Values),
                    Log = auditAction,
                    CreatedAt = DateTime.Now,
                };
                auditLogs.Add(log);
            }
            AuditLogs.AddRange(auditLogs);

            return await base.SaveChangesAsync(cancellation);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}