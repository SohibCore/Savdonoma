using Savdonoma.Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Security.Cryptography.X509Certificates;

namespace Savdonoma.Data.Configurations
{
    public class AuditConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("SYS_AUDIT_LOGS");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnOrder(0)
                .HasColumnName("ID")
                .HasColumnType("integer")
                .ValueGeneratedOnAdd();

            builder.Property(x => x.OldValue)
                .HasColumnName("OLD_VALUE")
                .HasColumnType("text")
                .HasColumnOrder(1);

            builder.Property(x => x.NewValue)
                .HasColumnName("NEW_VALUE")
                .HasColumnType("text")
                .HasColumnOrder(2);

            builder.Property(x => x.EntityName)
                .HasColumnOrder(3)
                .HasColumnName("ENTITY_NAME")
                .HasColumnType("varchar(20)")
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.Log)
                .HasColumnName("AUDIT_LOG")
                .HasConversion<string>()
                .HasColumnOrder(4)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .HasColumnOrder(5)
                .HasColumnName("CREATED_AT")
                .HasColumnType("timestamp with time zone")
                .IsRequired();
        }
    }
}
