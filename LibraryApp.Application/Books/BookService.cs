using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LibraryApp.Application.Books.Dtos;
using LibraryApp.Application.Common.Dtos;
using LibraryApp.Application.Common.Interfaces;
using LibraryApp.Domain.Entities;

namespace LibraryApp.Application.Books;

public class BookService : IBookService
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<BookService> _logger;

    public BookService(IAppDbContext dbContext, ILogger<BookService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<PaginatedResponseDto<BookResponseDto>> GetAllBooksAsync(GetAllBooksRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _dbContext.Books.AsQueryable();

            // Filter out invalid records (empty Id, Name, Author, or ISBN - these are required fields)
            query = query.Where(b => !string.IsNullOrEmpty(b.Id) 
                && !string.IsNullOrEmpty(b.Name) 
                && !string.IsNullOrEmpty(b.Author) 
                && !string.IsNullOrEmpty(b.ISBN));

            // Apply filters (case-insensitive contains using LIKE)
            // SQLite LIKE operator is case-insensitive for ASCII characters by default
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                query = query.Where(b => EF.Functions.Like(b.Name, $"%{request.Name}%"));
            }

            if (!string.IsNullOrWhiteSpace(request.Author))
            {
                query = query.Where(b => EF.Functions.Like(b.Author, $"%{request.Author}%"));
            }

            if (!string.IsNullOrWhiteSpace(request.ISBN))
            {
                query = query.Where(b => EF.Functions.Like(b.ISBN, $"%{request.ISBN}%"));
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination
            var pageNumber = Math.Max(1, request.PageNumber);
            var pageSize = Math.Max(1, Math.Min(100, request.PageSize)); // Limit page size to 100
            var skip = (pageNumber - 1) * pageSize;

            var books = await query
                .OrderBy(b => b.Name)
                .ThenBy(b => b.Author)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var bookDtos = books.Select(b => new BookResponseDto
            {
                Id = b.Id,
                Name = b.Name,
                Author = b.Author,
                IssueYear = b.IssueYear,
                ISBN = b.ISBN,
                NumberOfPieces = b.NumberOfPieces
            }).ToList();

            return new PaginatedResponseDto<BookResponseDto>
            {
                Items = bookDtos,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving books");
            throw;
        }
    }

    public async Task<BookResponseDto> AddBookAsync(AddBookRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check for uniqueness: Name + Author + ISBN combination must be unique
            var existingBook = await _dbContext.Books
                .FirstOrDefaultAsync(b => 
                    b.Name == request.name && 
                    b.Author == request.author && 
                    b.ISBN == request.isbn, 
                    cancellationToken);

            if (existingBook != null)
            {
                _logger.LogWarning("Attempt to create duplicate book: Name={Name}, Author={Author}, ISBN={ISBN}", 
                    request.name, request.author, request.isbn);
                throw new InvalidOperationException(
                    $"A book with the combination of Name '{request.name}', Author '{request.author}', and ISBN '{request.isbn}' already exists.");
            }

            // Create new Book - Id will be auto-generated as GUID by the entity default value
            // We explicitly do NOT set Id to ensure it's auto-generated
            var book = new Book
            {
                // Id is NOT set - it will be auto-generated as Guid.NewGuid() by entity default
                Name = request.name,
                Author = request.author,
                IssueYear = request.issueyear,
                ISBN = request.isbn,
                NumberOfPieces = request.numberOfPieces
            };

            _dbContext.Books.Add(book);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Book created successfully: Id={Id}, Name={Name}, Author={Author}, ISBN={ISBN}", 
                book.Id, book.Name, book.Author, book.ISBN);

            return new BookResponseDto
            {
                Id = book.Id,
                Name = book.Name,
                Author = book.Author,
                IssueYear = book.IssueYear,
                ISBN = book.ISBN,
                NumberOfPieces = book.NumberOfPieces
            };
        }
        catch (InvalidOperationException)
        {
            throw; // Re-throw uniqueness violation
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while adding book: Name={Name}, Author={Author}, ISBN={ISBN}", 
                request.name, request.author, request.isbn);
            throw;
        }
    }

    public async Task<List<string>> GetBookNameSuggestionsAsync(string prefix, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(prefix))
            {
                return new List<string>();
            }

            var suggestions = await _dbContext.Books
                .Where(b => !string.IsNullOrEmpty(b.Name) 
                    && !string.IsNullOrEmpty(b.Id) 
                    && !string.IsNullOrEmpty(b.Author) 
                    && !string.IsNullOrEmpty(b.ISBN)
                    && EF.Functions.Like(b.Name, $"{prefix}%"))
                .Select(b => b.Name)
                .Distinct()
                .OrderBy(name => name)
                .Take(20) // Limit to 20 suggestions
                .ToListAsync(cancellationToken);

            return suggestions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving book name suggestions for prefix: {Prefix}", prefix);
            throw;
        }
    }

    public async Task<List<string>> GetAuthorSuggestionsAsync(string prefix, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(prefix))
            {
                return new List<string>();
            }

            var suggestions = await _dbContext.Books
                .Where(b => !string.IsNullOrEmpty(b.Name) 
                    && !string.IsNullOrEmpty(b.Id) 
                    && !string.IsNullOrEmpty(b.Author) 
                    && !string.IsNullOrEmpty(b.ISBN)
                    && EF.Functions.Like(b.Author, $"{prefix}%"))
                .Select(b => b.Author)
                .Distinct()
                .OrderBy(author => author)
                .Take(20) // Limit to 20 suggestions
                .ToListAsync(cancellationToken);

            return suggestions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving author suggestions for prefix: {Prefix}", prefix);
            throw;
        }
    }
}

