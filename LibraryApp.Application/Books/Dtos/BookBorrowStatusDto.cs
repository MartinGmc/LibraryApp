using System.Text.Json.Serialization;

namespace LibraryApp.Application.Books.Dtos;

public class BookBorrowStatusDto
{
    [JsonPropertyName("bookId")]
    public string BookId { get; set; } = string.Empty;

    [JsonPropertyName("isBorrowedByUser")]
    public bool IsBorrowedByUser { get; set; }

    [JsonPropertyName("activeLoanCount")]
    public int ActiveLoanCount { get; set; }

    [JsonPropertyName("availableCount")]
    public int AvailableCount { get; set; }
}

