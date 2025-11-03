using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LibraryApp.Application.Books;
using LibraryApp.Application.Books.Dtos;
using LibraryApp.Application.Books.Services;
using LibraryApp.Application.Books.Validators;
using LibraryApp.Application.Common.Dtos;

namespace LibraryApp.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookController : ControllerBase
{
    private readonly IBookService _bookService;
    private readonly IBookLoanService _bookLoanService;
    private readonly ILogger<BookController> _logger;
    private readonly IValidator<AddBookRequestDto> _validator;
    private readonly IValidator<BorrowBookRequestDto> _borrowValidator;
    private readonly IValidator<ReturnBookRequestDto> _returnValidator;
    private readonly IIsbnValidationService _isbnValidationService;

    public BookController(
        IBookService bookService,
        IBookLoanService bookLoanService,
        ILogger<BookController> logger,
        IValidator<AddBookRequestDto> validator,
        IValidator<BorrowBookRequestDto> borrowValidator,
        IValidator<ReturnBookRequestDto> returnValidator,
        IIsbnValidationService isbnValidationService)
    {
        _bookService = bookService;
        _bookLoanService = bookLoanService;
        _logger = logger;
        _validator = validator;
        _borrowValidator = borrowValidator;
        _returnValidator = returnValidator;
        _isbnValidationService = isbnValidationService;
    }

