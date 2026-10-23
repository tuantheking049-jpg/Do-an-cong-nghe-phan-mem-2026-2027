using VisitorGuide.Application.Repositories;
using VisitorGuide.Application.Services;
using VisitorGuide.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = "TaiNguyenTinh"
});
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<ILandmarkRepository, InMemoryLandmarkRepository>();
builder.Services.AddScoped<IVisitorGuideService, VisitorGuideService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles(new DefaultFilesOptions
{
    DefaultFileNames = { "TrangChu.html" }
});
app.UseStaticFiles();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapFallbackToFile("TrangChu.html");

app.Run();

public partial class Program { }
