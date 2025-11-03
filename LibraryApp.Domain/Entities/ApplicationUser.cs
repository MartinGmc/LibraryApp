using LibraryApp.Domain.Enums;

namespace LibraryApp.Domain.Entities;

public class ApplicationUser
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public UserType UserType { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public Permissions Permissions { get; set; }
}

