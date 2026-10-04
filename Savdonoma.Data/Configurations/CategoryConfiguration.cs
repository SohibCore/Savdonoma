using Savdonoma.Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Savdonoma.Data.Configurations
{
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.ToTable("SYS_CATEGORY");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnOrder(0)
                .HasColumnName("ID")
                .HasColumnType("integer")
                .ValueGeneratedOnAdd();

            builder.Property(x => x.Name)
                .HasColumnType("varchar(100)")
                .HasColumnName("NAME")
                .HasColumnOrder(1)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.IsActive)
                .HasColumnType("boolean")
                .HasColumnName("IS_ACTIVE")
                .HasColumnOrder(2)
                .IsRequired();
        }
    }
}
