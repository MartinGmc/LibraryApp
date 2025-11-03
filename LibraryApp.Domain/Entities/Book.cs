namespace LibraryApp.Domain.Entities;

public class Book
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public int IssueYear { get; set; }
    public string ISBN { get; set; } = string.Empty;
    public int NumberOfPieces { get; set; }
}

