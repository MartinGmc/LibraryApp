using LibraryApp.Application.Books;
using LibraryApp.Domain.Entities;
using LibraryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryApp.UnitTests.Books;

public class BookLoanServiceTests
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<BookLoanService> _logger;

    public BookLoanServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
        
        _dbContext = new AppDbContext(options);
        _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<BookLoanService>.Instance;
    }

    [Fact]
    public async Task BorrowBookAsync_AvailableCopies_BorrowsSuccessfully()
    {
        // Arrange
        _dbContext.Books.Add(new Book { Id = "book1", Name = "Test Book", Author = "Author", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 5 });
        await _dbContext.SaveChangesAsync();

        var service = new BookLoanService(_dbContext, _logger);

        // Act
        var result = await service.BorrowBookAsync("book1", 1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("book1", result.BookId);
        Assert.Equal(1, result.UserId);
        Assert.NotEmpty(result.LoanId);
        Assert.Equal("Book borrowed successfully", result.Message);
    }

    [Fact]
    public async Task BorrowBookAsync_AllCopiesBorrowed_ThrowsInvalidOperationException()
    {
        // Arrange
        _dbContext.Books.Add(new Book { Id = "book1", Name = "Test Book", Author = "Author", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 2 });
        _dbContext.BookLoans.Add(new BookLoan { BookId = "book1", UserId = 2, BorrowedDate = DateTime.UtcNow });
        _dbContext.BookLoans.Add(new BookLoan { BookId = "book1", UserId = 3, BorrowedDate = DateTime.UtcNow });
        await _dbContext.SaveChangesAsync();

        var service = new BookLoanService(_dbContext, _logger);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.BorrowBookAsync("book1", 1));
    }

    [Fact]
    public async Task BorrowBookAsync_NonExistentBook_ThrowsKeyNotFoundException()
    {
        // Arrange
        var service = new BookLoanService(_dbContext, _logger);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.BorrowBookAsync("nonexistent", 1));
    }

    [Fact]
    public async Task ReturnBookAsync_ActiveLoan_ReturnsSuccessfully()
    {
        // Arrange
        _dbContext.Books.Add(new Book { Id = "book1", Name = "Test Book", Author = "Author", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 5 });
        var loan = new BookLoan { Id = "loan1", BookId = "book1", UserId = 1, BorrowedDate = DateTime.UtcNow.AddDays(-5) };
        _dbContext.BookLoans.Add(loan);
        await _dbContext.SaveChangesAsync();

        var service = new BookLoanService(_dbContext, _logger);

        // Act
        await service.ReturnBookAsync("book1", 1);

        // Assert
        Assert.NotNull(loan.ReturnedDate);
    }

    [Fact]
    public async Task ReturnBookAsync_NoActiveLoan_ThrowsInvalidOperationException()
    {
        // Arrange
        _dbContext.Books.Add(new Book { Id = "book1", Name = "Test Book", Author = "Author", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 5 });
        await _dbContext.SaveChangesAsync();

        var service = new BookLoanService(_dbContext, _logger);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReturnBookAsync("book1", 1));
    }

    [Fact]
    public async Task GetBookBorrowStatusAsync_BookExists_ReturnsCorrectStatus()
    {
        // Arrange
        _dbContext.Books.Add(new Book { Id = "book1", Name = "Test Book", Author = "Author", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 5 });
        _dbContext.BookLoans.Add(new BookLoan { BookId = "book1", UserId = 1, BorrowedDate = DateTime.UtcNow });
        _dbContext.BookLoans.Add(new BookLoan { BookId = "book1", UserId = 2, BorrowedDate = DateTime.UtcNow });
        await _dbContext.SaveChangesAsync();

        var service = new BookLoanService(_dbContext, _logger);

        // Act
        var result = await service.GetBookBorrowStatusAsync("book1", 1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("book1", result.BookId);
        Assert.True(result.IsBorrowedByUser);
        Assert.Equal(1, result.ActiveLoanCount);
        Assert.Equal(3, result.AvailableCount);
    }

    [Fact]
    public async Task GetBookBorrowStatusAsync_UserNotBorrowed_ReturnsCorrectStatus()
    {
        // Arrange
        _dbContext.Books.Add(new Book { Id = "book1", Name = "Test Book", Author = "Author", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 5 });
        _dbContext.BookLoans.Add(new BookLoan { BookId = "book1", UserId = 2, BorrowedDate = DateTime.UtcNow });
        await _dbContext.SaveChangesAsync();

        var service = new BookLoanService(_dbContext, _logger);

        // Act
        var result = await service.GetBookBorrowStatusAsync("book1", 1);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsBorrowedByUser);
        Assert.Equal(0, result.ActiveLoanCount);
        Assert.Equal(4, result.AvailableCount);
    }

    [Fact]
    public async Task GetBookBorrowStatusAsync_NonExistentBook_ThrowsKeyNotFoundException()
    {
        // Arrange
        var service = new BookLoanService(_dbContext, _logger);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetBookBorrowStatusAsync("nonexistent", 1));
    }

    [Fact]
    public async Task GetAvailableBookCountAsync_BookExists_ReturnsCorrectCount()
    {
        // Arrange
        _dbContext.Books.Add(new Book { Id = "book1", Name = "Test Book", Author = "Author", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 5 });
        _dbContext.BookLoans.Add(new BookLoan { BookId = "book1", UserId = 1, BorrowedDate = DateTime.UtcNow });
        _dbContext.BookLoans.Add(new BookLoan { BookId = "book1", UserId = 2, BorrowedDate = DateTime.UtcNow });
        await _dbContext.SaveChangesAsync();

        var service = new BookLoanService(_dbContext, _logger);

        // Act
        var result = await service.GetAvailableBookCountAsync("book1");

        // Assert
        Assert.Equal(3, result);
    }

    [Fact]
    public async Task GetAvailableBookCountAsync_NonExistentBook_ThrowsKeyNotFoundException()
    {
        // Arrange
        var service = new BookLoanService(_dbContext, _logger);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetAvailableBookCountAsync("nonexistent"));
    }

    [Fact]
    public async Task GetUserBorrowedBooksAsync_UserHasLoans_ReturnsBooks()
    {
        // Arrange
        _dbContext.Books.Add(new Book { Id = "book1", Name = "Book 1", Author = "Author 1", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 5 });
        _dbContext.Books.Add(new Book { Id = "book2", Name = "Book 2", Author = "Author 2", ISBN = "9781566199094", IssueYear = 2021, NumberOfPieces = 3 });
        _dbContext.BookLoans.Add(new BookLoan { Id = "loan1", BookId = "book1", UserId = 1, BorrowedDate = DateTime.UtcNow.AddDays(-5) });
        _dbContext.BookLoans.Add(new BookLoan { Id = "loan2", BookId = "book2", UserId = 1, BorrowedDate = DateTime.UtcNow.AddDays(-2) });
        await _dbContext.SaveChangesAsync();

        var service = new BookLoanService(_dbContext, _logger);

        // Act
        var result = await service.GetUserBorrowedBooksAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Book 1", result[0].Name);
        Assert.Equal("Book 2", result[1].Name);
    }

    [Fact]
    public async Task GetBooksBorrowStatusAsync_NullList_ReturnsEmptyDictionary()
    {
        // Arrange
        var service = new BookLoanService(_dbContext, _logger);

        // Act
        var result = await service.GetBooksBorrowStatusAsync(null!, 1);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetBooksBorrowStatusAsync_EmptyList_ReturnsEmptyDictionary()
    {
        // Arrange
        var service = new BookLoanService(_dbContext, _logger);

        // Act
        var result = await service.GetBooksBorrowStatusAsync(new List<string>(), 1);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetBooksBorrowStatusAsync_MultipleBooks_ReturnsCorrectStatus()
    {
        // Arrange
        _dbContext.Books.Add(new Book { Id = "book1", Name = "Book 1", Author = "Author 1", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 5 });
        _dbContext.Books.Add(new Book { Id = "book2", Name = "Book 2", Author = "Author 2", ISBN = "9781566199094", IssueYear = 2021, NumberOfPieces = 3 });
        _dbContext.BookLoans.Add(new BookLoan { BookId = "book1", UserId = 1, BorrowedDate = DateTime.UtcNow });
        _dbContext.BookLoans.Add(new BookLoan { BookId = "book1", UserId = 2, BorrowedDate = DateTime.UtcNow });
        await _dbContext.SaveChangesAsync();

        var service = new BookLoanService(_dbContext, _logger);

        // Act
        var result = await service.GetBooksBorrowStatusAsync(new List<string> { "book1", "book2" }, 1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.True(result.ContainsKey("book1"));
        Assert.True(result.ContainsKey("book2"));
        Assert.True(result["book1"].IsBorrowedByUser);
        Assert.Equal(1, result["book1"].ActiveLoanCount);
        Assert.Equal(3, result["book1"].AvailableCount);
        Assert.False(result["book2"].IsBorrowedByUser);
        Assert.Equal(0, result["book2"].ActiveLoanCount);
        Assert.Equal(3, result["book2"].AvailableCount);
    }

    [Fact]
    public async Task GetBooksBorrowStatusAsync_MixedLoanStatus_ReturnsCorrectCounts()
    {
        // Arrange
        _dbContext.Books.Add(new Book { Id = "book1", Name = "Book 1", Author = "Author 1", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 3 });
        _dbContext.BookLoans.Add(new BookLoan { BookId = "book1", UserId = 1, BorrowedDate = DateTime.UtcNow });
        _dbContext.BookLoans.Add(new BookLoan { BookId = "book1", UserId = 1, BorrowedDate = DateTime.UtcNow.AddDays(-1) });
        _dbContext.BookLoans.Add(new BookLoan { BookId = "book1", UserId = 2, BorrowedDate = DateTime.UtcNow });
        await _dbContext.SaveChangesAsync();

        var service = new BookLoanService(_dbContext, _logger);

        // Act
        var result = await service.GetBooksBorrowStatusAsync(new List<string> { "book1" }, 1);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.True(result["book1"].IsBorrowedByUser);
        Assert.Equal(2, result["book1"].ActiveLoanCount);
        Assert.Equal(0, result["book1"].AvailableCount);
    }

    [Fact]
    public async Task GetBooksBorrowStatusAsync_NonExistentBooks_SkipsThem()
    {
        // Arrange
        _dbContext.Books.Add(new Book { Id = "book1", Name = "Book 1", Author = "Author 1", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 5 });
        await _dbContext.SaveChangesAsync();

        var service = new BookLoanService(_dbContext, _logger);

        // Act
        var result = await service.GetBooksBorrowStatusAsync(new List<string> { "book1", "nonexistent" }, 1);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.True(result.ContainsKey("book1"));
        Assert.False(result.ContainsKey("nonexistent"));
    }
}
