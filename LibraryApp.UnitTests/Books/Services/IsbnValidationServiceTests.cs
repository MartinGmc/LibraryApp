using LibraryApp.Application.Books.Services;

namespace LibraryApp.UnitTests.Books.Services;

public class IsbnValidationServiceTests
{
    private readonly IsbnValidationService _service;

    public IsbnValidationServiceTests()
    {
        _service = new IsbnValidationService();
    }

    [Theory]
    [InlineData("978-0-13-110163-0")]
    [InlineData("9780131101630")]
    [InlineData("978 0 13 110163 0")]
    [InlineData("978-1-56619-909-4")]
    [InlineData("9781566199094")]
    public void ValidateIsbn13_ValidIsbn_ReturnsTrue(string isbn)
    {
        // Act
        var result = _service.ValidateIsbn13(isbn);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123")]
    [InlineData("97801311016301")]
    [InlineData("978013110163")]
    [InlineData("978-0-13-110163-1")]
    [InlineData("invalid-isbn")]
    [InlineData("1234567890123")]
    public void ValidateIsbn13_InvalidIsbn_ReturnsFalse(string? isbn)
    {
        // Act
        var result = _service.ValidateIsbn13(isbn);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateIsbn13_IsbnWithLetters_ReturnsFalse()
    {
        // Arrange
        var isbn = "978-0-AB-110163-0";

        // Act
        var result = _service.ValidateIsbn13(isbn);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateIsbn13_IsbnWithIncorrectCheckDigit_ReturnsFalse()
    {
        // Arrange
        var isbn = "978-0-13-110163-1"; // Last digit should be 0

        // Act
        var result = _service.ValidateIsbn13(isbn);

        // Assert
        Assert.False(result);
    }
}

