using Savdonoma.Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Savdonoma.Data.Configurations
{
    public class SystemSettingsConfiguration : IEntityTypeConfiguration<SystemSettings>
    {
        public void Configure(EntityTypeBuilder<SystemSettings> builder)
        {
            builder.ToTable("SYSTEM_SETTINGS");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnOrder(0)
                .HasColumnName("ID")
                .HasColumnType("integer")
                .ValueGeneratedOnAdd();

            builder.Property(x => x.InventoryEnabled)
                .HasColumnOrder(1)
                .HasColumnName("INVENTORY_ENABLED")
                .HasColumnType("boolean")
                .IsRequired();
            builder.HasIndex(x => x.InventoryEnabled);

            builder.Property(x => x.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .HasColumnName("UPDATED_AT")
                .HasColumnOrder(2)
                .IsRequired(false);
        }
    }
}