    /// <summary>
    /// Get all books with pagination and optional filters (Name, Author, ISBN) - Cookie authentication only
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10, max: 100)</param>
    /// <param name="name">Optional filter by book name (case-insensitive contains)</param>
    /// <param name="author">Optional filter by author (case-insensitive contains)</param>
    /// <param name="isbn">Optional filter by ISBN (case-insensitive contains)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of books</returns>
    [HttpGet]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public async Task<ActionResult<PaginatedResponseDto<BookResponseDto>>> GetAllBooks(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? name = null,
        [FromQuery] string? author = null,
        [FromQuery] string? isbn = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetAllBooks called - PageNumber: {PageNumber}, PageSize: {PageSize}, Name: {Name}, Author: {Author}, ISBN: {ISBN}",
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
    /// Add a new book. Name + Author + ISBN combination must be unique. ISBN must be valid (ISBN-13).
    /// </summary>
    /// <param name="request">Book creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created book</returns>
    [HttpPost]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public async Task<ActionResult<BookResponseDto>> AddNewBook(
        [FromBody] AddBookRequestDto? request,
        CancellationToken cancellationToken = default)
    {
        // Log immediately - this will help us see if the method is being called
        _logger.LogInformation("=== AddNewBook METHOD ENTERED ===");
        _logger.LogInformation("AddNewBook - Request is null: {IsNull}, ModelState.IsValid: {IsValid}, ModelState.ErrorCount: {ErrorCount}",
            request == null, 
            ModelState.IsValid,
            ModelState.ErrorCount);
        
        // Log authentication info
        var isAuthenticated = HttpContext.User?.Identity?.IsAuthenticated ?? false;
        var authType = HttpContext.User?.Identity?.AuthenticationType ?? "None";
        var userName = HttpContext.User?.Identity?.Name ?? "Anonymous";
        _logger.LogInformation("AddNewBook - Auth: IsAuthenticated: {IsAuthenticated}, AuthType: {AuthType}, UserName: {UserName}",
            isAuthenticated, authType, userName);
        
        // Log ModelState details if invalid
        if (!ModelState.IsValid)
        {
            var modelStateDetails = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(e => 
                    $"Key: '{x.Key}', Error: '{e.ErrorMessage}', Exception: {e.Exception?.Message ?? "None"}"))
                .ToList();
            
            _logger.LogWarning("AddNewBook - ModelState Invalid. Errors ({Count}): {Errors}",
                modelStateDetails.Count,
                string.Join(" | ", modelStateDetails));
        }
        
        // Log request data if available
        if (request != null)
        {
            _logger.LogInformation("AddNewBook - Request data: Name: '{Name}', Author: '{Author}', IssueYear: {IssueYear}, ISBN: '{ISBN}', NumberOfPieces: {NumberOfPieces}",
                request.name, request.author, request.issueyear, request.isbn, request.numberOfPieces);
        }
        else
        {
            _logger.LogWarning("AddNewBook - Request is NULL");
        }
        
        // Check if request is null (model binding failed completely)
        if (request == null)
        {
            _logger.LogWarning("AddNewBook: Request body is null or could not be deserialized");
            
            // Try to read the raw request body for debugging
            try
            {
                HttpContext.Request.EnableBuffering();
                HttpContext.Request.Body.Position = 0;
                using var reader = new StreamReader(HttpContext.Request.Body, leaveOpen: true);
                var rawBody = await reader.ReadToEndAsync();
                HttpContext.Request.Body.Position = 0;
                
                _logger.LogWarning("AddNewBook: Raw request body: {RequestBody}", rawBody);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AddNewBook: Could not read raw request body");
            }
            
            return BadRequest(new { message = "Request body is required and must be valid JSON" });
        }
        
        // Perform FluentValidation
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("AddNewBook: FluentValidation failed. Errors: {Errors}", 
                string.Join("; ", validationResult.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")));
            return BadRequest(new { message = "Validation failed", errors = validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }) });
        }
        
        // Check ModelState first
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(e => $"{x.Key}: {e.ErrorMessage}"))
                .ToList();
            
            _logger.LogWarning("ModelState is invalid. Errors: {Errors}", string.Join("; ", errors));
            
            // Return a more detailed error response
            var errorResponse = new
            {
                message = "Model validation failed",
                errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .SelectMany(x => x.Value!.Errors.Select(e => new { property = x.Key, error = e.ErrorMessage }))
            };
            
            return BadRequest(errorResponse);
        }

        // At this point, request is guaranteed to be non-null
        _logger.LogInformation("AddNewBook called - Name: {Name}, Author: {Author}, IssueYear: {IssueYear}, ISBN: {ISBN}, NumberOfPieces: {NumberOfPieces}",
            request.name, request.author, request.issueyear, request.isbn, request.numberOfPieces);

        try
        {
            var result = await _bookService.AddBookAsync(request, cancellationToken);

            _logger.LogInformation("Book created successfully - Id: {Id}", result.Id);

            return CreatedAtAction(
                nameof(GetAllBooks),
                new { id = result.Id },
                result);
        }
        catch (InvalidOperationException ex)
        {
            // This catches the uniqueness violation from BookService
            _logger.LogWarning("Failed to create book - Duplicate: {Message}", ex.Message);
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating book");
            return StatusCode(500, new { message = "An error occurred while creating the book" });
        }
    }

    /// <summary>
    /// Validate ISBN-13 in real-time. Returns true if valid, false otherwise.
    /// </summary>
    /// <param name="isbn">ISBN to validate (with or without hyphens)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result (true = valid, false = invalid)</returns>
    [HttpGet("validate-isbn")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public ActionResult<bool> ValidateIsbn(
        [FromQuery] string isbn,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(isbn))
        {
            return Ok(false);
        }

        var isValid = _isbnValidationService.ValidateIsbn13(isbn);
        return Ok(isValid);
    }

    /// <summary>
    /// Get book name suggestions that start with the given prefix. Returns up to 20 distinct book names.
    /// </summary>
    /// <param name="prefix">Prefix to search for (minimum 4 characters)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of book name suggestions</returns>
    [HttpGet("name-suggestions")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public async Task<ActionResult<List<string>>> GetBookNameSuggestions(
        [FromQuery] string prefix,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prefix) || prefix.Length < 4)
        {
            return Ok(new List<string>());
        }

        _logger.LogInformation("GetBookNameSuggestions called with prefix: {Prefix}", prefix);

        var suggestions = await _bookService.GetBookNameSuggestionsAsync(prefix, cancellationToken);
        return Ok(suggestions);
    }

    /// <summary>
    /// Get author name suggestions that start with the given prefix. Returns up to 20 distinct author names.
    /// </summary>
    /// <param name="prefix">Prefix to search for (minimum 4 characters)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of author name suggestions</returns>
    [HttpGet("author-suggestions")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public async Task<ActionResult<List<string>>> GetAuthorSuggestions(
        [FromQuery] string prefix,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prefix) || prefix.Length < 4)
        {
            return Ok(new List<string>());
        }

        _logger.LogInformation("GetAuthorSuggestions called with prefix: {Prefix}", prefix);

        var suggestions = await _bookService.GetAuthorSuggestionsAsync(prefix, cancellationToken);
        return Ok(suggestions);
    }

    /// <summary>
    /// Borrow a book. Requires authentication. Returns 409 Conflict if no copies are available.
    /// </summary>
    /// <param name="bookId">Book ID to borrow</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Borrow operation result</returns>
    [HttpPost("{bookId}/borrow")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public async Task<ActionResult<BorrowBookResponseDto>> BorrowBook(
        string bookId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bookId))
        {
            return BadRequest(new { message = "BookId is required" });
        }

        var userIdClaim = HttpContext.User?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            _logger.LogWarning("BorrowBook: User not authenticated or invalid UserId claim");
            return Unauthorized(new { message = "User authentication required" });
        }

        var request = new BorrowBookRequestDto { BookId = bookId };
        var validationResult = await _borrowValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("BorrowBook: Validation failed. Errors: {Errors}",
                string.Join("; ", validationResult.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")));
            return BadRequest(new { message = "Validation failed", errors = validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }) });
        }

        try
        {
            var result = await _bookLoanService.BorrowBookAsync(bookId, userId, cancellationToken);
            _logger.LogInformation("Book borrowed successfully - BookId: {BookId}, UserId: {UserId}, LoanId: {LoanId}", 
                bookId, userId, result.LoanId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("BorrowBook: Book not found - BookId: {BookId}, UserId: {UserId}", bookId, userId);
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("BorrowBook: No available copies - BookId: {BookId}, UserId: {UserId}, Message: {Message}", 
                bookId, userId, ex.Message);
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while borrowing book - BookId: {BookId}, UserId: {UserId}", bookId, userId);
            return StatusCode(500, new { message = "An error occurred while borrowing the book" });
        }
    }

    /// <summary>
    /// Return a book. Requires authentication. Returns 400 BadRequest if user doesn't have an active loan.
    /// </summary>
    /// <param name="bookId">Book ID to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Return operation result</returns>
    [HttpPost("{bookId}/return")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public async Task<ActionResult> ReturnBook(
        string bookId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bookId))
        {
            return BadRequest(new { message = "BookId is required" });
        }

        var userIdClaim = HttpContext.User?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            _logger.LogWarning("ReturnBook: User not authenticated or invalid UserId claim");
            return Unauthorized(new { message = "User authentication required" });
        }

        var request = new ReturnBookRequestDto { BookId = bookId };
        var validationResult = await _returnValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("ReturnBook: Validation failed. Errors: {Errors}",
                string.Join("; ", validationResult.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")));
            return BadRequest(new { message = "Validation failed", errors = validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }) });
        }

        try
        {
            await _bookLoanService.ReturnBookAsync(bookId, userId, cancellationToken);
            _logger.LogInformation("Book returned successfully - BookId: {BookId}, UserId: {UserId}", bookId, userId);
            return Ok(new { message = "Book returned successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("ReturnBook: Book not found - BookId: {BookId}, UserId: {UserId}", bookId, userId);
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("ReturnBook: User doesn't have active loan - BookId: {BookId}, UserId: {UserId}, Message: {Message}", 
                bookId, userId, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while returning book - BookId: {BookId}, UserId: {UserId}", bookId, userId);
            return StatusCode(500, new { message = "An error occurred while returning the book" });
        }
    }

    /// <summary>
    /// Get borrow status for a specific book and current user. Requires authentication.
    /// </summary>
    /// <param name="bookId">Book ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Borrow status information</returns>
    [HttpGet("{bookId}/borrow-status")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public async Task<ActionResult<BookBorrowStatusDto>> GetBookBorrowStatus(
        string bookId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bookId))
        {
            return BadRequest(new { message = "BookId is required" });
        }

        var userIdClaim = HttpContext.User?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            _logger.LogWarning("GetBookBorrowStatus: User not authenticated or invalid UserId claim");
            return Unauthorized(new { message = "User authentication required" });
        }

        try
        {
            var result = await _bookLoanService.GetBookBorrowStatusAsync(bookId, userId, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("GetBookBorrowStatus: Book not found - BookId: {BookId}, UserId: {UserId}", bookId, userId);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting borrow status - BookId: {BookId}, UserId: {UserId}", bookId, userId);
            return StatusCode(500, new { message = "An error occurred while getting borrow status" });
        }
    }

    /// <summary>
    /// Get borrow status for multiple books and current user in batch. Requires authentication.
    /// </summary>
    /// <param name="bookIds">List of book IDs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary mapping BookId to borrow status</returns>
    [HttpPost("borrow-status/batch")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public async Task<ActionResult<Dictionary<string, BookBorrowStatusDto>>> GetBooksBorrowStatus(
        [FromBody] List<string> bookIds,
        CancellationToken cancellationToken = default)
    {
        if (bookIds == null || bookIds.Count == 0)
        {
            return Ok(new Dictionary<string, BookBorrowStatusDto>());
        }

        var userIdClaim = HttpContext.User?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            _logger.LogWarning("GetBooksBorrowStatus: User not authenticated or invalid UserId claim");
            return Unauthorized(new { message = "User authentication required" });
        }

        try
        {
            var result = await _bookLoanService.GetBooksBorrowStatusAsync(bookIds, userId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting batch borrow status - BookIds: {BookIds}, UserId: {UserId}", 
                string.Join(", ", bookIds), userId);
            return StatusCode(500, new { message = "An error occurred while getting borrow status" });
        }
    }

    /// <summary>
    /// Get all currently borrowed books for the authenticated user. Requires authentication.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of borrowed books with details</returns>
    [HttpGet("my-borrowed-books")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public async Task<ActionResult<List<BorrowedBookDto>>> GetMyBorrowedBooks(
        CancellationToken cancellationToken = default)
    {
        var userIdClaim = HttpContext.User?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            _logger.LogWarning("GetMyBorrowedBooks: User not authenticated or invalid UserId claim");
            return Unauthorized(new { message = "User authentication required" });
        }

        try
        {
            var result = await _bookLoanService.GetUserBorrowedBooksAsync(userId, cancellationToken);
            _logger.LogInformation("GetMyBorrowedBooks: Retrieved {Count} borrowed books for user {UserId}", result.Count, userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting borrowed books - UserId: {UserId}", userId);
            return StatusCode(500, new { message = "An error occurred while getting borrowed books" });
        }
    }
}