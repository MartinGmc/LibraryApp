# Library Management System - Technical Overview

## What It Is

A web-based library management application built with .NET 8 that manages books, book loans, and user authentication. Supports both web UI (Blazor Server) and REST API access.

## Architecture

**Clean Architecture** with 4 layers:
- **Domain**: Core entities (Book, BookLoan, ApplicationUser)
- **Application**: Business logic, services, DTOs, validators
- **Infrastructure**: EF Core, SQLite database, migrations
- **Presentation**: Blazor Server UI + REST API controllers

## Technology Stack

- **.NET 8** with ASP.NET Core
- **Blazor Server** for real-time UI
- **SQLite** database (file-based)
- **Entity Framework Core** for data access
- **Serilog** for structured logging
- **FluentValidation** for input validation

## Key Features

1. **Book Management**: Create, search, and manage books with ISBN validation
2. **Book Loans**: Borrow/return books with availability tracking
3. **Dual Authentication**: Cookie-based for web users, API Key for programmatic access
4. **Real-time UI**: Blazor Server with SignalR for live updates
5. **Auto-migrations**: Database migrations applied automatically on startup

## Database

SQLite file (`app.db`) with tables:
- `Books`: Book catalog with unique constraint (Name + Author + ISBN)
- `BookLoans`: Borrowing transactions
- `ApplicationUsers`: User accounts with login/password and API keys
- `Statuses`: Status tracking

## API Endpoints

**Main Endpoints:**
- `GET /api/book` - List books (paginated, filterable)
- `POST /api/book` - Add new book
- `POST /api/book/{id}/borrow` - Borrow a book
- `POST /api/book/{id}/return` - Return a book
- `GET /api/book/my-borrowed-books` - Get user's borrowed books
- `POST /api/auth/login` - User login
- `POST /api/auth/logout` - User logout

## Authentication

- **Cookie Authentication**: For web users (8-hour sliding expiration)
- **API Key Authentication**: For programmatic access (X-API-Key header)
- **User Types**: User (web) or API (programmatic)
- **Permissions**: Standard or Elevated

## Deployment

- **Target**: Azure App Service (single instance)
- **Database**: SQLite stored in `/home/site/data/app.db` (persistent storage)
- **Logs**: Serilog files in `/home/site/data/logs/`
- **HTTPS**: Required in production

## Configuration

- **Connection String**: `Data Source=app.db` (local) or `/home/site/data/app.db` (Azure)
- **Logging**: Structured logs with daily rotation, 30-day retention
- **Environment**: Development (HTTP) or Production (HTTPS only)

## Key Business Rules

1. **Book Uniqueness**: Name + Author + ISBN combination must be unique
2. **Availability**: Books can be borrowed up to `NumberOfPieces` copies
3. **Loan Tracking**: Multiple loans per user allowed; returns use FIFO (oldest first)
4. **ISBN Validation**: Supports ISBN-13 format validation

## Project Structure

```
LibraryApp.Domain/          # Entities, Enums
LibraryApp.Application/     # Services, DTOs, Validators
LibraryApp.Infrastructure/  # EF Core, DbContext, Migrations
LibraryApp.Web/            # Blazor + API Controllers
LibraryApp.UnitTests/       # Unit tests
```

## Data Flow

1. **Request** → API Controller or Blazor Page
2. **Validation** → FluentValidation validators
3. **Business Logic** → Application services
4. **Data Access** → EF Core via AppDbContext
5. **Response** → DTOs returned to client

## Logging

Structured logging with Serilog:
- Console output (development)
- File logs with daily rotation (production)
- Logs authentication, operations, errors, and HTTP requests

## Testing

- **Unit Tests**: xUnit with Moq for mocking
- **Target Coverage**: 80%+
- **Test Location**: `LibraryApp.UnitTests/`

---

**Note**: See `TECHNICAL_DOCUMENTATION.md` for detailed documentation.

