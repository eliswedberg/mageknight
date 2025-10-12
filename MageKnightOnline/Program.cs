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

builder.Services.AddControllers();


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
        options.SignIn.RequireConfirmedEmail = false;
        options.User.RequireUniqueEmail = false;
        // Relax password requirements for development
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequiredLength = 6;
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
builder.Services.AddScoped<MapTileService>();
builder.Services.AddScoped<ActionCardService>();
builder.Services.AddScoped<TurnManagementService>();
builder.Services.AddScoped<CombatService>();
builder.Services.AddScoped<MovementService>();
builder.Services.AddScoped<SiteService>();

var app = builder.Build();

    // Ensure database is created and migrated
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();

    // Create test user
    try
    {
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        // Check if the problematic user ID exists
        var problematicUserId = "4033b8c6-0d58-45f6-999a-41f212b15b04";
        var existingUser = await userManager.FindByIdAsync(problematicUserId);
        if (existingUser == null)
        {
            // Create user with the specific ID that's causing issues
            var testUser = new ApplicationUser
            {
                Id = problematicUserId,
                UserName = "eliwe1@hotmail.com",
                Email = "eliwe1@hotmail.com",
                EmailConfirmed = true,
                NormalizedUserName = "ELIWE1@HOTMAIL.COM",
                NormalizedEmail = "ELIWE1@HOTMAIL.COM",
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };
            
            // Set password hash manually
            testUser.PasswordHash = userManager.PasswordHasher.HashPassword(testUser, "elis123");
            
            // Add directly to context to bypass UserManager validation
            context.Users.Add(testUser);
            await context.SaveChangesAsync();
            
            Console.WriteLine($"Test user created with specific ID: eliwe1@hotmail.com / elis123 with ID: {testUser.Id}");
        }
        else
        {
            Console.WriteLine($"User with problematic ID already exists: {existingUser.Email} with ID: {existingUser.Id}");
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

app.MapControllers();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();
