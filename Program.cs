using ManokshaApi.Data;
using ManokshaApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// --------------------
// 1️⃣ Add Services
// --------------------
builder.Services.AddControllers().AddNewtonsoftJson();

// SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Dependency Injection
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IPaymentService, RazorpayServiceStub>();
builder.Services.AddSingleton<FirebaseStorageService>();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Manoksha Mini Catalog API",
        Version = "v1",
        Description = "API for Mini Product Catalog - Manoksha Collections"
    });
});

// --------------------
// 2️⃣ Build App
// --------------------
var app = builder.Build();

// --------------------
// 3️⃣ Middleware
// --------------------
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Manoksha Mini Catalog API v1");
    c.RoutePrefix = string.Empty;
});

app.UseHttpsRedirection();
app.UseAuthorization();

// --------------------
// 4️⃣ Map Controllers
// --------------------
app.MapControllers(); // <-- This is enough for UsersController, ProductController, ReturnController, etc.

// --------------------
// 5️⃣ Run App
// --------------------
app.Run();
