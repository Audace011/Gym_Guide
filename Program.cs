using GymGuide.Services;
using GymGuide.Data;
using GymGuide.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Database provider selection ──────────────────────────────────────────────
// Priority: DATABASE_URL env var (Render PostgreSQL) → appsettings connection string
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
var defaultConn = builder.Configuration.GetConnectionString("DefaultConnection");

string connectionString;
bool usePostgres = !string.IsNullOrEmpty(databaseUrl) || 
                   (defaultConn != null && (defaultConn.StartsWith("postgres", StringComparison.OrdinalIgnoreCase) || defaultConn.Contains("Host=")));
bool isSqlite = false;

if (usePostgres)
{
    var rawUrl = !string.IsNullOrEmpty(databaseUrl) ? databaseUrl : defaultConn!;
    if (rawUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
        rawUrl.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        var uri = new Uri(rawUrl);
        var userInfo = uri.UserInfo.Split(':', 2);
        var user = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
        var port = uri.Port > 0 ? uri.Port : 5432;
        var database = uri.AbsolutePath.TrimStart('/');

        connectionString = $"Host={uri.Host};Port={port};Database={database};Username={user};Password={password};SSL Mode=Require;Trust Server Certificate=true";
    }
    else
    {
        connectionString = rawUrl;
    }
}
else
{
    // Fallback: use appsettings (SQLite for local dev)
    connectionString = defaultConn ?? "Data Source=GymGuide.db";
    isSqlite = builder.Configuration.GetValue<bool?>("UseSqlite") ?? true;
}

if (usePostgres)
{
    builder.Services.AddDbContext<AuthDbContext>(options =>
        options.UseNpgsql(connectionString));
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else if (isSqlite)
{
    builder.Services.AddDbContext<AuthDbContext>(options =>
        options.UseSqlite(connectionString));
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(connectionString));
}
else
{
    builder.Services.AddDbContext<AuthDbContext>(options =>
        options.UseSqlServer(connectionString));
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));
}

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<AuthDbContext>();

feature/member-bmi-progress
builder.Services.AddControllersWithViews();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddScoped<BmiService>();
builder.Services.AddScoped<IBmiService, BmiService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddScoped<RecommendationService>();
builder.Services.AddScoped<WorkoutService>();
builder.Services.AddScoped<ITrainerAssignmentService, TrainerAssignmentService>();

builder.Services.AddRazorPages();
 main

var app = builder.Build();

// Trust Render's reverse proxy headers (required for HTTPS redirect detection)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

using (var scope = app.Services.CreateScope())
{
    try
    {
        await DbInitializer.SeedAsync(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "An error occurred while seeding the database.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Only redirect to HTTPS when NOT running behind a proxy that already handles it
if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

if (args.Contains("--test"))
{
    using var testScope = app.Services.CreateScope();
    var ctx = testScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var results = await GymGuide.Tests.UnitTestsRunner.RunAllTestsAsync(ctx);
    int passed = results.Count(r => r.Passed);
    Console.WriteLine($"\n================ TEST RESULTS: {passed}/{results.Count} PASSED ================");
    foreach (var r in results)
    {
        Console.WriteLine($"[{(r.Passed ? "PASS" : "FAIL")}] {r.Category} - {r.TestName}: {r.Message}");
    }
    Console.WriteLine("=================================================================\n");
    return;
}

app.Run();
