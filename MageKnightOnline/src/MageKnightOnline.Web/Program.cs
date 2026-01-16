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

// SignalR for real-time communication
builder.Services.AddSignalR();

// Database
builder.Services.AddDbContext<MageKnightDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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

// Game Service
builder.Services.AddScoped<IGameService>(sp =>
{
    var dbContext = sp.GetRequiredService<MageKnightDbContext>();
    var definitionService = sp.GetRequiredService<IGameDefinitionService>();
    return new GameService(dbContext, definitionService);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// Auto-migrate database in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<MageKnightDbContext>();
    db.Database.Migrate();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map SignalR hub
app.MapHub<GameHub>("/gamehub");

app.Run();
