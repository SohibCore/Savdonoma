using Savdonoma.Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Savdonoma.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("SYS_USER");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnOrder(0)
                .HasColumnName("ID")
                .HasColumnType("integer")
                .ValueGeneratedOnAdd();

            builder.Property(x => x.FullName)
                .HasColumnOrder(1)
                .HasColumnName("FULL_NAME")
                .HasColumnType("varchar(350)")
                .HasMaxLength(350)
                .IsRequired();
            builder.HasIndex(x => x.FullName);

            builder.Property(x => x.UserName)
                .HasColumnOrder(2)
                .HasColumnName("USER_NAME")
                .HasColumnType("varchar(100)")
                .HasMaxLength(100)
                .IsRequired();
            builder.HasIndex(x => x.UserName);

            builder.Property(x => x.Password)
                .HasColumnOrder(3)
                .HasColumnName("PASSWORD")
                .HasColumnType("varchar(500)")
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(x => x.OrgName)
                .HasColumnOrder(4)
                .HasColumnName("ORG_NAME")
                .HasColumnType("varchar(500)")
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(x => x.IsActive)
                .HasColumnOrder(5)
                .HasColumnType("boolean")
                .HasColumnName("IS_ACTIVE")
                .IsRequired();

            builder.Property(x => x.UserRole)
               .HasColumnOrder(6)
               .HasColumnName("USER_ROLE")
               .HasColumnType("text")
               .HasConversion<string>()
               .IsRequired();

            builder.Property(x => x.SelfRegister)
                .HasColumnOrder(7)
                .HasColumnType("boolean")
                .HasColumnName("SELF_REGISTER")
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
        }
    }
}
