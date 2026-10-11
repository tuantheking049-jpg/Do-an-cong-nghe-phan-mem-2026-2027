
namespace Backend.Models;

public class User
{
    public int UserID { get; set; }

    public int? RoleID { get; set; }

    public string? FullName { get; set; }

    public string? Email { get; set; }

    public string? PasswordHash { get; set; }
}
