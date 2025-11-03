namespace LibraryApp.Application.Books.Dtos;

public class BookResponseDto
{
    public string Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public int IssueYear { get; set; }
    public string ISBN { get; set; } = string.Empty;
    public int NumberOfPieces { get; set; }
}

