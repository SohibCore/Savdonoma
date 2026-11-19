using Savdonoma.Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Savdonoma.Data.Configurations
{
    public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
    {
        public void Configure(EntityTypeBuilder<Organization> builder)
        {
            builder.ToTable("SYS_ORGANIZATION");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("ID")
                .HasColumnOrder(1)
                .HasColumnType("integer")
                .ValueGeneratedOnAdd();

            builder.Property(x => x.Name)
                .HasColumnName("NAME")
                .HasColumnOrder(2)
                .HasColumnType("varchar(100)")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.Address)
                .HasColumnName("ADDRESS")
                .HasColumnOrder(3)
                .HasColumnType("varchar(200)")
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.INN)
                .HasColumnName("INN")
                .HasColumnOrder(4)
                .HasColumnType("varchar(12)")
                .HasMaxLength(12)
                .IsRequired();

            builder.Property(x => x.Phone)
                .HasColumnName("PHONE")
                .HasColumnOrder(5)
                .HasColumnType("varchar(12)")
                .IsRequired();

            builder.Property(x => x.Website)
                .HasColumnName("WEBSITE")
                .HasColumnOrder(6)
                .HasColumnType("varchar(100)")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.OwnerId)
                .HasColumnName("OWNER_ID")
                .HasColumnOrder(7)
                .HasColumnType("integer")
                .IsRequired();

            builder.HasOne(x => x.Owner)
                .WithMany(x => x.Organizations)
                .HasForeignKey(x => x.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
