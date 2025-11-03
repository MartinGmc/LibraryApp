using Microsoft.EntityFrameworkCore;
using LibraryApp.Application.Common.Interfaces;
using LibraryApp.Domain.Entities;
using StatusEntity = LibraryApp.Domain.Entities.Status;

namespace LibraryApp.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<StatusEntity> Statuses => Set<StatusEntity>();
    public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<BookLoan> BookLoans => Set<BookLoan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}


