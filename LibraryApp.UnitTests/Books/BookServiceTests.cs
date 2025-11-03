using LibraryApp.Application.Books;
using LibraryApp.Application.Books.Dtos;
using LibraryApp.Application.Common.Interfaces;
using LibraryApp.Domain.Entities;
using LibraryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryApp.UnitTests.Books;

public class BookServiceTests
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<BookService> _logger;

    public BookServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
        
        _dbContext = new AppDbContext(options);
        _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<BookService>.Instance;
    }

    [Fact]
    public async Task GetAllBooksAsync_NoFilters_ReturnsAllBooks()
    {
        // Arrange
        _dbContext.Books.Add(new Book { Id = "1", Name = "Book 1", Author = "Author 1", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 5 });
        _dbContext.Books.Add(new Book { Id = "2", Name = "Book 2", Author = "Author 2", ISBN = "9781566199094", IssueYear = 2021, NumberOfPieces = 3 });
        await _dbContext.SaveChangesAsync();

        var service = new BookService(_dbContext, _logger);
        var request = new GetAllBooksRequestDto { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await service.GetAllBooksAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task GetAllBooksAsync_WithNameFilter_ReturnsFilteredBooks()
    {
        // Arrange
        _dbContext.Books.Add(new Book { Id = "1", Name = "Book 1", Author = "Author 1", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 5 });
        _dbContext.Books.Add(new Book { Id = "2", Name = "Book 2", Author = "Author 2", ISBN = "9781566199094", IssueYear = 2021, NumberOfPieces = 3 });
        await _dbContext.SaveChangesAsync();

        var service = new BookService(_dbContext, _logger);
        var request = new GetAllBooksRequestDto { PageNumber = 1, PageSize = 10, Name = "Book 1" };

        // Act
        var result = await service.GetAllBooksAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("Book 1", result.Items[0].Name);
    }

    [Fact]
    public async Task GetAllBooksAsync_WithAuthorFilter_ReturnsFilteredBooks()
    {
        // Arrange
        _dbContext.Books.Add(new Book { Id = "1", Name = "Book 1", Author = "Author 1", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 5 });
        _dbContext.Books.Add(new Book { Id = "2", Name = "Book 2", Author = "Author 2", ISBN = "9781566199094", IssueYear = 2021, NumberOfPieces = 3 });
        await _dbContext.SaveChangesAsync();

        var service = new BookService(_dbContext, _logger);
        var request = new GetAllBooksRequestDto { PageNumber = 1, PageSize = 10, Author = "Author 1" };

        // Act
        var result = await service.GetAllBooksAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("Author 1", result.Items[0].Author);
    }

    [Fact]
    public async Task GetAllBooksAsync_WithIsbnFilter_ReturnsFilteredBooks()
    {
        // Arrange
        _dbContext.Books.Add(new Book { Id = "1", Name = "Book 1", Author = "Author 1", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 5 });
        _dbContext.Books.Add(new Book { Id = "2", Name = "Book 2", Author = "Author 2", ISBN = "9781566199094", IssueYear = 2021, NumberOfPieces = 3 });
        await _dbContext.SaveChangesAsync();

        var service = new BookService(_dbContext, _logger);
        var request = new GetAllBooksRequestDto { PageNumber = 1, PageSize = 10, ISBN = "9781566199094" };

        // Act
        var result = await service.GetAllBooksAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("9781566199094", result.Items[0].ISBN);
    }

    [Fact]
    public async Task GetAllBooksAsync_WithPagination_ReturnsPaginatedResults()
    {
        // Arrange
        for (int i = 1; i <= 3; i++)
        {
            _dbContext.Books.Add(new Book { Id = i.ToString(), Name = $"Book {i}", Author = "Author", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 5 });
        }
        await _dbContext.SaveChangesAsync();

        var service = new BookService(_dbContext, _logger);
        var request = new GetAllBooksRequestDto { PageNumber = 1, PageSize = 2 };

        // Act
        var result = await service.GetAllBooksAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(2, result.PageSize);
    }

    [Fact]
    public async Task GetAllBooksAsync_PageNumberZero_NormalizesToPage1()
    {
        // Arrange
        _dbContext.Books.Add(new Book { Id = "1", Name = "Book 1", Author = "Author", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 5 });
        await _dbContext.SaveChangesAsync();

        var service = new BookService(_dbContext, _logger);
        var request = new GetAllBooksRequestDto { PageNumber = 0, PageSize = 10 };

        // Act
        var result = await service.GetAllBooksAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.PageNumber);
    }

    [Fact]
    public async Task GetAllBooksAsync_PageSizeZero_NormalizesToPageSize1()
    {
        // Arrange
        _dbContext.Books.Add(new Book { Id = "1", Name = "Book 1", Author = "Author", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 5 });
        await _dbContext.SaveChangesAsync();

        var service = new BookService(_dbContext, _logger);
        var request = new GetAllBooksRequestDto { PageNumber = 1, PageSize = 0 };

        // Act
        var result = await service.GetAllBooksAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.PageSize);
    }

    [Fact]
    public async Task GetAllBooksAsync_PageSizeExceedsMax_NormalizesTo100()
    {
        // Arrange
        for (int i = 1; i <= 150; i++)
        {
            _dbContext.Books.Add(new Book { Id = i.ToString(), Name = $"Book {i}", Author = "Author", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 5 });
        }
        await _dbContext.SaveChangesAsync();

        var service = new BookService(_dbContext, _logger);
        var request = new GetAllBooksRequestDto { PageNumber = 1, PageSize = 200 };

        // Act
        var result = await service.GetAllBooksAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(100, result.PageSize);
        Assert.Equal(100, result.Items.Count);
    }

    [Fact]
    public async Task AddBookAsync_ValidBook_ReturnsCreatedBook()
    {
        // Arrange
        var service = new BookService(_dbContext, _logger);
        var request = new AddBookRequestDto
        {
            name = "Test Book",
            author = "Test Author",
            issueyear = 2023,
            isbn = "9780131101630",
            numberOfPieces = 5
        };

        // Act
        var result = await service.AddBookAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Id);
        Assert.Equal("Test Book", result.Name);
        Assert.Equal("Test Author", result.Author);
        Assert.Equal(2023, result.IssueYear);
        Assert.Equal("9780131101630", result.ISBN);
        Assert.Equal(5, result.NumberOfPieces);
    }

    [Fact]
    public async Task AddBookAsync_DuplicateBook_ThrowsInvalidOperationException()
    {
        // Arrange
        _dbContext.Books.Add(new Book { Id = "1", Name = "Test Book", Author = "Test Author", ISBN = "9780131101630", IssueYear = 2023, NumberOfPieces = 5 });
        await _dbContext.SaveChangesAsync();

        var service = new BookService(_dbContext, _logger);
        var request = new AddBookRequestDto
        {
            name = "Test Book",
            author = "Test Author",
            issueyear = 2023,
            isbn = "9780131101630",
            numberOfPieces = 5
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddBookAsync(request));
    }

    [Fact]
    public async Task GetBookNameSuggestionsAsync_WithPrefix_ReturnsSuggestions()
    {
        // Arrange
        _dbContext.Books.Add(new Book { Id = "1", Name = "Book One", Author = "Author 1", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 5 });
        _dbContext.Books.Add(new Book { Id = "2", Name = "Book Two", Author = "Author 2", ISBN = "9781566199094", IssueYear = 2021, NumberOfPieces = 3 });
        _dbContext.Books.Add(new Book { Id = "3", Name = "Article Three", Author = "Author 3", ISBN = "9781234567890", IssueYear = 2022, NumberOfPieces = 10 });
        await _dbContext.SaveChangesAsync();

        var service = new BookService(_dbContext, _logger);

        // Act
        var result = await service.GetBookNameSuggestionsAsync("Book");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains("Book One", result);
        Assert.Contains("Book Two", result);
    }

    [Fact]
    public async Task GetBookNameSuggestionsAsync_EmptyPrefix_ReturnsEmptyList()
    {
        // Arrange
        _dbContext.Books.Add(new Book { Id = "1", Name = "Book One", Author = "Author 1", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 5 });
        await _dbContext.SaveChangesAsync();

        var service = new BookService(_dbContext, _logger);

        // Act
        var result = await service.GetBookNameSuggestionsAsync("");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAuthorSuggestionsAsync_WithPrefix_ReturnsSuggestions()
    {
        // Arrange
        _dbContext.Books.Add(new Book { Id = "1", Name = "Book 1", Author = "John Doe", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 5 });
        _dbContext.Books.Add(new Book { Id = "2", Name = "Book 2", Author = "John Smith", ISBN = "9781566199094", IssueYear = 2021, NumberOfPieces = 3 });
        _dbContext.Books.Add(new Book { Id = "3", Name = "Book 3", Author = "Jane Doe", ISBN = "9781234567890", IssueYear = 2022, NumberOfPieces = 10 });
        await _dbContext.SaveChangesAsync();

        var service = new BookService(_dbContext, _logger);

        // Act
        var result = await service.GetAuthorSuggestionsAsync("John");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains("John Doe", result);
        Assert.Contains("John Smith", result);
    }
}
