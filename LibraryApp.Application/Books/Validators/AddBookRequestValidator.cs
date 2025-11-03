using FluentValidation;
using LibraryApp.Application.Books.Dtos;
using LibraryApp.Application.Books.Services;

namespace LibraryApp.Application.Books.Validators;

public class AddBookRequestValidator : AbstractValidator<AddBookRequestDto>
{
    private readonly IIsbnValidationService _isbnValidationService;

    public AddBookRequestValidator(IIsbnValidationService isbnValidationService)
    {
        _isbnValidationService = isbnValidationService;

        RuleFor(x => x.name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MaximumLength(300)
            .WithMessage("Name must not exceed 300 characters");

        RuleFor(x => x.author)
            .NotEmpty()
            .WithMessage("Author is required")
            .MaximumLength(200)
            .WithMessage("Author must not exceed 200 characters");

        RuleFor(x => x.issueyear)
            .NotEmpty()
            .WithMessage("IssueYear is required")
            .GreaterThanOrEqualTo(1000)
            .WithMessage("IssueYear must be greater than or equal to 1000")
            .LessThanOrEqualTo(DateTime.Now.Year)
            .WithMessage($"IssueYear must be less than or equal to {DateTime.Now.Year}");

        RuleFor(x => x.isbn)
            .NotEmpty()
            .WithMessage("ISBN is required")
            .Must(BeValidIsbn)
            .WithMessage("ISBN must be a valid ISBN-13 format");

        RuleFor(x => x.numberOfPieces)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Number of Pieces must be greater than or equal to 0");
    }

    private bool BeValidIsbn(string isbn)
    {
        return _isbnValidationService.ValidateIsbn13(isbn);
    }
}

