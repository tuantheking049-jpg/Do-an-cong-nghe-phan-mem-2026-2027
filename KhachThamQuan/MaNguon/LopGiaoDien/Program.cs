using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using VisitorGuide.Api.Controllers;
using VisitorGuide.Api.Services;
using VisitorGuide.Application.Services;
using VisitorGuide.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddSingleton<IOnlineVisitorTracker, OnlineVisitorTracker>();
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "VisitorGuide.Admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(4);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddJsonFileDataAccess(
    Path.Combine(builder.Environment.ContentRootPath, "App_Data", "places.json"));
var connectionString = builder.Configuration.GetConnectionString("VisitorGuide")
    ?? throw new InvalidOperationException(
        "Connection string 'VisitorGuide' is missing. Configure ConnectionStrings:VisitorGuide.");
builder.Services.AddSingleton(new SqlServerApplicationStore(connectionString));
builder.Services.AddSingleton(new SqlServerRelationalStore(connectionString));
builder.Services.AddSingleton<ChonNgonNgu>();
builder.Services.AddScoped<TimDiaDiem>();
builder.Services.AddScoped<XemDiaDiem>();
builder.Services.AddScoped<TimViTri>();
builder.Services.AddScoped<XemChiTiet>();
builder.Services.AddScoped<NgheNoiDung>();
builder.Services.AddSingleton<TamDung>();
builder.Services.AddSingleton<NgheLai>();
builder.Services.AddScoped<IVisitorGuideService, VisitorGuideService>();

var app = builder.Build();

var applicationStore = app.Services.GetRequiredService<SqlServerApplicationStore>();
var relationalStore = app.Services.GetRequiredService<SqlServerRelationalStore>();
await applicationStore.EnsureReadyAsync();
var relationalStateReady = await applicationStore.IsRelationalStateReadyAsync();
var legacyAdminState = relationalStateReady
    ? null
    : await applicationStore.LoadSectionAsync("adminState");
if (!string.IsNullOrWhiteSpace(legacyAdminState))
{
    AdminDiaDiemController.InitializePersistedState(legacyAdminState);
}
else
{
    AdminDiaDiemController.InitializePersistedState(await relationalStore.LoadAdminStateJsonAsync());
}
await relationalStore.SaveAdminStateAsync(
    AdminDiaDiemController.ExportPersistedState(),
    deleteMissingVoices: string.IsNullOrWhiteSpace(legacyAdminState),
    syncPlayCounts: !relationalStateReady);
await DatabaseInitializer.InitializeAsync(app.Services);

if (!relationalStateReady)
{
    if (!string.IsNullOrWhiteSpace(legacyAdminState))
    {
        await relationalStore.SaveAdminStateAsync(
            AdminDiaDiemController.ExportPersistedState(),
            deleteMissingVoices: false,
            syncPlayCounts: true);
    }

    AdminDiaDiemController.InitializePersistedState(await relationalStore.LoadAdminStateJsonAsync());
    await relationalStore.EnsureDefaultAudioFilesAsync();
    AdminDiaDiemController.InitializePersistedState(await relationalStore.LoadAdminStateJsonAsync());
    await applicationStore.MarkRelationalStateReadyAsync();
}
else
{
    AdminDiaDiemController.InitializePersistedState(await relationalStore.LoadAdminStateJsonAsync());
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
{
    if (string.Equals(context.Request.Path.Value, "/admin_dashboard.html", StringComparison.OrdinalIgnoreCase)
        && context.User.Identity?.IsAuthenticated != true)
    {
        context.Response.Redirect("/admin_login.html");
        return;
    }

    await next();
});

app.UseDefaultFiles(new DefaultFilesOptions
{
    DefaultFileNames = { "index.html" }
});
app.UseStaticFiles();
var apiRequestLock = new SemaphoreSlim(1, 1);
app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    var independentTraffic = path.StartsWithSegments("/api/thongke/online")
        || path.StartsWithSegments("/api/thongke/tang-nghe");
    if (!path.StartsWithSegments("/api") || independentTraffic)
    {
        await next();
        return;
    }

    await apiRequestLock.WaitAsync(context.RequestAborted);
    try
    {
        var currentRelationalStore = context.RequestServices.GetRequiredService<SqlServerRelationalStore>();
        var dbContext = context.RequestServices.GetRequiredService<VisitorGuideDbContext>();
        await dbContext.ReloadFromSqlAsync(
            await currentRelationalStore.LoadPlacesAsync(context.RequestAborted),
            context.RequestAborted);
        AdminDiaDiemController.InitializePersistedState(
            await currentRelationalStore.LoadAdminStateJsonAsync(context.RequestAborted));
        await next();
    }
    finally
    {
        apiRequestLock.Release();
    }
});
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program { }
