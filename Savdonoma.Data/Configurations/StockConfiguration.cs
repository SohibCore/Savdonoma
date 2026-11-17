using Savdonoma.Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Savdonoma.Data.Configurations
{
    public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
    {
        public void Configure(EntityTypeBuilder<Supplier> builder)
        {
            builder.ToTable("SYS_SUPPLIER");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("ID").HasColumnType("integer").HasColumnOrder(0).ValueGeneratedOnAdd();
            builder.Property(x => x.Name).HasColumnName("NAME").HasColumnType("varchar(200)").HasMaxLength(200).HasColumnOrder(1).IsRequired();
            builder.Property(x => x.Phone).HasColumnName("PHONE").HasColumnType("varchar(50)").HasMaxLength(50).HasColumnOrder(2);
            builder.Property(x => x.Inn).HasColumnName("INN").HasColumnType("varchar(20)").HasMaxLength(20).HasColumnOrder(3);
            builder.Property(x => x.Note).HasColumnName("NOTE").HasColumnType("varchar(500)").HasMaxLength(500).HasColumnOrder(4);
            builder.Property(x => x.IsActive).HasColumnName("IS_ACTIVE").HasColumnType("boolean").HasColumnOrder(5).IsRequired();
            builder.Property(x => x.Debt).HasColumnName("DEBT").HasColumnType("bigint").HasColumnOrder(6).IsRequired();
            builder.Property(x => x.CreatedAt).HasColumnName("CREATED_AT").HasColumnType("timestamp with time zone").HasColumnOrder(7).IsRequired();
            builder.Property(x => x.UpdatedAt).HasColumnName("UPDATED_AT").HasColumnType("timestamp with time zone").HasColumnOrder(8);

            builder.HasIndex(x => x.Name);
        }
    }

    public class SupplierPaymentConfiguration : IEntityTypeConfiguration<SupplierPayment>
    {
        public void Configure(EntityTypeBuilder<SupplierPayment> builder)
        {
            builder.ToTable("SYS_SUPPLIER_PAYMENT");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("ID").HasColumnType("integer").HasColumnOrder(0).ValueGeneratedOnAdd();
            builder.Property(x => x.SupplierId).HasColumnName("SUPPLIER_ID").HasColumnType("integer").HasColumnOrder(1).IsRequired();
            builder.Property(x => x.Amount).HasColumnName("AMOUNT").HasColumnType("bigint").HasColumnOrder(2).IsRequired();
            builder.Property(x => x.Note).HasColumnName("NOTE").HasColumnType("varchar(500)").HasMaxLength(500).HasColumnOrder(3);
            builder.Property(x => x.UserId).HasColumnName("USER_ID").HasColumnType("integer").HasColumnOrder(4);
            builder.Property(x => x.CreatedAt).HasColumnName("CREATED_AT").HasColumnType("timestamp with time zone").HasColumnOrder(5).IsRequired();

            builder.HasOne(x => x.Supplier)
                .WithMany(x => x.Payments)
                .HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.SupplierId, x.CreatedAt });
        }
    }

    public class StockDocumentConfiguration : IEntityTypeConfiguration<StockDocument>
    {
        public void Configure(EntityTypeBuilder<StockDocument> builder)
        {
            builder.ToTable("SYS_STOCK_DOCUMENT");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("ID").HasColumnType("integer").HasColumnOrder(0).ValueGeneratedOnAdd();
            builder.Property(x => x.Number).HasColumnName("NUMBER").HasColumnType("varchar(30)").HasMaxLength(30).HasColumnOrder(1).IsRequired();
            builder.Property(x => x.Type).HasColumnName("TYPE").HasColumnType("text").HasConversion<string>().HasColumnOrder(2).IsRequired();
            builder.Property(x => x.Status).HasColumnName("STATUS").HasColumnType("text").HasConversion<string>().HasColumnOrder(3).IsRequired();
            builder.Property(x => x.SupplierId).HasColumnName("SUPPLIER_ID").HasColumnType("integer").HasColumnOrder(4);
            builder.Property(x => x.Note).HasColumnName("NOTE").HasColumnType("varchar(500)").HasMaxLength(500).HasColumnOrder(5);
            builder.Property(x => x.TotalCost).HasColumnName("TOTAL_COST").HasColumnType("bigint").HasColumnOrder(6).IsRequired();
            builder.Property(x => x.CreatedAt).HasColumnName("CREATED_AT").HasColumnType("timestamp with time zone").HasColumnOrder(7).IsRequired();
            builder.Property(x => x.CreatedByUserId).HasColumnName("CREATED_BY").HasColumnType("integer").HasColumnOrder(8);
            builder.Property(x => x.PostedAt).HasColumnName("POSTED_AT").HasColumnType("timestamp with time zone").HasColumnOrder(9);
            builder.Property(x => x.PostedByUserId).HasColumnName("POSTED_BY").HasColumnType("integer").HasColumnOrder(10);
            builder.Property(x => x.CancelledAt).HasColumnName("CANCELLED_AT").HasColumnType("timestamp with time zone").HasColumnOrder(11);
            builder.Property(x => x.CancelledByUserId).HasColumnName("CANCELLED_BY").HasColumnType("integer").HasColumnOrder(12);
            builder.Property(x => x.CancelReason).HasColumnName("CANCEL_REASON").HasColumnType("varchar(500)").HasMaxLength(500).HasColumnOrder(13);
            builder.Property(x => x.UpdatedAt).HasColumnName("UPDATED_AT").HasColumnType("timestamp with time zone").HasColumnOrder(14);

            builder.HasOne(x => x.Supplier)
                .WithMany()
                .HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.Number).IsUnique();
            builder.HasIndex(x => new { x.Type, x.CreatedAt });
            builder.HasIndex(x => x.SupplierId);
        }
    }

    public class StockDocumentLineConfiguration : IEntityTypeConfiguration<StockDocumentLine>
    {
        public void Configure(EntityTypeBuilder<StockDocumentLine> builder)
        {
            builder.ToTable("SYS_STOCK_DOCUMENT_LINE");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("ID").HasColumnType("integer").HasColumnOrder(0).ValueGeneratedOnAdd();
            builder.Property(x => x.DocumentId).HasColumnName("DOCUMENT_ID").HasColumnType("integer").HasColumnOrder(1).IsRequired();
            builder.Property(x => x.ProductId).HasColumnName("PRODUCT_ID").HasColumnType("integer").HasColumnOrder(2).IsRequired();
            builder.Property(x => x.Quantity).HasColumnName("QUANTITY").HasColumnType("decimal").HasColumnOrder(3).IsRequired();
            builder.Property(x => x.CostPrice).HasColumnName("COST_PRICE").HasColumnType("bigint").HasColumnOrder(4).IsRequired();
            builder.Property(x => x.NewSalePrice).HasColumnName("NEW_SALE_PRICE").HasColumnType("bigint").HasColumnOrder(5);
            builder.Property(x => x.ExpectedQuantity).HasColumnName("EXPECTED_QUANTITY").HasColumnType("decimal").HasColumnOrder(6);

            builder.HasOne(x => x.Document)
                .WithMany(x => x.Lines)
                .HasForeignKey(x => x.DocumentId)
                .OnDelete(DeleteBehavior.Cascade); // qoralama o'chirilganda qatorlari ham o'chadi

            builder.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.DocumentId);
            builder.HasIndex(x => x.ProductId);
        }
    }

    public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
    {
        public void Configure(EntityTypeBuilder<StockMovement> builder)
        {
            builder.ToTable("SYS_STOCK_MOVEMENT");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("ID").HasColumnType("integer").HasColumnOrder(0).ValueGeneratedOnAdd();
            builder.Property(x => x.ProductId).HasColumnName("PRODUCT_ID").HasColumnType("integer").HasColumnOrder(1).IsRequired();
            builder.Property(x => x.Type).HasColumnName("TYPE").HasColumnType("text").HasConversion<string>().HasColumnOrder(2).IsRequired();
            builder.Property(x => x.Quantity).HasColumnName("QUANTITY").HasColumnType("decimal").HasColumnOrder(3).IsRequired();
            builder.Property(x => x.BalanceAfter).HasColumnName("BALANCE_AFTER").HasColumnType("decimal").HasColumnOrder(4).IsRequired();
            builder.Property(x => x.CostPrice).HasColumnName("COST_PRICE").HasColumnType("bigint").HasColumnOrder(5).IsRequired();
            builder.Property(x => x.DocumentId).HasColumnName("DOCUMENT_ID").HasColumnType("integer").HasColumnOrder(6);
            builder.Property(x => x.SaleId).HasColumnName("SALE_ID").HasColumnType("integer").HasColumnOrder(7);
            builder.Property(x => x.UserId).HasColumnName("USER_ID").HasColumnType("integer").HasColumnOrder(8);
            builder.Property(x => x.Note).HasColumnName("NOTE").HasColumnType("varchar(500)").HasMaxLength(500).HasColumnOrder(9);
            builder.Property(x => x.CreatedAt).HasColumnName("CREATED_AT").HasColumnType("timestamp with time zone").HasColumnOrder(10).IsRequired();

            builder.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Document)
                .WithMany()
                .HasForeignKey(x => x.DocumentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Sale)
                .WithMany()
                .HasForeignKey(x => x.SaleId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.ProductId, x.Id });
            builder.HasIndex(x => x.DocumentId);
            builder.HasIndex(x => x.SaleId);
            builder.HasIndex(x => x.CreatedAt);
        }
    }

    public class ProductBarcodeConfiguration : IEntityTypeConfiguration<ProductBarcode>
    {
        public void Configure(EntityTypeBuilder<ProductBarcode> builder)
        {
            builder.ToTable("SYS_PRODUCT_BARCODE");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("ID").HasColumnType("integer").HasColumnOrder(0).ValueGeneratedOnAdd();
            builder.Property(x => x.ProductId).HasColumnName("PRODUCT_ID").HasColumnType("integer").HasColumnOrder(1).IsRequired();
            builder.Property(x => x.Barcode).HasColumnName("BARCODE").HasColumnType("varchar(50)").HasMaxLength(50).HasColumnOrder(2).IsRequired();
            builder.Property(x => x.CreatedAt).HasColumnName("CREATED_AT").HasColumnType("timestamp with time zone").HasColumnOrder(3).IsRequired();

            builder.HasOne(x => x.Product)
                .WithMany(x => x.Barcodes)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.Barcode).IsUnique();
            builder.HasIndex(x => x.ProductId);
        }
    }
}
