using ManokshaApi.Data;
using ManokshaApi.Models;
using ManokshaApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

// --------------------
// 1️⃣ Add Services
// --------------------
builder.Services.AddControllers().AddNewtonsoftJson();

// ✅ Use SQL Server instead of InMemory database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ✅ Dependency Injection for services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IPaymentService, RazorpayServiceStub>();

// ✅ Register Firebase Storage Service globally (no constructor arguments)
builder.Services.AddSingleton<FirebaseStorageService>();

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
// 4️⃣ Product Endpoints
// --------------------

// GET /products (search, filter, sort, pagination)
app.MapGet("/products", async (HttpRequest req, IProductService productService) =>
{
    var search = req.Query["search"].ToString();
    var category = req.Query["category"].ToString();
    decimal.TryParse(req.Query["minPrice"], out var minPrice);
    decimal.TryParse(req.Query["maxPrice"], out var maxPrice);
    var sort = req.Query["sort"].ToString();
    int.TryParse(req.Query["page"], out var page);
    int.TryParse(req.Query["pageSize"], out var pageSize);

    page = page <= 0 ? 1 : page;
    pageSize = pageSize <= 0 ? 10 : pageSize;

    var result = await productService.GetProductsAsync(
        search, category, minPrice, maxPrice, sort, page, pageSize);

    return Results.Ok(result);
});

// GET /products/{id}
app.MapGet("/products/{id:guid}", async (Guid id, IProductService productService) =>
{
    var product = await productService.GetProductByIdAsync(id);
    return product is not null ? Results.Ok(product) : Results.NotFound();
});

// POST /products (multipart/form-data with Firebase image upload)
app.MapPost("/products", async (HttpRequest req, IProductService productService, FirebaseStorageService firebaseService) =>
{
    var form = await req.ReadFormAsync();

    var name = form["name"].ToString();
    var category = form["category"].ToString();
    decimal.TryParse(form["price"], out var price);
    int.TryParse(form["stock"], out var stock);
    bool.TryParse(form["isActive"], out var isActive);

    var file = form.Files.FirstOrDefault();
    string imageUrl = "";

    if (file != null && file.Length > 0)
    {
        using var stream = file.OpenReadStream();
        imageUrl = await firebaseService.UploadFileAsync(stream, file.FileName, file.ContentType);
    }

    var product = new Product
    {
        Name = name,
        Category = category,
        Price = price,
        Stock = stock,
        IsActive = isActive,
        ImageUrl = imageUrl
    };

    var createdProduct = await productService.CreateProductAsync(product);
    return Results.Created($"/products/{createdProduct.Id}", createdProduct);
});

// PUT /products/{id}
app.MapPut("/products/{id:guid}", async (Guid id, Product product, IProductService productService) =>
{
    var updatedProduct = await productService.UpdateProductAsync(id, product);
    return updatedProduct is not null ? Results.Ok(updatedProduct) : Results.NotFound();
});

// DELETE /products/{id}
app.MapDelete("/products/{id:guid}", async (Guid id, IProductService productService) =>
{
    var deleted = await productService.DeleteProductAsync(id);
    return deleted ? Results.Ok() : Results.NotFound();
});

// PATCH /products/{id}/active
app.MapPatch("/products/{id:guid}/active", async (Guid id, bool isActive, IProductService productService) =>
{
    var ok = await productService.SetProductActiveStateAsync(id, isActive);
    return ok ? Results.NoContent() : Results.NotFound();
});

// --------------------
// 5️⃣ Run App
// --------------------
app.Run();
