using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LibraryApp.Application.Books;
using LibraryApp.Application.Books.Dtos;
using LibraryApp.Application.Books.Validators;
using LibraryApp.Application.Common.Dtos;

namespace LibraryApp.Web.Controllers;

/// <summary>
/// API endpoints for Books - requires API Key authentication
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "ApiKey")]
public class BookApiController : ControllerBase
{
    private readonly IBookService _bookService;
    private readonly IBookLoanService _bookLoanService;
    private readonly ILogger<BookApiController> _logger;
    private readonly IValidator<BorrowBookRequestDto> _borrowValidator;
    private readonly IValidator<ReturnBookRequestDto> _returnValidator;

    public BookApiController(
        IBookService bookService, 
        IBookLoanService bookLoanService,
        ILogger<BookApiController> logger,
        IValidator<BorrowBookRequestDto> borrowValidator,
        IValidator<ReturnBookRequestDto> returnValidator)
    {
        _bookService = bookService;
        _bookLoanService = bookLoanService;
        _logger = logger;
        _borrowValidator = borrowValidator;
        _returnValidator = returnValidator;
    }

    /// <summary>
    /// Get all books with pagination and optional filters (Name, Author, ISBN) - API Key authentication only
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10, max: 100)</param>
    /// <param name="name">Optional filter by book name (case-insensitive contains)</param>
    /// <param name="author">Optional filter by author (case-insensitive contains)</param>
    /// <param name="isbn">Optional filter by ISBN (case-insensitive contains)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of books</returns>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponseDto<BookResponseDto>>> GetAllBooks(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? name = null,
        [FromQuery] string? author = null,
        [FromQuery] string? isbn = null,
        CancellationToken cancellationToken = default)
    {
        // CRITICAL: Verify that authentication came from ApiKey scheme, not Cookie
        // When both Cookie and ApiKey schemes are present, User.Identity.AuthenticationType
        // reflects the PRIMARY identity (usually Cookie due to DefaultScheme).
        // We need to check ALL identities to find ApiKey
        var hasApiKeyAuth = User.Identities.Any(id => id.AuthenticationType == "ApiKey" && id.IsAuthenticated);
        if (!hasApiKeyAuth)
        {
            _logger.LogWarning("BookApiController.GetAllBooks: No ApiKey authentication found. Returning 401");
            return Unauthorized(new { message = "API key authentication required" });
        }
        
        _logger.LogInformation("BookApiController.GetAllBooks called - PageNumber: {PageNumber}, PageSize: {PageSize}, Name: {Name}, Author: {Author}, ISBN: {ISBN}",
            pageNumber, pageSize, name, author, isbn);

        var request = new GetAllBooksRequestDto
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Name = name,
            Author = author,
            ISBN = isbn
        };

        var result = await _bookService.GetAllBooksAsync(request, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Add a new book via API Key authentication. Name + Author + ISBN combination must be unique. ISBN must be valid (ISBN-13).
    /// </summary>
    /// <param name="request">Book creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created book</returns>
    [HttpPost]
    public async Task<ActionResult<BookResponseDto>> AddNewBook(
        [FromBody] AddBookRequestDto request,
        CancellationToken cancellationToken = default)
    {
        // CRITICAL: Verify that authentication came from ApiKey scheme, not Cookie
        // When both Cookie and ApiKey schemes are present, User.Identity.AuthenticationType
        // reflects the PRIMARY identity (usually Cookie due to DefaultScheme).
        // We need to check ALL identities to find ApiKey
        var hasApiKeyAuth = User.Identities.Any(id => id.AuthenticationType == "ApiKey" && id.IsAuthenticated);
        if (!hasApiKeyAuth)
        {
            _logger.LogWarning("BookApiController.AddNewBook: No ApiKey authentication found. Returning 401");
            return Unauthorized(new { message = "API key authentication required" });
        }
        
        _logger.LogInformation("BookApiController.AddNewBook called - Name: {Name}, Author: {Author}, IssueYear: {IssueYear}, ISBN: {ISBN}, NumberOfPieces: {NumberOfPieces}",
            request.name, request.author, request.issueyear, request.isbn, request.numberOfPieces);

        try
        {
            var result = await _bookService.AddBookAsync(request, cancellationToken);

            _logger.LogInformation("Book created successfully via API - Id: {Id}", result.Id);

            return CreatedAtAction(
                nameof(GetAllBooks),
                new { id = result.Id },
                result);
        }
        catch (InvalidOperationException ex)
        {
            // This catches the uniqueness violation from BookService
            _logger.LogWarning("Failed to create book via API - Duplicate: {Message}", ex.Message);
            return Conflict(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            // This catches FluentValidation errors
            _logger.LogWarning("Validation failed for book creation via API: {Errors}", string.Join(", ", ex.Errors.Select(e => e.ErrorMessage)));
            return BadRequest(new { message = "Validation failed", errors = ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating book via API");
            return StatusCode(500, new { message = "An error occurred while creating the book" });
        }
    }

    /// <summary>
    /// Borrow a book via API Key authentication. Returns 409 Conflict if no copies are available.
    /// </summary>
    /// <param name="bookId">Book ID to borrow</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Borrow operation result</returns>
    [HttpPost("{bookId}/borrow")]
    public async Task<ActionResult<BorrowBookResponseDto>> BorrowBook(
        string bookId,
        CancellationToken cancellationToken = default)
    {
        // CRITICAL: Verify that authentication came from ApiKey scheme, not Cookie
        var hasApiKeyAuth = User.Identities.Any(id => id.AuthenticationType == "ApiKey" && id.IsAuthenticated);
        if (!hasApiKeyAuth)
        {
            _logger.LogWarning("BookApiController.BorrowBook: No ApiKey authentication found. Returning 401");
            return Unauthorized(new { message = "API key authentication required" });
        }

        if (string.IsNullOrWhiteSpace(bookId))
        {
            return BadRequest(new { message = "BookId is required" });
        }

        var userIdClaim = User.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            _logger.LogWarning("BookApiController.BorrowBook: User not authenticated or invalid UserId claim");
            return Unauthorized(new { message = "User authentication required" });
        }

        var request = new BorrowBookRequestDto { BookId = bookId };
        var validationResult = await _borrowValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("BookApiController.BorrowBook: Validation failed. Errors: {Errors}",
                string.Join("; ", validationResult.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")));
            return BadRequest(new { message = "Validation failed", errors = validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }) });
        }

        try
        {
            var result = await _bookLoanService.BorrowBookAsync(bookId, userId, cancellationToken);
            _logger.LogInformation("Book borrowed successfully via API - BookId: {BookId}, UserId: {UserId}, LoanId: {LoanId}", 
                bookId, userId, result.LoanId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("BookApiController.BorrowBook: Book not found - BookId: {BookId}, UserId: {UserId}", bookId, userId);
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("BookApiController.BorrowBook: No available copies - BookId: {BookId}, UserId: {UserId}, Message: {Message}", 
                bookId, userId, ex.Message);
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while borrowing book via API - BookId: {BookId}, UserId: {UserId}", bookId, userId);
            return StatusCode(500, new { message = "An error occurred while borrowing the book" });
        }
    }

    /// <summary>
    /// Return a book via API Key authentication. Returns 400 BadRequest if user doesn't have an active loan.
    /// </summary>
    /// <param name="bookId">Book ID to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Return operation result</returns>
    [HttpPost("{bookId}/return")]
    public async Task<ActionResult> ReturnBook(
        string bookId,
        CancellationToken cancellationToken = default)
    {
        // CRITICAL: Verify that authentication came from ApiKey scheme, not Cookie
        var hasApiKeyAuth = User.Identities.Any(id => id.AuthenticationType == "ApiKey" && id.IsAuthenticated);
        if (!hasApiKeyAuth)
        {
            _logger.LogWarning("BookApiController.ReturnBook: No ApiKey authentication found. Returning 401");
            return Unauthorized(new { message = "API key authentication required" });
        }

        if (string.IsNullOrWhiteSpace(bookId))
        {
            return BadRequest(new { message = "BookId is required" });
        }

        var userIdClaim = User.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            _logger.LogWarning("BookApiController.ReturnBook: User not authenticated or invalid UserId claim");
            return Unauthorized(new { message = "User authentication required" });
        }

        var request = new ReturnBookRequestDto { BookId = bookId };
        var validationResult = await _returnValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("BookApiController.ReturnBook: Validation failed. Errors: {Errors}",
                string.Join("; ", validationResult.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")));
            return BadRequest(new { message = "Validation failed", errors = validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }) });
        }

        try
        {
            await _bookLoanService.ReturnBookAsync(bookId, userId, cancellationToken);
            _logger.LogInformation("Book returned successfully via API - BookId: {BookId}, UserId: {UserId}", bookId, userId);
            return Ok(new { message = "Book returned successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("BookApiController.ReturnBook: Book not found - BookId: {BookId}, UserId: {UserId}", bookId, userId);
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("BookApiController.ReturnBook: User doesn't have active loan - BookId: {BookId}, UserId: {UserId}, Message: {Message}", 
                bookId, userId, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while returning book via API - BookId: {BookId}, UserId: {UserId}", bookId, userId);
            return StatusCode(500, new { message = "An error occurred while returning the book" });
        }
    }
}
