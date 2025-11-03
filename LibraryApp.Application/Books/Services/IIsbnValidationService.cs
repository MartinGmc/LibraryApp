namespace LibraryApp.Application.Books.Services;

public interface IIsbnValidationService
{
    bool ValidateIsbn13(string isbn);
}

