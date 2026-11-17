using Savdonoma.Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Savdonoma.Data.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("SYS_PRODUCT");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("ID")
                .HasColumnOrder(0)
                .HasColumnType("integer")
                .ValueGeneratedOnAdd();

            builder.Property(x => x.Unit)
                .HasColumnType("text")
                .HasColumnName("UNIT")
                .HasColumnOrder(1)
                .HasConversion<string>()
                .IsRequired();

            builder.Property(x => x.Price)
                .HasColumnOrder(2)
                .HasColumnName("PRICE")
                .HasColumnType("bigint")
                .IsRequired();
            builder.HasIndex(x => x.Price);

            builder.Property(x => x.IsActive)
                .HasColumnType("boolean")
                .HasColumnName("IS_ACTIVE")
                .HasColumnOrder(3)
                .IsRequired();

            builder.Property(x => x.CategoryId)
                .HasColumnType("integer")
                .HasColumnName("CATEGORY_ID")
                .HasColumnOrder(4)
                .IsRequired(false);

            builder.Property(x => x.Barcode)
                .HasColumnType("text")
                .HasColumnName("BARCODE")
                .HasColumnOrder(5)
                .IsRequired(false);
            builder.HasIndex(x => x.Barcode).IsUnique(true);

            builder.Property(x => x.Name)
                .HasColumnType("varchar(200)")
                .HasColumnName("NAME")
                .HasColumnOrder(6)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.NameSearch)
                .HasColumnType("varchar(200)")
                .HasColumnName("NAME_SEARCH")
                .HasColumnOrder(7)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .HasColumnName("CREATED_AT")
                .HasColumnOrder(8)
                .IsRequired(false);

            builder.Property(x => x.UpdatedAt)
                .HasColumnType("timestamp with time zone")
                .HasColumnName("UPDATED_AT")
                .HasColumnOrder(9)
                .IsRequired(false);

            builder.Property(x => x.Quantity)
                .HasColumnOrder(10)
                .HasColumnName("QUANTITY")
                .HasColumnType("decimal")
                .HasDefaultValue(0m)
                .IsRequired();

            builder.Property(x => x.CostPrice)
                .HasColumnOrder(11)
                .HasColumnName("COST_PRICE")
                .HasColumnType("bigint")
                .IsRequired();

            builder.Property(x => x.MarkupPercent)
                .HasColumnOrder(18)
                .HasColumnName("MARKUP_PERCENT")
                .HasColumnType("decimal")
                .IsRequired(false);

            builder.Property(x => x.MinQuantity)
                .HasColumnOrder(12)
                .HasColumnName("MIN_QUANTITY")
                .HasColumnType("decimal")
                .IsRequired(false);

            builder.Property(x => x.ScaleCode)
                .HasColumnOrder(13)
                .HasColumnName("SCALE_CODE")
                .HasColumnType("varchar(10)")
                .HasMaxLength(10)
                .IsRequired(false);
            builder.HasIndex(x => x.ScaleCode).IsUnique();

            builder.Property(x => x.MxikCode)
                .HasColumnOrder(14)
                .HasColumnName("MXIK_CODE")
                .HasColumnType("varchar(20)")
                .HasMaxLength(20)
                .IsRequired(false);

            builder.Property(x => x.PackageCode)
                .HasColumnOrder(15)
                .HasColumnName("PACKAGE_CODE")
                .HasColumnType("varchar(20)")
                .HasMaxLength(20)
                .IsRequired(false);

            builder.Property(x => x.VatPercent)
                .HasColumnOrder(16)
                .HasColumnName("VAT_PERCENT")
                .HasColumnType("decimal")
                .IsRequired(false);

            builder.Property(x => x.IsMarked)
                .HasColumnOrder(17)
                .HasColumnName("IS_MARKED")
                .HasColumnType("boolean")
                .IsRequired();

            builder.HasOne(x => x.Category)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(x => x.NameSearch);
            builder.HasIndex(x => x.Barcode).IsUnique();
        }
    }
}
