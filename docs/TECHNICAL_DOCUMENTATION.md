# Library Management System - Technical Documentation

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Technology Stack](#technology-stack)
4. [Project Structure](#project-structure)
5. [Domain Layer](#domain-layer)
6. [Application Layer](#application-layer)
7. [Infrastructure Layer](#infrastructure-layer)
8. [Presentation Layer](#presentation-layer)
9. [Authentication & Authorization](#authentication--authorization)
10. [Database Schema](#database-schema)
11. [API Endpoints](#api-endpoints)
12. [Key Features & Workflows](#key-features--workflows)
13. [Configuration](#configuration)
14. [Logging](#logging)
15. [Error Handling](#error-handling)
16. [Testing](#testing)

---

## Overview

The Library Management System is a web application built with **.NET 8** that provides functionality for managing a library catalog, book loans, and user authentication. The application follows **Clean Architecture** principles, separating concerns into distinct layers.

### Core Functionality

- **Book Management**: Create, search, and manage books in the library catalog
- **Book Loans**: Borrow and return books with availability tracking
- **User Authentication**: Cookie-based authentication for web users and API Key authentication for programmatic access
- **User Management**: Support for different user types (User, API) with permission levels (Standard, Elevated)
- **Real-time Features**: Blazor Server provides real-time UI updates via SignalR

### Key Characteristics

- **Clean Architecture**: Separation of concerns across Domain, Application, Infrastructure, and Presentation layers
- **Dual Authentication**: Cookie authentication for web UI, API Key authentication for REST API
- **SQLite Database**: File-based database suitable for single-instance deployments
- **Async/Await**: All I/O operations are asynchronous for better performance
- **Structured Logging**: Serilog for comprehensive logging with file and console outputs
- **Validation**: FluentValidation for input validation
- **Azure-Ready**: Configured for deployment to Azure App Service

---

## Architecture

The application follows **Clean Architecture** principles with clear separation of concerns:

```
┌─────────────────────────────────────────┐
│         Presentation Layer             │
│  (Blazor Server, API Controllers)      │
└─────────────────┬───────────────────────┘
                  │
┌─────────────────▼───────────────────────┐
│         Application Layer                │
│  (Services, DTOs, Validators)           │
└─────────────────┬───────────────────────┘
                  │
┌─────────────────▼───────────────────────┐
│         Infrastructure Layer             │
│  (EF Core, DbContext, Migrations)       │
└─────────────────┬───────────────────────┘
                  │
┌─────────────────▼───────────────────────┐
│         Domain Layer                    │
│  (Entities, Value Objects, Enums)      │
└─────────────────────────────────────────┘
```

### Dependency Flow

- **Presentation** → **Application** → **Infrastructure** → **Domain**
- **Domain** has no dependencies on other layers
- **Application** depends only on **Domain**
- **Infrastructure** implements interfaces defined in **Application**
- **Presentation** orchestrates user interactions and delegates to **Application**

---

## Technology Stack

### Backend
- **.NET 8**: Latest .NET framework
- **ASP.NET Core**: Web framework
- **Blazor Server**: Real-time UI with SignalR
- **Entity Framework Core**: ORM for database access
- **SQLite**: File-based relational database

### Libraries & Tools
- **Serilog**: Structured logging framework
- **FluentValidation**: Input validation library
- **Swagger/OpenAPI**: API documentation

### Development Tools
- **xUnit**: Unit testing framework
- **Moq**: Mocking framework for tests

---

## Project Structure

```
LibraryApp/
├── LibraryApp.Domain/              # Domain Layer
│   ├── Entities/                   # Domain entities
│   │   ├── Book.cs
│   │   ├── BookLoan.cs
│   │   ├── ApplicationUser.cs
│   │   └── Status.cs
│   └── Enums/                      # Domain enums
│       ├── UserType.cs
│       └── Permissions.cs
│
├── LibraryApp.Application/          # Application Layer
│   ├── Auth/                       # Authentication services
│   ├── Books/                      # Book management services
│   │   ├── Dtos/                   # Data Transfer Objects
│   │   ├── Services/               # Helper services
│   │   └── Validators/             # FluentValidation validators
│   ├── Status/                     # Status services
│   ├── Common/                     # Shared DTOs and interfaces
│   └── DependencyInjection.cs      # Service registration
│
├── LibraryApp.Infrastructure/      # Infrastructure Layer
│   ├── Persistence/                # Database access
│   │   ├── AppDbContext.cs        # EF Core DbContext
│   │   ├── Configurations/         # EF Core entity configurations
│   │   ├── BookSeeder.cs          # Database seeding
│   │   └── UserSeeder.cs          # User seeding
│   └── Migrations/                 # EF Core migrations
│
├── LibraryApp.Web/                 # Presentation Layer
│   ├── Controllers/                # API controllers
│   │   ├── AuthController.cs
│   │   ├── BookController.cs
│   │   ├── BookApiController.cs
│   │   └── StatusController.cs
│   ├── Pages/                      # Blazor pages
│   ├── Shared/                     # Shared Blazor components
│   ├── Middleware/                 # Custom middleware
│   ├── Filters/                    # Action filters
│   ├── Authentication/             # Authentication handlers
│   └── Program.cs                  # Application startup
│
└── LibraryApp.UnitTests/           # Unit Tests
    ├── Auth/
    ├── Books/
    └── Status/
```

---

## Domain Layer

The Domain layer contains the core business entities and value objects with no dependencies on other layers.

### Entities

#### Book
Represents a book in the library catalog.

```12:12:LibraryApp.Domain/Entities/Book.cs
    public string Id { get; set; } = Guid.NewGuid().ToString();
```

**Properties:**
- `Id` (string, GUID): Unique identifier
- `Name` (string): Book title
- `Author` (string): Author name
- `IssueYear` (int): Publication year
- `ISBN` (string): ISBN-13 identifier
- `NumberOfPieces` (int): Total number of copies available

**Business Rules:**
- Unique constraint: Combination of `Name`, `Author`, and `ISBN` must be unique
- `Id` is auto-generated as GUID on entity creation

#### BookLoan
Represents a book borrowing transaction.

**Properties:**
- `Id` (string, GUID): Unique loan identifier
- `BookId` (string): Reference to Book entity
- `UserId` (int): Reference to ApplicationUser
- `BorrowedDate` (DateTime): When the book was borrowed
- `ReturnedDate` (DateTime?): When the book was returned (null if still borrowed)

**Business Rules:**
- Multiple loans per user for the same book are allowed
- Return operation uses oldest active loan first (FIFO)
- `BorrowedDate` is set to UTC when loan is created

#### ApplicationUser
Represents a user in the system.

**Properties:**
- `Id` (int): Unique user identifier
- `Login` (string): Username for authentication
- `Password` (string): Password (stored in plain text for simplicity - production should use hashing)
- `UserType` (UserType enum): `User` or `API`
- `ApiKey` (string): API key for programmatic access (only for API users)
- `Permissions` (Permissions enum): `Standard` or `Elevated`

#### Status
Simple entity for status tracking.

**Properties:**
- `Id` (int): Unique identifier
- `Value` (string): Status value

### Enums

#### UserType
```3:7:LibraryApp.Domain/Enums/UserType.cs
public enum UserType
{
    User,
    API
}
```

#### Permissions
```3:7:LibraryApp.Domain/Enums/Permissions.cs
public enum Permissions
{
    Standard,
    Elevated
}
```

---

## Application Layer

The Application layer contains business logic, services, DTOs, and validators. It depends only on the Domain layer.

### Services

#### IBookService / BookService
Manages book-related operations.

**Key Methods:**
- `GetAllBooksAsync`: Retrieves paginated books with optional filters (Name, Author, ISBN)
- `AddBookAsync`: Creates a new book with uniqueness validation
- `GetBookNameSuggestionsAsync`: Returns autocomplete suggestions for book names
- `GetAuthorSuggestionsAsync`: Returns autocomplete suggestions for author names

**Features:**
- Case-insensitive search using SQLite LIKE operator
- Pagination with configurable page size (max 100)
- Filters out invalid records (empty Id, Name, Author, or ISBN)
- Uniqueness check: Name + Author + ISBN combination must be unique

#### IBookLoanService / BookLoanService
Manages book loan operations.

**Key Methods:**
- `BorrowBookAsync`: Creates a new loan if copies are available
- `ReturnBookAsync`: Marks the oldest active loan as returned
- `GetBookBorrowStatusAsync`: Gets availability and user loan status for a book
- `GetBooksBorrowStatusAsync`: Batch operation for multiple books
- `GetUserBorrowedBooksAsync`: Retrieves all active loans for a user

**Business Logic:**
- Availability calculation: `NumberOfPieces - ActiveLoanCount`
- Returns 409 Conflict if no copies available
- Multiple loans per user allowed for same book
- Return uses FIFO (oldest loan first)

#### IAuthService / AuthService
Handles authentication and user validation.

**Key Methods:**
- `ValidateLoginAsync`: Validates login credentials
- `GetUserByIdAsync`: Retrieves user by ID
- `ValidateApiKeyAsync`: Validates API key for programmatic access

### DTOs (Data Transfer Objects)

DTOs use lowercase property names to match JSON conventions:

- `AddBookRequestDto`: Book creation request
- `BookResponseDto`: Book response data
- `GetAllBooksRequestDto`: Pagination and filter parameters
- `PaginatedResponseDto<T>`: Generic paginated response wrapper
- `BorrowBookResponseDto`: Loan creation response
- `BookBorrowStatusDto`: Availability and loan status information
- `BorrowedBookDto`: User's borrowed books list

### Validators

FluentValidation validators ensure data integrity:

- `AddBookRequestDtoValidator`: Validates book creation (ISBN-13, required fields, year range)
- `BorrowBookRequestDtoValidator`: Validates borrow requests
- `ReturnBookRequestDtoValidator`: Validates return requests

### ISBN Validation Service

`IIsbnValidationService` validates ISBN-13 format:
- Supports ISBN with or without hyphens
- Validates check digit using ISBN-13 algorithm

---

## Infrastructure Layer

The Infrastructure layer handles data persistence and external integrations.

### AppDbContext

```8:24:LibraryApp.Infrastructure/Persistence/AppDbContext.cs
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
```

**Features:**
- Implements `IAppDbContext` interface for abstraction
- Uses EF Core configurations from `Configurations/` folder
- Applies all configurations automatically via assembly scanning

### Entity Configurations

Entity configurations define:
- Primary keys
- Unique constraints (Book: Name + Author + ISBN)
- Indexes
- Required fields
- String length limits

### Migrations

EF Core migrations are automatically applied on application startup:

1. `InitialCreate`: Creates Status table
2. `AddApplicationUsersTable`: Creates ApplicationUsers table
3. `AddBookTable`: Creates Books table
4. `AddUniqueConstraintForBookNameAuthorIsbn`: Adds uniqueness constraint
5. `AddBookLoanTable`: Creates BookLoans table

**Migration Strategy:**
- Migrations run automatically at startup via `Program.cs`
- Only pending migrations are applied
- Logs all migration operations for audit

### Seeders

#### UserSeeder
Seeds initial users on first startup:
- Creates default users with different types and permissions
- Only creates users if they don't exist (idempotent)

#### BookSeeder
Seeds initial books for testing:
- Creates sample books with valid ISBNs
- Only runs if no books exist

---

## Presentation Layer

The Presentation layer consists of Blazor Server components and API controllers.

### Blazor Server

Blazor Server provides real-time UI updates via SignalR:

- **Pages:**
  - `Index.razor`: Main library interface
  - `Login.razor`: User login page
  - `_Host.cshtml`: Host page for Blazor Server

- **Features:**
  - Real-time updates when data changes
  - Server-side rendering
  - Cookie-based authentication
  - HttpClient integration with cookie forwarding

### API Controllers

#### BookController
RESTful API for book operations (Cookie authentication only).

**Endpoints:**
- `GET /api/book`: Get paginated books with filters
- `POST /api/book`: Add new book
- `GET /api/book/validate-isbn`: Validate ISBN-13
- `GET /api/book/name-suggestions`: Get book name autocomplete
- `GET /api/book/author-suggestions`: Get author name autocomplete
- `POST /api/book/{bookId}/borrow`: Borrow a book
- `POST /api/book/{bookId}/return`: Return a book
- `GET /api/book/{bookId}/borrow-status`: Get borrow status for a book
- `POST /api/book/borrow-status/batch`: Batch borrow status check
- `GET /api/book/my-borrowed-books`: Get user's borrowed books

#### BookApiController
API endpoints for programmatic access (API Key authentication).

**Endpoints:**
- Similar to BookController but uses API Key authentication
- Designed for external system integration

#### AuthController
Authentication endpoints.

**Endpoints:**
- `POST /api/auth/login`: User login (creates cookie)
- `POST /api/auth/logout`: User logout (clears cookie)

#### StatusController
Status endpoint for health checks.

**Endpoints:**
- `GET /api/status/first`: Returns first status value

### Middleware

#### RequestLoggingMiddleware
Logs all HTTP requests with:
- Request path, method, query string
- Response status code
- Request duration

#### ModelBindingErrorMiddleware
Catches and logs model binding errors for debugging.

### Filters

#### ModelStateLoggingFilter
Logs ModelState validation errors for API endpoints.

#### ModelBindingResultFilter
Logs model binding results for debugging.

---

## Authentication & Authorization

The application supports dual authentication mechanisms:

### Cookie Authentication (Web Users)

**Configuration:**
```268:293:LibraryApp.Web/Program.cs
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.LoginPath = "/Login";
                options.LogoutPath = "/Logout";
                options.Cookie.HttpOnly = true;
                // In production (Azure), always use Secure cookies (HTTPS only)
                // In development, allow HTTP
                options.Cookie.SecurePolicy = builder.Environment.IsProduction() 
                    ? CookieSecurePolicy.Always 
                    : CookieSecurePolicy.SameAsRequest;
                // In Azure App Service behind a proxy/load balancer, we need SameSite=None with Secure
                // Lax doesn't work for cross-site requests even when behind the same proxy
                options.Cookie.SameSite = builder.Environment.IsProduction()
                    ? SameSiteMode.None  // Required for Azure App Service behind proxy
                    : SameSiteMode.Lax;
                // Don't set domain - let browser handle it (works with any subdomain in Azure)
                // Explicitly set path to root to ensure cookie is available everywhere
                options.Cookie.Path = "/";
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
```

**Features:**
- HttpOnly cookies for XSS protection
- Secure cookies in production (HTTPS only)
- SameSite=None for Azure App Service (behind proxy)
- 8-hour sliding expiration
- Persistent cookies

**Claims:**
- `NameIdentifier`: User ID
- `Name`: Username
- `UserType`: User type (User or API)
- `Permissions`: Permission level (Standard or Elevated)

### API Key Authentication (Programmatic Access)

**Configuration:**
```352:358:LibraryApp.Web/Program.cs
            .AddScheme<AuthenticationSchemeOptions, Authentication.ApiKeyAuthenticationHandler>(
                "ApiKey", 
                options => 
                {
                    // API Key scheme is only used when explicitly specified in [Authorize] attribute
                    // It won't be auto-challenged because default challenge scheme is Cookie
                });
```

**Usage:**
- API Key passed via `X-API-Key` header
- Only used when explicitly specified: `[Authorize(AuthenticationSchemes = "ApiKey")]`
- Validated against `ApplicationUser.ApiKey` for users with `UserType.API`

### Authorization Policies

Default policy requires Cookie authentication:
```362:369:LibraryApp.Web/Program.cs
        builder.Services.AddAuthorization(options =>
        {
            // Default policy uses only Cookie authentication
            // Endpoints that need API Key should explicitly specify it: [Authorize(AuthenticationSchemes = "ApiKey")]
            options.DefaultPolicy = new AuthorizationPolicyBuilder(
                    CookieAuthenticationDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .Build();
        });
```

---

## Database Schema

### Books Table
```sql
CREATE TABLE Books (
    Id TEXT PRIMARY KEY,
    Name TEXT NOT NULL,
    Author TEXT NOT NULL,
    IssueYear INTEGER NOT NULL,
    ISBN TEXT NOT NULL,
    NumberOfPieces INTEGER NOT NULL,
    UNIQUE(Name, Author, ISBN)
);
```

### BookLoans Table
```sql
CREATE TABLE BookLoans (
    Id TEXT PRIMARY KEY,
    BookId TEXT NOT NULL,
    UserId INTEGER NOT NULL,
    BorrowedDate TEXT NOT NULL,
    ReturnedDate TEXT
);
```

### ApplicationUsers Table
```sql
CREATE TABLE ApplicationUsers (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Login TEXT NOT NULL UNIQUE,
    Password TEXT NOT NULL,
    UserType INTEGER NOT NULL,
    ApiKey TEXT NOT NULL,
    Permissions INTEGER NOT NULL
);
```

### Statuses Table
```sql
CREATE TABLE Statuses (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Value TEXT NOT NULL
);
```

---

## API Endpoints

### Book Management

#### Get All Books
```
GET /api/book?pageNumber=1&pageSize=10&name=search&author=author&isbn=isbn
Authorization: Cookie
Response: PaginatedResponseDto<BookResponseDto>
```

#### Add New Book
```
POST /api/book
Authorization: Cookie
Body: {
  "name": "Book Title",
  "author": "Author Name",
  "issueyear": 2024,
  "isbn": "978-0-123456-78-9",
  "numberOfPieces": 5
}
Response: BookResponseDto (201 Created)
```

#### Validate ISBN
```
GET /api/book/validate-isbn?isbn=9780123456789
Authorization: Cookie
Response: bool
```

#### Get Book Name Suggestions
```
GET /api/book/name-suggestions?prefix=search
Authorization: Cookie
Response: List<string>
```

#### Get Author Suggestions
```
GET /api/book/author-suggestions?prefix=search
Authorization: Cookie
Response: List<string>
```

### Book Loans

#### Borrow Book
```
POST /api/book/{bookId}/borrow
Authorization: Cookie
Response: BorrowBookResponseDto (200 OK) or 409 Conflict if unavailable
```

#### Return Book
```
POST /api/book/{bookId}/return
Authorization: Cookie
Response: { "message": "Book returned successfully" } (200 OK)
```

#### Get Borrow Status
```
GET /api/book/{bookId}/borrow-status
Authorization: Cookie
Response: BookBorrowStatusDto
```

#### Batch Borrow Status
```
POST /api/book/borrow-status/batch
Authorization: Cookie
Body: ["bookId1", "bookId2", ...]
Response: Dictionary<string, BookBorrowStatusDto>
```

#### Get My Borrowed Books
```
GET /api/book/my-borrowed-books
Authorization: Cookie
Response: List<BorrowedBookDto>
```

### Authentication

#### Login
```
POST /api/auth/login
Authorization: None (AllowAnonymous)
Body: {
  "login": "username",
  "password": "password"
}
Response: { "message": "Login successful", "userId": 1 } (200 OK)
Sets: Authentication cookie
```

#### Logout
```
POST /api/auth/logout
Authorization: Cookie
Response: { "message": "Logout successful" } (200 OK)
Clears: Authentication cookie
```

---

## Key Features & Workflows

### Book Management Workflow

1. **Adding a Book:**
   - User submits book details via form or API
   - FluentValidation validates ISBN-13 format
   - Service checks uniqueness (Name + Author + ISBN)
   - If unique, book is created with auto-generated GUID
   - Response includes created book with ID

2. **Searching Books:**
   - User provides optional filters (Name, Author, ISBN)
   - Case-insensitive search using SQLite LIKE
   - Results paginated (default 10, max 100 per page)
   - Ordered by Name, then Author

3. **Autocomplete:**
   - User types at least 4 characters
   - System queries database for matching names/authors
   - Returns up to 20 distinct suggestions
   - Sorted alphabetically

### Book Loan Workflow

1. **Borrowing a Book:**
   - User selects a book to borrow
   - System checks availability: `NumberOfPieces - ActiveLoanCount`
   - If available (count > 0), creates new BookLoan
   - Sets `BorrowedDate` to UTC now
   - Returns loan details with LoanId

2. **Returning a Book:**
   - User selects a borrowed book to return
   - System finds oldest active loan for user + book
   - Sets `ReturnedDate` to UTC now
   - Book becomes available again

3. **Availability Tracking:**
   - Real-time calculation of available copies
   - Counts active loans (where `ReturnedDate IS NULL`)
   - Prevents over-borrowing beyond `NumberOfPieces`

### Authentication Workflow

1. **Login:**
   - User submits login credentials
   - `AuthService.ValidateLoginAsync` checks database
   - If valid, creates ClaimsPrincipal with user claims
   - Signs in with Cookie authentication
   - Cookie sent to browser, subsequent requests authenticated

2. **API Key Authentication:**
   - External system sends request with `X-API-Key` header
   - `ApiKeyAuthenticationHandler` validates key
   - If valid, creates ClaimsPrincipal with API user claims
   - Request proceeds with API user context

---

## Configuration

### Connection Strings

**Development:**
```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=app.db"
}
```

**Production (Azure):**
```
ConnectionStrings__DefaultConnection=Data Source=/home/site/data/app.db
```

### Logging Configuration

**Serilog Settings:**
```json
"Serilog": {
  "MinimumLevel": {
    "Default": "Information",
    "Override": {
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

**Log Paths:**
- Development: `logs/log-YYYYMMDD.txt`
- Production: `/home/site/data/logs/log-YYYYMMDD.txt`
- Rolling interval: Daily
- Retention: 30 days
- Flush interval: 1 second

### Data Protection

**Configuration:**
```253:256:LibraryApp.Web/Program.cs
        builder.Services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(keysDirectory))
            .SetApplicationName("LibraryApp")
            .SetDefaultKeyLifetime(TimeSpan.FromDays(90));
```

**Key Storage:**
- Development: `keys/` directory
- Production: `/home/site/data/keys/` directory
- Key lifetime: 90 days
- Required for cookie encryption/decryption

### Environment-Specific Settings

**Development:**
- HTTP allowed
- Cookie SameSite: Lax
- Cookie Secure: SameAsRequest
- Logs to local `logs/` directory

**Production:**
- HTTPS required
- Cookie SameSite: None (for Azure proxy)
- Cookie Secure: Always
- Logs to `/home/site/data/logs/`
- ForwardedHeaders enabled for Azure proxy

---

## Logging

### Serilog Configuration

Structured logging with Serilog:

```49:63:LibraryApp.Web/Program.cs
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                path: logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                flushToDiskInterval: TimeSpan.FromSeconds(1),
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .CreateLogger();
```

### Log Levels

- **Information**: Normal operations, business events
- **Warning**: Validation failures, authentication issues
- **Error**: Exceptions in try-catch blocks
- **Fatal**: Startup failures that prevent application start

### Logged Events

- **Authentication**: Login/logout, API key validation
- **Book Operations**: Create, search, borrow, return
- **Database**: Migrations, seeding operations
- **HTTP Requests**: All requests with path, method, status, duration
- **Errors**: All exceptions with stack traces

### Log Outputs

1. **Console**: For development and Azure Log Stream
2. **File**: Persistent log files with daily rotation
3. **Structured Format**: JSON-compatible structured logs

---

## Error Handling

### Global Exception Handler

```484:484:LibraryApp.Web/Program.cs
        app.UseExceptionHandler("/Error");
```

- Catches unhandled exceptions
- Redirects to error page
- Logs exception details

### API Error Responses

**Model Validation Errors:**
```87:175:LibraryApp.Web/Program.cs
            .ConfigureApiBehaviorOptions(options =>
            {
                // Customize the automatic 400 response for invalid model state
                options.InvalidModelStateResponseFactory = context =>
                {
                    var loggerFactory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
                    var logger = loggerFactory.CreateLogger("ApiController.Validation");
                    
                    // CRITICAL: Check if response has already started - if so, we can't write to it
                    if (context.HttpContext.Response.HasStarted)
                    {
                        logger.LogWarning("InvalidModelStateResponseFactory - Response already started, cannot write body");
                        return new Microsoft.AspNetCore.Mvc.BadRequestResult();
                    }
                    
                    var actionName = context.ActionDescriptor.DisplayName ?? "Unknown";
                    var route = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
                    var method = context.HttpContext.Request.Method;
                    
                    logger.LogWarning(
                        "=== InvalidModelStateResponseFactory TRIGGERED === Action: {Action}, Route: {Route}, Method: {Method}, Response.HasStarted: {HasStarted}, ModelState.ErrorCount: {ErrorCount}",
                        actionName, route, method, context.HttpContext.Response.HasStarted, context.ModelState.ErrorCount);
                    
                    var errors = context.ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .SelectMany(x => x.Value!.Errors.Select(e => new
                        {
                            Property = x.Key,
                            ErrorMessage = e.ErrorMessage,
                            Exception = e.Exception?.Message,
                            AttemptedValue = x.Value.AttemptedValue
                        }))
                        .ToList();
                    
                    if (errors.Any())
                    {
                        var errorDetails = errors.Select(e => 
                            $"Property: '{e.Property}', Error: '{e.ErrorMessage}', AttemptedValue: '{e.AttemptedValue}', Exception: {e.Exception ?? "None"}")
                            .ToList();
                        
                        logger.LogWarning(
                            "InvalidModelStateResponseFactory - Errors ({Count}): {Errors}",
                            errors.Count,
                            string.Join(" | ", errorDetails));
                        
                        // Log all ModelState keys
                        var allKeys = string.Join(", ", context.ModelState.Keys);
                        logger.LogWarning("InvalidModelStateResponseFactory - All ModelState Keys: [{Keys}]", allKeys);
                    }
                    else
                    {
                        logger.LogWarning("InvalidModelStateResponseFactory - ModelState has errors but error list is empty. ErrorCount: {ErrorCount}",
                            context.ModelState.ErrorCount);
                    }
                    
                    logger.LogWarning(
                        "ApiController automatic validation failed for {Action} - Route: {Route}, Errors: {Errors}",
                        actionName,
                        route,
                        string.Join("; ", errors.Select(e => $"{e.Property}: {e.ErrorMessage}")));
                    
                    // Return detailed error response
                    var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
                    {
                        Status = 400,
                        Title = "Validation Error",
                        Detail = "One or more validation errors occurred.",
                        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
                    };
                    
                    var errorDict = new Dictionary<string, string[]>();
                    foreach (var error in errors)
                    {
                        if (!errorDict.ContainsKey(error.Property))
                        {
                            errorDict[error.Property] = Array.Empty<string>();
                        }
                        var existingErrors = errorDict[error.Property].ToList();
                        existingErrors.Add(error.ErrorMessage);
                        errorDict[error.Property] = existingErrors.ToArray();
                    }
                    
                    problemDetails.Extensions["errors"] = errorDict;
                    
                    // Ensure content type is set to JSON
                    context.HttpContext.Response.ContentType = "application/json";
                    
                    return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(problemDetails);
                };
```

**Service-Level Errors:**
- `KeyNotFoundException` → 404 Not Found
- `InvalidOperationException` → 400 Bad Request or 409 Conflict
- General exceptions → 500 Internal Server Error

### Status Code Pages

```487:514:LibraryApp.Web/Program.cs
        app.UseStatusCodePages(context =>
        {
            // Skip status code pages for API routes - let controllers handle JSON responses
            if (context.HttpContext.Request.Path.StartsWithSegments("/api"))
            {
                // For API routes, do nothing - let the controller's JSON response pass through
                return Task.CompletedTask;
            }
            
            // Only write status code page for non-API routes if response hasn't been written yet
            if (!context.HttpContext.Response.HasStarted)
            {
                context.HttpContext.Response.ContentType = "text/plain";
                var statusCode = context.HttpContext.Response.StatusCode;
                var statusDescription = statusCode switch
                {
                    400 => "Bad Request",
                    401 => "Unauthorized",
                    403 => "Forbidden",
                    404 => "Not Found",
                    500 => "Internal Server Error",
                    _ => "Error"
                };
                return context.HttpContext.Response.WriteAsync($"Status Code: {statusCode}; {statusDescription}");
            }
            
            return Task.CompletedTask;
        });
```

---

## Testing

### Unit Tests

Unit tests are located in `LibraryApp.UnitTests/`:

**Test Structure:**
- `Auth/`: Authentication service tests
- `Books/`: Book and book loan service tests
- `Status/`: Status service tests

**Test Frameworks:**
- **xUnit**: Test framework
- **Moq**: Mocking framework for dependencies

**Coverage Areas:**
- Service business logic
- Validation rules
- Error handling
- Edge cases

### Test Execution

Run tests with:
```bash
dotnet test
```

### Test Strategy

- **Unit Tests**: Test services in isolation with mocked dependencies
- **Integration Tests**: Test full request/response cycle (future enhancement)
- **Target Coverage**: 80%+ code coverage

---

## Additional Considerations

### Performance Optimizations

1. **Async/Await**: All I/O operations are asynchronous
2. **Pagination**: Limits result sets to prevent memory issues
3. **Batch Operations**: `GetBooksBorrowStatusAsync` for multiple books in one query
4. **Database Indexes**: Unique constraints create indexes for fast lookups

### Security Considerations

1. **HTTPS Only**: Enforced in production
2. **HttpOnly Cookies**: Prevents XSS attacks
3. **Secure Cookies**: Required in production
4. **Input Validation**: FluentValidation on all inputs
5. **SQL Injection Protection**: EF Core parameterized queries

### Scalability Limitations

1. **SQLite**: File-based database, single-instance only
2. **Blazor Server**: Requires SignalR connection per user
3. **No Load Balancing**: Single instance deployment only

### Future Enhancements

1. **Password Hashing**: Implement bcrypt/Argon2 for password storage
2. **JWT Authentication**: Add JWT token support
3. **Azure SQL**: Migrate to Azure SQL for multi-instance support
4. **Caching**: Add IMemoryCache for frequently accessed data
5. **Background Jobs**: Hangfire for scheduled tasks
6. **API Rate Limiting**: Prevent abuse

---

## Conclusion

The Library Management System is a well-architected application following Clean Architecture principles. It provides a robust foundation for managing library operations with support for both web users and programmatic API access. The application is production-ready for single-instance deployments on Azure App Service.

