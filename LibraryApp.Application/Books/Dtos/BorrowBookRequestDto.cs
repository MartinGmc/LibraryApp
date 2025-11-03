using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LibraryApp.Application.Books.Dtos;

public class BorrowBookRequestDto
{
    [Required(ErrorMessage = "BookId is required")]
    [JsonPropertyName("bookId")]
    public string BookId { get; set; } = string.Empty;
}

