using System.Text.Json.Serialization;

namespace LibraryApp.Application.Books.Dtos;

public class BorrowedBookDto
{
    [JsonPropertyName("loanId")]
    public string LoanId { get; set; } = string.Empty;

    [JsonPropertyName("bookId")]
    public string BookId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;

    [JsonPropertyName("issueYear")]
    public int IssueYear { get; set; }

    [JsonPropertyName("isbn")]
    public string ISBN { get; set; } = string.Empty;

    [JsonPropertyName("borrowedDate")]
    public DateTime BorrowedDate { get; set; }
}

