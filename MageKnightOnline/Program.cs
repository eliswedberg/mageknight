using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MageKnightOnline.Components;
using MageKnightOnline.Components.Account;
using MageKnightOnline.Data;
using MageKnightOnline.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => 
    {
        options.SignIn.RequireConfirmedAccount = false;
        // Relax password requirements for development
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequiredLength = 4;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

// Add custom services
builder.Services.AddScoped<GameService>();
builder.Services.AddScoped<MageKnightGameService>();
builder.Services.AddScoped<GameDataSeeder>();
builder.Services.AddScoped<TurnManagementService>();
builder.Services.AddScoped<CardManagementService>();
builder.Services.AddScoped<CombatResolutionService>();
builder.Services.AddScoped<TileDataService>();
builder.Services.AddScoped<HexGridManager>();
builder.Services.AddScoped<ManaSourceService>();
builder.Services.AddScoped<DayNightService>();

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();
    
    // Ensure game-related tables exist (for existing DBs created before adding game models)
    try
    {
        await context.Database.OpenConnectionAsync();
        using (var cmd = context.Database.GetDbConnection().CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(1) FROM sqlite_master WHERE type='table' AND name='GameSessions'";
            var result = await cmd.ExecuteScalarAsync();
            var hasGameTables = (result is long l && l > 0) || (result is int i && i > 0);
            if (!hasGameTables)
            {
                var sqlPath = Path.Combine(app.Environment.ContentRootPath, "Data", "Migrations", "20241206000000_AddGameModels.sql");
                if (File.Exists(sqlPath))
                {
                    var sql = await File.ReadAllTextAsync(sqlPath);
                    await context.Database.ExecuteSqlRawAsync(sql);
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: Could not ensure game tables exist: {ex.Message}");
    }
    finally
    {
        try { await context.Database.CloseConnectionAsync(); } catch { }
    }

    // Create test user
    try
    {
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var testUser = await userManager.FindByEmailAsync("eliwe1@hotmail.com");
        if (testUser == null)
        {
            testUser = new ApplicationUser
            {
                UserName = "eliwe1@hotmail.com",
                Email = "eliwe1@hotmail.com",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(testUser, "elis123");
            if (result.Succeeded)
            {
                Console.WriteLine("Test user created: eliwe1@hotmail.com / elis123");
            }
            else
            {
                Console.WriteLine($"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: Could not create test user: {ex.Message}");
    }

    // Seed data after database is created
    try
    {
        var seeder = scope.ServiceProvider.GetRequiredService<GameDataSeeder>();
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        // Log error but don't stop application startup
        Console.WriteLine($"Warning: Could not seed data: {ex.Message}");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseHttpsRedirection();
}


app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();
