using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LibraryApp.Domain.Entities;

namespace LibraryApp.Infrastructure.Persistence.Configurations;

public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("Books");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .IsRequired();

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(b => b.Author)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.IssueYear)
            .IsRequired();

        builder.Property(b => b.ISBN)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(b => b.NumberOfPieces)
            .IsRequired();

        builder.HasIndex(b => b.ISBN)
            .IsUnique();

        // Unique constraint on combination of Name + Author + ISBN
        builder.HasIndex(b => new { b.Name, b.Author, b.ISBN })
            .IsUnique();
    }
}

