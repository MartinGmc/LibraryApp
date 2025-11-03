using Microsoft.EntityFrameworkCore;
using LibraryApp.Domain.Entities;
using StatusEntity = LibraryApp.Domain.Entities.Status;

namespace LibraryApp.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<StatusEntity> Statuses { get; }
    DbSet<ApplicationUser> ApplicationUsers { get; }
    DbSet<Book> Books { get; }
    DbSet<BookLoan> BookLoans { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}


