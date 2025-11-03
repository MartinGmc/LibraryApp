using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LibraryApp.Application.Books.Dtos;

public class AddBookRequestDto
{
    [Required(ErrorMessage = "Name is required")]
    [MaxLength(300, ErrorMessage = "Name must not exceed 300 characters")]
    [JsonPropertyName("name")]
    public string name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Author is required")]
    [MaxLength(200, ErrorMessage = "Author must not exceed 200 characters")]
    [JsonPropertyName("author")]
    public string author { get; set; } = string.Empty;

    [Required(ErrorMessage = "Issue Year is required")]
    [Range(1000, 2100, ErrorMessage = "Issue Year must be between 1000 and current year")]
    [JsonPropertyName("issueyear")]
    public int issueyear { get; set; }

    [Required(ErrorMessage = "ISBN is required")]
    [MaxLength(50, ErrorMessage = "ISBN must not exceed 50 characters")]
    [JsonPropertyName("isbn")]
    public string isbn { get; set; } = string.Empty;

    [Required(ErrorMessage = "Number of Pieces is required")]
    [Range(0, int.MaxValue, ErrorMessage = "Number of Pieces must be greater than or equal to 0")]
    [JsonPropertyName("numberOfPieces")]
    public int numberOfPieces { get; set; } = 0;
    // Note: Id is intentionally NOT included - it will be auto-generated as GUID
}

