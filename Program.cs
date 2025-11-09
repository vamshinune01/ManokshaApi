using ManokshaApi.Data;
using ManokshaApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// --------------------
// 1️⃣ Add Services
// --------------------
builder.Services.AddControllers().AddNewtonsoftJson();

// ✅ SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ✅ Dependency Injection
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IPaymentService, RazorpayServiceStub>();
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddSingleton<FirebaseStorageService>();

// ✅ Swagger configuration
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

// ✅ Add Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// --------------------
// 2️⃣ Global Exception Handling Middleware
// --------------------
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var error = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        var response = new
        {
            message = "Internal server error occurred.",
            detail = error?.Message
        };

        await context.Response.WriteAsJsonAsync(response);
    });
});

// --------------------
// 3️⃣ Swagger Middleware
// --------------------
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Manoksha Mini Catalog API v1");
    c.RoutePrefix = string.Empty; // Swagger at root
});

app.UseHttpsRedirection();
app.UseAuthorization();

// --------------------
// 4️⃣ Map Controllers
// --------------------
app.MapControllers();

// --------------------
// 5️⃣ Run App
// --------------------
app.Run();
