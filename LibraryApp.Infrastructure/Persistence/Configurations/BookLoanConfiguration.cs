using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LibraryApp.Domain.Entities;

namespace LibraryApp.Infrastructure.Persistence.Configurations;

public class BookLoanConfiguration : IEntityTypeConfiguration<BookLoan>
{
    public void Configure(EntityTypeBuilder<BookLoan> builder)
    {
        builder.ToTable("BookLoans");

        builder.HasKey(bl => bl.Id);

        builder.Property(bl => bl.Id)
            .IsRequired();

        builder.Property(bl => bl.BookId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(bl => bl.UserId)
            .IsRequired();

        builder.Property(bl => bl.BorrowedDate)
            .IsRequired();

        builder.Property(bl => bl.ReturnedDate)
            .IsRequired(false);

        // Foreign key to Books
        builder.HasOne<Book>()
            .WithMany()
            .HasForeignKey(bl => bl.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign key to ApplicationUsers
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(bl => bl.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Index on BookId for availability queries
        builder.HasIndex(bl => bl.BookId);

        // Index on UserId for user's borrowed books queries
        builder.HasIndex(bl => bl.UserId);

        // Composite index on BookId and ReturnedDate for availability calculation performance
        builder.HasIndex(bl => new { bl.BookId, bl.ReturnedDate });
    }
}

