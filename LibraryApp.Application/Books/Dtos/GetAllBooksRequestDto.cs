namespace LibraryApp.Application.Books.Dtos;

public class GetAllBooksRequestDto
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Name { get; set; }
    public string? Author { get; set; }
    public string? ISBN { get; set; }
}

