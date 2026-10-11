
var builder = WebApplication.CreateBuilder(args);

// Đăng ký Controller cho các API
builder.Services.AddControllers();

// Đăng ký dịch vụ khám phá endpoint
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Chưa bật Swagger ở bước này
}

app.UseHttpsRedirection();

app.UseAuthorization();

// Ánh xạ các Controller
app.MapControllers();

app.Run();
