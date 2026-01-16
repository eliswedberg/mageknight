using System.Security.Cryptography;
using System.Text;
using MageKnightOnline.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace MageKnightOnline.Core.Services;

public class AuthService : IAuthService
{
    private readonly DbContext _dbContext;
    private User? _currentUser;

    public AuthService(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public bool IsAuthenticated => _currentUser != null;
    public User? CurrentUser => _currentUser;

    public async Task<AuthResult> RegisterAsync(string username, string email, string password)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
            return AuthResult.Fail("Username must be at least 3 characters.");

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return AuthResult.Fail("Please enter a valid email address.");

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            return AuthResult.Fail("Password must be at least 6 characters.");

        // Check if username or email already exists
        var users = _dbContext.Set<User>();
        
        if (await users.AnyAsync(u => u.Username.ToLower() == username.ToLower()))
            return AuthResult.Fail("Username is already taken.");

        if (await users.AnyAsync(u => u.Email.ToLower() == email.ToLower()))
            return AuthResult.Fail("Email is already registered.");

        // Create user
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email.ToLower(),
            PasswordHash = HashPassword(password),
            CreatedAt = DateTime.UtcNow
        };

        users.Add(user);
        await _dbContext.SaveChangesAsync();

        _currentUser = user;
        return AuthResult.Ok(user);
    }

    public async Task<AuthResult> LoginAsync(string usernameOrEmail, string password)
    {
        if (string.IsNullOrWhiteSpace(usernameOrEmail))
            return AuthResult.Fail("Please enter your username or email.");

        if (string.IsNullOrWhiteSpace(password))
            return AuthResult.Fail("Please enter your password.");

        var users = _dbContext.Set<User>();
        var normalizedInput = usernameOrEmail.ToLower();

        var user = await users.FirstOrDefaultAsync(u => 
            u.Username.ToLower() == normalizedInput || 
            u.Email.ToLower() == normalizedInput);

        if (user == null)
            return AuthResult.Fail("Invalid username/email or password.");

        if (!VerifyPassword(password, user.PasswordHash))
            return AuthResult.Fail("Invalid username/email or password.");

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        _currentUser = user;
        return AuthResult.Ok(user);
    }

    public Task LogoutAsync()
    {
        _currentUser = null;
        return Task.CompletedTask;
    }

    public Task<User?> GetCurrentUserAsync()
    {
        return Task.FromResult(_currentUser);
    }

    // Simple password hashing - in production use BCrypt or similar
    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "MageKnightSalt2024"));
        return Convert.ToBase64String(bytes);
    }

    private static bool VerifyPassword(string password, string hash)
    {
        return HashPassword(password) == hash;
    }
}
