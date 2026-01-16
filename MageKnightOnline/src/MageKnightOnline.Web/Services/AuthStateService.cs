using MageKnightOnline.Core.Entities;
using MageKnightOnline.Data;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace MageKnightOnline.Web.Services;

/// <summary>
/// Authentication state service that persists across page reloads using ProtectedSessionStorage.
/// For production, use ASP.NET Core Identity or similar.
/// </summary>
public class AuthStateService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ProtectedSessionStorage _sessionStorage;
    private User? _currentUser;
    private bool _isInitialized;

    private const string UserIdKey = "auth_user_id";

    public AuthStateService(IServiceProvider serviceProvider, ProtectedSessionStorage sessionStorage)
    {
        _serviceProvider = serviceProvider;
        _sessionStorage = sessionStorage;
    }

    public bool IsAuthenticated => _currentUser != null;
    public User? CurrentUser => _currentUser;
    public bool IsInitialized => _isInitialized;

    public event Action? OnAuthStateChanged;

    /// <summary>
    /// Initialize the auth state from session storage. Must be called after render.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        try
        {
            var result = await _sessionStorage.GetAsync<Guid>(UserIdKey);
            if (result.Success && result.Value != Guid.Empty)
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<MageKnightDbContext>();
                _currentUser = await dbContext.Users.FindAsync(result.Value);
            }
        }
        catch
        {
            // Session storage not available (e.g., during prerendering)
        }

        _isInitialized = true;
        OnAuthStateChanged?.Invoke();
    }

    public async Task<AuthResult> RegisterAsync(string username, string email, string password)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
            return AuthResult.Fail("Username must be at least 3 characters.");

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return AuthResult.Fail("Please enter a valid email address.");

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            return AuthResult.Fail("Password must be at least 6 characters.");

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MageKnightDbContext>();

        // Check if username or email already exists
        if (await dbContext.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower()))
            return AuthResult.Fail("Username is already taken.");

        if (await dbContext.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower()))
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

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        _currentUser = user;
        await _sessionStorage.SetAsync(UserIdKey, user.Id);
        OnAuthStateChanged?.Invoke();
        return AuthResult.Ok(user);
    }

    public async Task<AuthResult> LoginAsync(string usernameOrEmail, string password)
    {
        if (string.IsNullOrWhiteSpace(usernameOrEmail))
            return AuthResult.Fail("Please enter your username or email.");

        if (string.IsNullOrWhiteSpace(password))
            return AuthResult.Fail("Please enter your password.");

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MageKnightDbContext>();

        var normalizedInput = usernameOrEmail.ToLower();

        var user = await dbContext.Users.FirstOrDefaultAsync(u =>
            u.Username.ToLower() == normalizedInput ||
            u.Email.ToLower() == normalizedInput);

        if (user == null)
            return AuthResult.Fail("Invalid username/email or password.");

        if (!VerifyPassword(password, user.PasswordHash))
            return AuthResult.Fail("Invalid username/email or password.");

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        _currentUser = user;
        await _sessionStorage.SetAsync(UserIdKey, user.Id);
        OnAuthStateChanged?.Invoke();
        return AuthResult.Ok(user);
    }

    public async Task LogoutAsync()
    {
        _currentUser = null;
        await _sessionStorage.DeleteAsync(UserIdKey);
        OnAuthStateChanged?.Invoke();
    }

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

public class AuthResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public User? User { get; set; }

    public static AuthResult Ok(User user) => new() { Success = true, User = user };
    public static AuthResult Fail(string message) => new() { Success = false, ErrorMessage = message };
}
