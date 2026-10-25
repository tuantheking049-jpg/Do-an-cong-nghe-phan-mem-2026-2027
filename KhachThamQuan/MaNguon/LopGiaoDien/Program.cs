using VisitorGuide.Application.Repositories;
using VisitorGuide.Application.Services;
using VisitorGuide.Infrastructure;
using VisitorGuide.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSqlServerDataAccess(builder.Configuration);
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

await DatabaseInitializer.InitializeAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles(new DefaultFilesOptions
{
    DefaultFileNames = { "index.html" }
});
app.UseStaticFiles();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program { }
