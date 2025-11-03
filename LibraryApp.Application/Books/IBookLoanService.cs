using LibraryApp.Application.Books.Dtos;

namespace LibraryApp.Application.Books;

public interface IBookLoanService
{
    Task<BorrowBookResponseDto> BorrowBookAsync(string bookId, int userId, CancellationToken cancellationToken = default);
    Task ReturnBookAsync(string bookId, int userId, CancellationToken cancellationToken = default);
    Task<BookBorrowStatusDto> GetBookBorrowStatusAsync(string bookId, int userId, CancellationToken cancellationToken = default);
    Task<Dictionary<string, BookBorrowStatusDto>> GetBooksBorrowStatusAsync(List<string> bookIds, int userId, CancellationToken cancellationToken = default);
    Task<int> GetAvailableBookCountAsync(string bookId, CancellationToken cancellationToken = default);
    Task<List<BorrowedBookDto>> GetUserBorrowedBooksAsync(int userId, CancellationToken cancellationToken = default);
}

