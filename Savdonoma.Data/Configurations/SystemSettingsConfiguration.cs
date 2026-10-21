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

            builder.Property(x => x.ShopName)
                .HasColumnName("ORG_NAME")
                .HasColumnOrder(3)
                .HasColumnType("text")
                .IsRequired(false);

            builder.Property(x => x.Address)
                .HasColumnName("ADDRESS")
                .HasColumnOrder(4)
                .HasColumnType("text")
                .IsRequired(false);

            builder.Property(x => x.PrinterName)
                .HasColumnName("PRINTER_NAME")
                .HasColumnOrder(5)
                .HasColumnType("text")
                .IsRequired(false);

            builder.Property(x => x.CheckLength)
                .HasColumnName("CHECK_LENGTH")
                .HasColumnOrder(6)
                .HasColumnType("integer")
                .IsRequired(false);
        }
    }
}
