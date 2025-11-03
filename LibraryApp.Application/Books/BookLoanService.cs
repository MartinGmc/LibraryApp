using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LibraryApp.Application.Books.Dtos;
using LibraryApp.Application.Common.Interfaces;
using LibraryApp.Domain.Entities;

namespace LibraryApp.Application.Books;

public class BookLoanService : IBookLoanService
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<BookLoanService> _logger;

    public BookLoanService(IAppDbContext dbContext, ILogger<BookLoanService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<BorrowBookResponseDto> BorrowBookAsync(string bookId, int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify book exists
            var book = await _dbContext.Books
                .FirstOrDefaultAsync(b => b.Id == bookId, cancellationToken);

            if (book == null)
            {
                _logger.LogWarning("Attempt to borrow non-existent book: BookId={BookId}, UserId={UserId}", bookId, userId);
                throw new KeyNotFoundException($"Book with ID '{bookId}' not found.");
            }

            // Calculate available count
            var activeLoanCount = await _dbContext.BookLoans
                .CountAsync(bl => bl.BookId == bookId && bl.ReturnedDate == null, cancellationToken);

            var availableCount = book.NumberOfPieces - activeLoanCount;

            if (availableCount <= 0)
            {
                _logger.LogWarning("Attempt to borrow book with no available copies: BookId={BookId}, UserId={UserId}, AvailableCount={AvailableCount}", 
                    bookId, userId, availableCount);
                throw new InvalidOperationException($"No available copies of this book. Currently {activeLoanCount} out of {book.NumberOfPieces} are borrowed.");
            }

            // Create new loan - allow multiple loans per user for the same book
            var loan = new BookLoan
            {
                BookId = bookId,
                UserId = userId,
                BorrowedDate = DateTime.UtcNow,
                ReturnedDate = null
            };

            _dbContext.BookLoans.Add(loan);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} borrowed book {BookId} - LoanId={LoanId}", userId, bookId, loan.Id);

            return new BorrowBookResponseDto
            {
                LoanId = loan.Id,
                BookId = loan.BookId,
                UserId = loan.UserId,
                BorrowedDate = loan.BorrowedDate,
                Message = "Book borrowed successfully"
            };
        }
        catch (KeyNotFoundException)
        {
            throw;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while borrowing book: BookId={BookId}, UserId={UserId}", bookId, userId);
            throw;
        }
    }

    public async Task ReturnBookAsync(string bookId, int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Find oldest active loan for this user and book
            var loan = await _dbContext.BookLoans
                .Where(bl => bl.BookId == bookId 
                    && bl.UserId == userId 
                    && bl.ReturnedDate == null)
                .OrderBy(bl => bl.BorrowedDate)
                .FirstOrDefaultAsync(cancellationToken);

            if (loan == null)
            {
                _logger.LogWarning("Attempt to return book that user hasn't borrowed: BookId={BookId}, UserId={UserId}", bookId, userId);
                throw new InvalidOperationException($"You don't have an active loan for this book.");
            }

            // Verify book still exists (should always exist if loan exists, but safety check)
            var bookExists = await _dbContext.Books
                .AnyAsync(b => b.Id == bookId, cancellationToken);

            if (!bookExists)
            {
                _logger.LogError("Book not found for return operation: BookId={BookId}, UserId={UserId}", bookId, userId);
                throw new KeyNotFoundException($"Book with ID '{bookId}' not found.");
            }

            // Mark as returned
            loan.ReturnedDate = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} returned book {BookId} - LoanId={LoanId}", userId, bookId, loan.Id);
        }
        catch (KeyNotFoundException)
        {
            throw;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while returning book: BookId={BookId}, UserId={UserId}", bookId, userId);
            throw;
        }
    }

    public async Task<BookBorrowStatusDto> GetBookBorrowStatusAsync(string bookId, int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify book exists
            var book = await _dbContext.Books
                .FirstOrDefaultAsync(b => b.Id == bookId, cancellationToken);

            if (book == null)
            {
                throw new KeyNotFoundException($"Book with ID '{bookId}' not found.");
            }

            // Count active loans for this user
            var activeLoansByUser = await _dbContext.BookLoans
                .CountAsync(bl => bl.BookId == bookId 
                    && bl.UserId == userId 
                    && bl.ReturnedDate == null, cancellationToken);

            // Count total active loans
            var totalActiveLoans = await _dbContext.BookLoans
                .CountAsync(bl => bl.BookId == bookId && bl.ReturnedDate == null, cancellationToken);

            var availableCount = book.NumberOfPieces - totalActiveLoans;

            return new BookBorrowStatusDto
            {
                BookId = bookId,
                IsBorrowedByUser = activeLoansByUser > 0,
                ActiveLoanCount = activeLoansByUser,
                AvailableCount = Math.Max(0, availableCount) // Ensure non-negative
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting borrow status: BookId={BookId}, UserId={UserId}", bookId, userId);
            throw;
        }
    }

    public async Task<Dictionary<string, BookBorrowStatusDto>> GetBooksBorrowStatusAsync(List<string> bookIds, int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (bookIds == null || bookIds.Count == 0)
            {
                return new Dictionary<string, BookBorrowStatusDto>();
            }

            // Get all books in one query
            var books = await _dbContext.Books
                .Where(b => bookIds.Contains(b.Id))
                .ToListAsync(cancellationToken);

            // Get all active loans for these books in one query
            var activeLoans = await _dbContext.BookLoans
                .Where(bl => bookIds.Contains(bl.BookId) && bl.ReturnedDate == null)
                .GroupBy(bl => bl.BookId)
                .Select(g => new { BookId = g.Key, TotalCount = g.Count(), UserCount = g.Count(bl => bl.UserId == userId) })
                .ToListAsync(cancellationToken);

            var loanCountsByBook = activeLoans.ToDictionary(x => x.BookId, x => new { x.TotalCount, x.UserCount });

            var result = new Dictionary<string, BookBorrowStatusDto>();

            foreach (var book in books)
            {
                var loanInfo = loanCountsByBook.GetValueOrDefault(book.Id);
                var totalActiveLoans = loanInfo?.TotalCount ?? 0;
                var userActiveLoans = loanInfo?.UserCount ?? 0;

                result[book.Id] = new BookBorrowStatusDto
                {
                    BookId = book.Id,
                    IsBorrowedByUser = userActiveLoans > 0,
                    ActiveLoanCount = userActiveLoans,
                    AvailableCount = Math.Max(0, book.NumberOfPieces - totalActiveLoans)
                };
            }

            // Handle books that don't exist (return null status or skip)
            foreach (var bookId in bookIds)
            {
                if (!result.ContainsKey(bookId))
                {
                    // Book doesn't exist, you could throw or return null status
                    // For now, we'll skip it or you could add a null check
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting batch borrow status: BookIds={BookIds}, UserId={UserId}", 
                string.Join(", ", bookIds), userId);
            throw;
        }
    }

    public async Task<int> GetAvailableBookCountAsync(string bookId, CancellationToken cancellationToken = default)
    {
        try
        {
            var book = await _dbContext.Books
                .FirstOrDefaultAsync(b => b.Id == bookId, cancellationToken);

            if (book == null)
            {
                throw new KeyNotFoundException($"Book with ID '{bookId}' not found.");
            }

            var activeLoanCount = await _dbContext.BookLoans
                .CountAsync(bl => bl.BookId == bookId && bl.ReturnedDate == null, cancellationToken);

            return Math.Max(0, book.NumberOfPieces - activeLoanCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting available book count: BookId={BookId}", bookId);
            throw;
        }
    }

    public async Task<List<BorrowedBookDto>> GetUserBorrowedBooksAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get all active loans (not returned) for the user with book details
            var borrowedBooks = await _dbContext.BookLoans
                .Where(bl => bl.UserId == userId && bl.ReturnedDate == null)
                .Join(
                    _dbContext.Books,
                    loan => loan.BookId,
                    book => book.Id,
                    (loan, book) => new BorrowedBookDto
                    {
                        LoanId = loan.Id,
                        BookId = loan.BookId,
                        Name = book.Name,
                        Author = book.Author,
                        IssueYear = book.IssueYear,
                        ISBN = book.ISBN,
                        BorrowedDate = loan.BorrowedDate
                    })
                .OrderBy(b => b.BorrowedDate)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Retrieved {Count} borrowed books for user {UserId}", borrowedBooks.Count, userId);
            return borrowedBooks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting user borrowed books: UserId={UserId}", userId);
            throw;
        }
    }
}

