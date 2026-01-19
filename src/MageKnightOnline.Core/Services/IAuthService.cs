using MageKnightOnline.Core.Entities;

namespace MageKnightOnline.Core.Services;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(string username, string email, string password);
    Task<AuthResult> LoginAsync(string usernameOrEmail, string password);
    Task LogoutAsync();
    Task<User?> GetCurrentUserAsync();
    bool IsAuthenticated { get; }
    User? CurrentUser { get; }
}

public class AuthResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public User? User { get; set; }

    public static AuthResult Ok(User user) => new() { Success = true, User = user };
    public static AuthResult Fail(string message) => new() { Success = false, ErrorMessage = message };
}
