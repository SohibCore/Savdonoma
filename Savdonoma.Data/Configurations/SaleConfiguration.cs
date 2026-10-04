using Savdonoma.Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Savdonoma.Data.Configurations
{
    public class SaleConfiguration : IEntityTypeConfiguration<Sale>
    {
        public void Configure(EntityTypeBuilder<Sale> builder)
        {
            builder.ToTable("SYS_SALES");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnOrder(0)
                .HasColumnName("ID")
                .HasColumnType("integer")
                .ValueGeneratedOnAdd();

            builder.Property(x => x.Number)
                .HasColumnOrder(1)
                .HasColumnName("NUMBER")
                .HasColumnType("varchar(20)")
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.TotalAmount)
                .HasColumnType("bigint")
                .HasColumnOrder(2)
                .HasColumnName("TOTAL_AMOUNT")
                .IsRequired();

            builder.Property(x => x.PaymentMethod)
                .HasColumnOrder(3)
                .HasColumnName("PAYMENT_METHOD")
                .HasColumnType("text")
                .HasConversion<string>()
                .IsRequired();

            builder.Property(x => x.Status)
                .HasColumnType("text")
                .HasColumnOrder(4)
                .HasColumnName("STATUS")
                .HasConversion<string>()
                .IsRequired();

            builder.Property(x => x.CancelReason)
                .HasColumnType("varchar(500)")
                .HasColumnOrder(7)
                .HasColumnName("CANCEL_REASON")
                .IsRequired(false);

            builder.Property(x => x.CancelledAt)
                .HasColumnType("timestamp with time zone")
                .HasColumnOrder(8)
                .HasColumnName("CANCELLED_AT")
                .IsRequired(false);

            builder.Property(x => x.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .HasColumnOrder(9)
                .HasColumnName("CREATED_AT")
                .IsRequired(false);

            builder.Property(x => x.UpdatedAt)
                .HasColumnType("timestamp with time zone")
                .HasColumnOrder(10)
                .HasColumnName("UPDATED_AT")
                .IsRequired(false);
        }
    }
}
