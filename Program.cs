using ManokshaApi.Data;
using ManokshaApi.Models;
using ManokshaApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// --------------------
// 1️⃣ Add Services
// --------------------
builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase("ManokshaDb"));
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IPaymentService, RazorpayServiceStub>();

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
// 4️⃣ Minimal API Endpoints
// --------------------

// ✅ Unified GET /products (supports search, filter, sort, pagination)
app.MapGet("/products", async (HttpRequest req, IProductService productService) =>
{
    var search = req.Query["search"].ToString();
    var category = req.Query["category"].ToString();
    decimal.TryParse(req.Query["minPrice"], out var minPrice);
    decimal.TryParse(req.Query["maxPrice"], out var maxPrice);
    var sort = req.Query["sort"].ToString(); // e.g. price_asc, price_desc, name_asc
    int.TryParse(req.Query["page"], out var page);
    int.TryParse(req.Query["pageSize"], out var pageSize);

    page = page <= 0 ? 1 : page;
    pageSize = pageSize <= 0 ? 10 : pageSize;

    var result = await productService.GetProductsAsync(
        search, category, minPrice, maxPrice, sort, page, pageSize);

    return Results.Ok(result);
});

// GET by Id
app.MapGet("/products/{id:guid}", async (Guid id, IProductService productService) =>
{
    var product = await productService.GetProductByIdAsync(id);
    return product is not null ? Results.Ok(product) : Results.NotFound();
});

// POST
app.MapPost("/products", async (Product product, IProductService productService) =>
{
    var createdProduct = await productService.CreateProductAsync(product);
    return Results.Created($"/products/{createdProduct.Id}", createdProduct);
});

// PUT
app.MapPut("/products/{id:guid}", async (Guid id, Product product, IProductService productService) =>
{
    var updatedProduct = await productService.UpdateProductAsync(id, product);
    return updatedProduct is not null ? Results.Ok(updatedProduct) : Results.NotFound();
});

// DELETE
app.MapDelete("/products/{id:guid}", async (Guid id, IProductService productService) =>
{
    var deleted = await productService.DeleteProductAsync(id);
    return deleted ? Results.Ok() : Results.NotFound();
});

// PATCH → Activate / Deactivate Product
app.MapPatch("/products/{id:guid}/active", async (Guid id, bool isActive, IProductService productService) =>
{
    var ok = await productService.SetProductActiveStateAsync(id, isActive);
    return ok ? Results.NoContent() : Results.NotFound();
});

// --------------------
// 5️⃣ Seed Sample Data
// --------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (!db.Products.Any())
    {
        db.Products.AddRange(
            new Product { Name = "Gold Necklace", Category = "Jewelry", Price = 1200, ImageUrl = "https://example.com/gold_necklace.jpg" },
            new Product { Name = "Silk Saree", Category = "Clothing", Price = 350, ImageUrl = "https://example.com/silk_saree.jpg" },
            new Product { Name = "Stud Earrings", Category = "Jewelry", Price = 500, ImageUrl = "https://example.com/earrings.jpg" }
        );
        db.SaveChanges();
    }
}

// --------------------
// 6️⃣ Run App
// --------------------
app.Run();
