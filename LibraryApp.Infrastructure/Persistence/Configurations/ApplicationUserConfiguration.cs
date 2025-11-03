using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LibraryApp.Domain.Entities;

namespace LibraryApp.Infrastructure.Persistence.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("ApplicationUsers");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .ValueGeneratedOnAdd();

        builder.Property(u => u.Login)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.Password)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(u => u.UserType)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(u => u.ApiKey)
            .HasMaxLength(500);

        builder.Property(u => u.Permissions)
            .IsRequired()
            .HasConversion<string>();

        builder.HasIndex(u => u.Login)
            .IsUnique();
    }
}

