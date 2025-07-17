using Microsoft.EntityFrameworkCore;
using ProductService.Infrastructure.Data;
using ProductService.Domain.Entities;
using MassTransit;
using ProductService.API.Consumers;
using Contracts;

var builder = WebApplication.CreateBuilder(args);

// Add DB Context
builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// MassTransit + RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<UserCreatedConsumer>();
    x.AddConsumer<GetProductsByUserIdConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });


        cfg.ReceiveEndpoint("user-created-queue", e =>
       {
           e.ConfigureConsumer<UserCreatedConsumer>(ctx);

       });
        cfg.ReceiveEndpoint("get-products-by-user-id-queue", e =>
        {
            e.ConfigureConsumer<GetProductsByUserIdConsumer>(ctx);
            e.SetQueueArgument("durable", true);
        });
    });
});

var app = builder.Build();

// Middleware
app.UseSwagger();
app.UseSwaggerUI();

// API Endpoints
app.MapGet("/api/products", async (int? userId, ProductDbContext db) =>
{
var query = db.Products.AsQueryable();

if (userId.HasValue)
{
    query = query.Where(p => p.UserId == userId.Value);
}

var products = await query.ToListAsync();

return Results.Ok(products);
});


app.MapPost("/api/products", async (Product product, ProductDbContext db, IPublishEndpoint publishEndpoint) =>
{
    var exists = await db.Products.AnyAsync(p => p.Id == product.Id);
    if (exists)
        return Results.Conflict($"Product with Id {product.Id} already exists.");

    db.Products.Add(product);
    await db.SaveChangesAsync();

    await publishEndpoint.Publish(new ProductCreatedEvent(product.Id, product.Name, product.Price, product.UserId));
    return Results.Created($"/api/products/{product.Id}", product);
});

app.Run();
