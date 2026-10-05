using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using RealEstatePMS.Data;
using RealEstatePMS.Models.Entities;
using RealEstatePMS.Repositories;
using RealEstatePMS.Services;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Railway (and most container hosts) inject the port to listen on via PORT.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Database configuration
// Checks for Railway PostgreSQL URL (DATABASE_URL / POSTGRES_URL), configured connection string, or falls back to SQLite.
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? Environment.GetEnvironmentVariable("POSTGRES_URL")
    ?? Environment.GetEnvironmentVariable("DATABASE_PUBLIC_URL");

var configuredConnection = builder.Configuration.GetConnectionString("DefaultConnection");

string? npgsqlConnection = null;

if (!string.IsNullOrWhiteSpace(databaseUrl))
{
    npgsqlConnection = FormatPostgreSqlUrl(databaseUrl);
}
else if (!string.IsNullOrWhiteSpace(configuredConnection) &&
         !configuredConnection.Contains("localhost", StringComparison.OrdinalIgnoreCase) &&
         (configuredConnection.Contains("postgres", StringComparison.OrdinalIgnoreCase) ||
          configuredConnection.Contains("Host=", StringComparison.OrdinalIgnoreCase)))
{
    npgsqlConnection = configuredConnection.StartsWith("postgres", StringComparison.OrdinalIgnoreCase)
        ? FormatPostgreSqlUrl(configuredConnection)
        : configuredConnection;
}

if (!string.IsNullOrWhiteSpace(npgsqlConnection))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(npgsqlConnection));
}
else
{
    // SQLite local file database (ideal zero-config fallback for Railway or local dev without PostgreSQL)
    var dataFolder = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
    Directory.CreateDirectory(dataFolder);
    var sqliteDbPath = Path.Combine(dataFolder, "realestate.db");
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite($"Data Source={sqliteDbPath}"));
}

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
});

// Repositories & Services
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ISalesService, SalesService>();
builder.Services.AddScoped<IRentalService, RentalService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<IBookingRequestService, BookingRequestService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddMemoryCache();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Railway terminates HTTPS at its edge proxy and forwards plain HTTP to the
// container, so trust its X-Forwarded-* headers instead of redirecting here.
var forwardedHeaderOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedHeaderOptions.KnownNetworks.Clear();
forwardedHeaderOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeaderOptions);

if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PORT")))
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Public}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    try
    {
        await DbInitializer.SeedAsync(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred during database initialization/seeding.");
    }
}

app.Run();

static string FormatPostgreSqlUrl(string rawUrl)
{
    if (string.IsNullOrWhiteSpace(rawUrl))
        return rawUrl;

    if (rawUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
        rawUrl.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        try
        {
            var uri = new Uri(rawUrl);
            var userInfo = uri.UserInfo.Split(':');
            var user = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "";
            var pass = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
            var port = uri.Port > 0 ? uri.Port : 5432;
            var database = uri.AbsolutePath.TrimStart('/');

            var sb = new System.Text.StringBuilder();
            sb.Append($"Host={uri.Host};Port={port};Database={database};Username={user};Password={pass};");
            sb.Append("Trust Server Certificate=true;Include Error Detail=true;");
            return sb.ToString();
        }
        catch
        {
            return rawUrl;
        }
    }

    return rawUrl;
}
