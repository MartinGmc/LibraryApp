using FluentValidation.TestHelper;
using LibraryApp.Application.Books.Dtos;
using LibraryApp.Application.Books.Validators;

namespace LibraryApp.UnitTests.Books.Validators;

public class BorrowBookRequestValidatorTests
{
    private readonly BorrowBookRequestValidator _validator;

    public BorrowBookRequestValidatorTests()
    {
        _validator = new BorrowBookRequestValidator();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_BookIdIsEmpty_ReturnsValidationError(string? bookId)
    {
        // Arrange
        var request = new BorrowBookRequestDto
        {
            BookId = bookId ?? ""
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.BookId).WithErrorMessage("BookId is required");
    }

    [Fact]
    public void Validate_ValidBookId_ReturnsNoErrors()
    {
        // Arrange
        var request = new BorrowBookRequestDto
        {
            BookId = "book-id-123"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}

