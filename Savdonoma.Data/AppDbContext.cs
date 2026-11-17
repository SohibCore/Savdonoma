using Savdonoma.Core.Entity;
using Microsoft.EntityFrameworkCore;
using Savdonoma.Core.Enums;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Savdonoma.Data
{
    public class AppDbContext : DbContext
    {
        // Audit logga qiymati yozilmaydigan maydonlar (masalan, parol xeshi)
        private static readonly HashSet<string> MaskedProperties = new() { nameof(User.Password) };
        private const string MaskedValue = "***";

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Sale> Sales { get; set; } = null!;
        public DbSet<SaleItem> SaleItems { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<SystemSettings> SysSettings { get; set; } = null!;
        public DbSet<Supplier> Suppliers { get; set; } = null!;
        public DbSet<SupplierPayment> SupplierPayments { get; set; } = null!;
        public DbSet<StockDocument> StockDocuments { get; set; } = null!;
        public DbSet<StockDocumentLine> StockDocumentLines { get; set; } = null!;
        public DbSet<StockMovement> StockMovements { get; set; } = null!;
        public DbSet<ProductBarcode> ProductBarcodes { get; set; } = null!;

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            AddAuditLogs();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            AddAuditLogs();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        // O'zgartirilgan va o'chirilgan yozuvlar uchun audit log yaratadi
        private void AddAuditLogs()
        {
            var entries = ChangeTracker
                .Entries()
                .Where(x => x.Entity is not AuditLog
                         && (x.State == EntityState.Modified || x.State == EntityState.Deleted))
                .ToList();

            if (entries.Count == 0)
                return;

            var now = DateTime.UtcNow;
            var logs = new List<AuditLog>();

            foreach (var entry in entries)
            {
                var oldValues = new Dictionary<string, object?>();
                var newValues = new Dictionary<string, object?>();
                AuditAction action;

                if (entry.State == EntityState.Modified)
                {
                    action = AuditAction.UPDATE;

                    foreach (var property in entry.Properties)
                    {
                        if (!property.IsModified || Equals(property.OriginalValue, property.CurrentValue))
                            continue;

                        var name = property.Metadata.Name;
                        oldValues[name] = Mask(name, property.OriginalValue);
                        newValues[name] = Mask(name, property.CurrentValue);
                    }

                    // Haqiqatda hech narsa o'zgarmagan bo'lsa, log yozmaymiz
                    if (oldValues.Count == 0)
                        continue;
                }
                else
                {
                    action = AuditAction.DELETE;

                    foreach (var property in entry.Properties)
                    {
                        var name = property.Metadata.Name;
                        oldValues[name] = Mask(name, property.OriginalValue);
                    }
                }

                logs.Add(new AuditLog
                {
                    EntityName = entry.Metadata.ClrType.Name,
                    EntityId = GetPrimaryKey(entry),
                    OldValue = oldValues.Count == 0 ? null : JsonSerializer.Serialize(oldValues),
                    NewValue = newValues.Count == 0 ? null : JsonSerializer.Serialize(newValues),
                    Log = action,
                    CreatedAt = now,
                });
            }

            if (logs.Count > 0)
                AuditLogs.AddRange(logs);
        }

        private static object? Mask(string propertyName, object? value)
            => MaskedProperties.Contains(propertyName) && value is not null ? MaskedValue : value;

        private static string? GetPrimaryKey(EntityEntry entry)
        {
            var key = entry.Metadata.FindPrimaryKey();
            if (key is null)
                return null;

            return string.Join(",", key.Properties.Select(p => entry.Property(p.Name).OriginalValue?.ToString()));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
