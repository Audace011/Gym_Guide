using GymGuide.Data;
using GymGuide.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var rawConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");

var isSqlite = builder.Configuration.GetValue<bool?>("UseSqlite") ?? true;
if (rawConnectionString != null && rawConnectionString.Contains("Server=") && !builder.Configuration.GetValue<bool>("UseSqlite"))
{
    isSqlite = false;
}

var connectionString = isSqlite
    ? (rawConnectionString != null && rawConnectionString.Contains(".db") ? rawConnectionString : "Data Source=GymGuide.db")
    : rawConnectionString!;

if (isSqlite)
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

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Register Business Services (conserving both teammate services and core interfaces)
builder.Services.AddScoped<IBmiService, BmiService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddScoped<RecommendationService>();
builder.Services.AddScoped<WorkoutService>();
builder.Services.AddScoped<ITrainerAssignmentService, TrainerAssignmentService>();

builder.Services.AddRazorPages();

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
