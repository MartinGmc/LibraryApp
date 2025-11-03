using System.Text.Json.Serialization;

namespace LibraryApp.Application.Books.Dtos;

public class BorrowBookResponseDto
{
    [JsonPropertyName("loanId")]
    public string LoanId { get; set; } = string.Empty;

    [JsonPropertyName("bookId")]
    public string BookId { get; set; } = string.Empty;

    [JsonPropertyName("userId")]
    public int UserId { get; set; }

    [JsonPropertyName("borrowedDate")]
    public DateTime BorrowedDate { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

