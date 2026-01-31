using MageKnightOnline.Core.GameState;
using MageKnightOnline.Core.Services;
using MageKnightOnline.Data;
using MageKnightOnline.Web.Components;
using MageKnightOnline.Web.Hubs;
using MageKnightOnline.Web.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Enable detailed errors for Blazor circuits in development
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddServerSideBlazor()
        .AddCircuitOptions(options => options.DetailedErrors = true);
}

// SignalR for real-time communication
builder.Services.AddSignalR();

// Database - SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<MageKnightDbContext>(options =>
    options.UseSqlite(connectionString));

// Game Definition Service
builder.Services.AddSingleton<IGameDefinitionService>(sp =>
{
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var definitionsPath = Path.Combine(env.WebRootPath, "data", "definitions");
    return new GameDefinitionService(definitionsPath);
});

// Game State Initializer
builder.Services.AddScoped<GameStateInitializer>(sp =>
{
    var definitionService = sp.GetRequiredService<IGameDefinitionService>();
    return new GameStateInitializer(definitionService);
});

// Auth State Service - scoped per circuit (Blazor Server session)
builder.Services.AddScoped<AuthStateService>();

// Notification Service - scoped per circuit
builder.Services.AddScoped<MageKnightOnline.Web.Services.NotificationService>();

// Game Service
builder.Services.AddScoped<IGameService>(sp =>
{
    var dbContext = sp.GetRequiredService<MageKnightDbContext>();
    var definitionService = sp.GetRequiredService<IGameDefinitionService>();
    return new GameService(dbContext, definitionService);
});

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<MageKnightDbContext>("database");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// Auto-migrate database
// In development: always migrate
// In production: migrate on startup (Azure will handle this)
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<MageKnightDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    logger.LogInformation("Applying database migrations...");
    db.Database.Migrate();
    logger.LogInformation("Database migrations completed successfully.");
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred while migrating the database.");
    // In production, we might want to continue anyway if migrations fail
    // but log the error for investigation
    if (!app.Environment.IsDevelopment())
    {
        logger.LogWarning("Continuing despite migration error. Please check database connection.");
    }
    else
    {
        throw; // In development, fail fast
    }
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

// Rensa pågående spel i databasen vid start om miljövariabeln är satt
if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CLEAR_ONGOING_GAMES")))
{
    try
    {
        using var scope = app.Services.CreateScope();
        var gameService = scope.ServiceProvider.GetRequiredService<IGameService>();
        var removed = await gameService.ClearOngoingGamesAsync();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Rensade {Count} pågående spel från databasen.", removed);
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Kunde inte rensa pågående spel.");
    }
}

app.MapStaticAssets();
app.MapHub<GameHub>("/gamehub");
app.MapHealthChecks("/health");

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
