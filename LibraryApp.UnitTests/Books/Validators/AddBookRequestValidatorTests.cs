using FluentValidation.TestHelper;
using LibraryApp.Application.Books.Dtos;
using LibraryApp.Application.Books.Services;
using LibraryApp.Application.Books.Validators;
using Moq;

namespace LibraryApp.UnitTests.Books.Validators;

public class AddBookRequestValidatorTests
{
    private readonly Mock<IIsbnValidationService> _mockIsbnValidationService;
    private readonly AddBookRequestValidator _validator;

    public AddBookRequestValidatorTests()
    {
        _mockIsbnValidationService = new Mock<IIsbnValidationService>();
        _validator = new AddBookRequestValidator(_mockIsbnValidationService.Object);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_NameIsEmpty_ReturnsValidationError(string? name)
    {
        // Arrange
        var request = new AddBookRequestDto
        {
            name = name ?? "",
            author = "Author",
            issueyear = 2020,
            isbn = "9780131101630",
            numberOfPieces = 5
        };

        _mockIsbnValidationService.Setup(x => x.ValidateIsbn13(It.IsAny<string>())).Returns(true);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.name).WithErrorMessage("Name is required");
    }

    [Fact]
    public void Validate_NameTooLong_ReturnsValidationError()
    {
        // Arrange
        var request = new AddBookRequestDto
        {
            name = new string('a', 301),
            author = "Author",
            issueyear = 2020,
            isbn = "9780131101630",
            numberOfPieces = 5
        };

        _mockIsbnValidationService.Setup(x => x.ValidateIsbn13(It.IsAny<string>())).Returns(true);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.name).WithErrorMessage("Name must not exceed 300 characters");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_AuthorIsEmpty_ReturnsValidationError(string? author)
    {
        // Arrange
        var request = new AddBookRequestDto
        {
            name = "Book Name",
            author = author ?? "",
            issueyear = 2020,
            isbn = "9780131101630",
            numberOfPieces = 5
        };

        _mockIsbnValidationService.Setup(x => x.ValidateIsbn13(It.IsAny<string>())).Returns(true);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.author).WithErrorMessage("Author is required");
    }

    [Fact]
    public void Validate_AuthorTooLong_ReturnsValidationError()
    {
        // Arrange
        var request = new AddBookRequestDto
        {
            name = "Book Name",
            author = new string('a', 201),
            issueyear = 2020,
            isbn = "9780131101630",
            numberOfPieces = 5
        };

        _mockIsbnValidationService.Setup(x => x.ValidateIsbn13(It.IsAny<string>())).Returns(true);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.author).WithErrorMessage("Author must not exceed 200 characters");
    }

    [Theory]
    [InlineData(999)]
    [InlineData(9999)]
    public void Validate_IssueYearTooLow_ReturnsValidationError(int issueYear)
    {
        // Arrange
        var request = new AddBookRequestDto
        {
            name = "Book Name",
            author = "Author",
            issueyear = issueYear,
            isbn = "9780131101630",
            numberOfPieces = 5
        };

        _mockIsbnValidationService.Setup(x => x.ValidateIsbn13(It.IsAny<string>())).Returns(true);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.issueyear);
    }

    [Fact]
    public void Validate_IssueYearTooHigh_ReturnsValidationError()
    {
        // Arrange
        var request = new AddBookRequestDto
        {
            name = "Book Name",
            author = "Author",
            issueyear = DateTime.Now.Year + 1,
            isbn = "9780131101630",
            numberOfPieces = 5
        };

        _mockIsbnValidationService.Setup(x => x.ValidateIsbn13(It.IsAny<string>())).Returns(true);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.issueyear);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_IsbnIsEmpty_ReturnsValidationError(string? isbn)
    {
        // Arrange
        var request = new AddBookRequestDto
        {
            name = "Book Name",
            author = "Author",
            issueyear = 2020,
            isbn = isbn ?? "",
            numberOfPieces = 5
        };

        _mockIsbnValidationService.Setup(x => x.ValidateIsbn13(It.IsAny<string>())).Returns(true);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.isbn).WithErrorMessage("ISBN is required");
    }

    [Fact]
    public void Validate_InvalidIsbn_ReturnsValidationError()
    {
        // Arrange
        var request = new AddBookRequestDto
        {
            name = "Book Name",
            author = "Author",
            issueyear = 2020,
            isbn = "invalid-isbn",
            numberOfPieces = 5
        };

        _mockIsbnValidationService.Setup(x => x.ValidateIsbn13("invalid-isbn")).Returns(false);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.isbn).WithErrorMessage("ISBN must be a valid ISBN-13 format");
    }

    [Fact]
    public void Validate_NegativeNumberOfPieces_ReturnsValidationError()
    {
        // Arrange
        var request = new AddBookRequestDto
        {
            name = "Book Name",
            author = "Author",
            issueyear = 2020,
            isbn = "9780131101630",
            numberOfPieces = -1
        };

        _mockIsbnValidationService.Setup(x => x.ValidateIsbn13(It.IsAny<string>())).Returns(true);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.numberOfPieces);
    }

    [Fact]
    public void Validate_ValidRequest_ReturnsNoErrors()
    {
        // Arrange
        var request = new AddBookRequestDto
        {
            name = "Book Name",
            author = "Author",
            issueyear = 2020,
            isbn = "9780131101630",
            numberOfPieces = 5
        };

        _mockIsbnValidationService.Setup(x => x.ValidateIsbn13("9780131101630")).Returns(true);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ZeroNumberOfPieces_ReturnsNoErrors()
    {
        // Arrange
        var request = new AddBookRequestDto
        {
            name = "Book Name",
            author = "Author",
            issueyear = 2020,
            isbn = "9780131101630",
            numberOfPieces = 0
        };

        _mockIsbnValidationService.Setup(x => x.ValidateIsbn13(It.IsAny<string>())).Returns(true);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}

