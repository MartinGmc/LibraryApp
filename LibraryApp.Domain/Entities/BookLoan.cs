namespace LibraryApp.Domain.Entities;

public class BookLoan
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string BookId { get; set; } = string.Empty;
    public int UserId { get; set; }
    public DateTime BorrowedDate { get; set; }
    public DateTime? ReturnedDate { get; set; }
}

