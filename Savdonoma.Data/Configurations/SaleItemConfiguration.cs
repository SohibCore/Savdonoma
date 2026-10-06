using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Savdonoma.Core.Entity;

namespace Savdonoma.Data.Configurations
{
    public class SaleItemConfiguration : IEntityTypeConfiguration<SaleItem>
    {
        public void Configure(EntityTypeBuilder<SaleItem> builder)
        {
            builder.ToTable("SYS_SALE_ITEMS");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnOrder(0)
                .HasColumnName("ID")
                .HasColumnType("integer")
                .ValueGeneratedOnAdd();

            builder.Property(x => x.SaleId)
                .HasColumnOrder(1)
                .HasColumnName("SALE_ID")
                .HasColumnType("integer")
                .IsRequired();

            builder.Property(x => x.ProductId)
                .HasColumnOrder(2)
                .HasColumnName("PRODUCT_ID")
                .HasColumnType("integer")
                .IsRequired();

            builder.Property(x => x.Quantity)
                .HasColumnOrder(3)
                .HasColumnName("QUANTITY")
                .HasColumnType("integer")
                //.HasConversion(v => (long)(v * 1000m), v => v / 1000m)
                .IsRequired();

            builder.Property(x => x.UnitPrice)
                .HasColumnOrder(4)
                .HasColumnName("UNIT_PRICE")
                .HasColumnType("bigint")
                .IsRequired();

            builder.Property(x => x.ProductName)
                .HasColumnOrder(5)
                .HasColumnName("PRODUCT_NAME")
                .HasColumnType("varchar(200)")
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.LineTotal)
                .HasColumnOrder(6)
                .HasColumnName("LINE_TOTAL")
                .HasColumnType("bigint")
                .IsRequired();

            builder.Property(x => x.TotalAmount)
                .HasColumnOrder(7)
                .HasColumnName("TOTAL_AMOUNT")
                .HasColumnType("bigint")
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .HasColumnOrder(8)
                .HasColumnName("CREATED_AT")
                .HasColumnType("timestamp with time zone")
                .IsRequired(false);

            builder.Property(x => x.UpdatedAt)
                .HasColumnOrder(9)
                .HasColumnName("UPDATED_AT")
                .HasColumnType("timestamp with time zone")
                .IsRequired(false);

            builder.HasOne(x => x.Sale)
                .WithMany(x => x.SaleItems)
                .HasForeignKey(x => x.SaleId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Product)
                .WithMany(x => x.SaleItems)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.SaleId);
            builder.HasIndex(x => x.ProductId);
        }
    }
}
