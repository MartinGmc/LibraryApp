using FluentValidation.TestHelper;
using LibraryApp.Application.Books.Dtos;
using LibraryApp.Application.Books.Validators;

namespace LibraryApp.UnitTests.Books.Validators;

public class ReturnBookRequestValidatorTests
{
    private readonly ReturnBookRequestValidator _validator;

    public ReturnBookRequestValidatorTests()
    {
        _validator = new ReturnBookRequestValidator();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_BookIdIsEmpty_ReturnsValidationError(string? bookId)
    {
        // Arrange
        var request = new ReturnBookRequestDto
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
        var request = new ReturnBookRequestDto
        {
            BookId = "book-id-123"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}

