using LibraryApp.Application.Books.Dtos;
using LibraryApp.Application.Common.Dtos;

namespace LibraryApp.Application.Books;

public interface IBookService
{
    Task<PaginatedResponseDto<BookResponseDto>> GetAllBooksAsync(GetAllBooksRequestDto request, CancellationToken cancellationToken = default);
    Task<BookResponseDto> AddBookAsync(AddBookRequestDto request, CancellationToken cancellationToken = default);
    Task<List<string>> GetBookNameSuggestionsAsync(string prefix, CancellationToken cancellationToken = default);
    Task<List<string>> GetAuthorSuggestionsAsync(string prefix, CancellationToken cancellationToken = default);
}

