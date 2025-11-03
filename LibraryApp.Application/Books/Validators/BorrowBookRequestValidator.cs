using FluentValidation;
using LibraryApp.Application.Books.Dtos;

namespace LibraryApp.Application.Books.Validators;

public class BorrowBookRequestValidator : AbstractValidator<BorrowBookRequestDto>
{
    public BorrowBookRequestValidator()
    {
        RuleFor(x => x.BookId)
            .NotEmpty()
            .WithMessage("BookId is required");
    }
}

