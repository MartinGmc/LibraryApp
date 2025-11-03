using FluentValidation;
using LibraryApp.Application.Books.Dtos;

namespace LibraryApp.Application.Books.Validators;

public class ReturnBookRequestValidator : AbstractValidator<ReturnBookRequestDto>
{
    public ReturnBookRequestValidator()
    {
        RuleFor(x => x.BookId)
            .NotEmpty()
            .WithMessage("BookId is required");
    }
}

