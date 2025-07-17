using Microsoft.EntityFrameworkCore;
using UserService.Infrastructure.Data;
using UserService.Domain.Entities;
using MassTransit;
using Contracts;
using UserService.API.Consumers;

var builder = WebApplication.CreateBuilder(args);

// Register DB Context
builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Swagger & HTTP Client
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient("ProductService", client =>
{
    client.BaseAddress = new Uri("http://localhost:5291"); // ProductService URL used for Http Sync
});

// Add MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ProductCreatedConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        cfg.ReceiveEndpoint("product-created-queue", e =>
        {
            e.ConfigureConsumer<ProductCreatedConsumer>(ctx);
        });
    });
    x.AddRequestClient<ProductQuery.GetProductsByUserId>();
});


var app = builder.Build();

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

// API Endpoints

// Get all users
app.MapGet("/api/users", async (UserDbContext db) =>
    await db.Users.ToListAsync());

// Get users with products (sync via HttpClient)
app.MapGet("/api/users/with-products", async (UserDbContext db, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient("ProductService");

    try
    {
        var users = await db.Users.ToListAsync();

        if (users == null || users.Count == 0)
            return Results.NotFound("No users found.");

        var result = new List<object>();

        foreach (var user in users)
        {
            try
            {
                var products = await client.GetFromJsonAsync<List<ProductDto>>($"/api/products?userId={user.Id}") ?? [];
                result.Add(new
                {
                    User = user,
                    Products = products
                });
            }
            catch (Exception ex)
            {
                result.Add(new
                {
                    User = user,
                    Products = new List<ProductDto>(),
                    Error = ex.Message
                });
            }
        }

        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.Problem("Failed to fetch users or products: " + ex.Message);
    }
});

// Create new user (async event to RabbitMQ)
app.MapPost("/api/users", async (User user, UserDbContext db, IPublishEndpoint publishEndpoint) =>
{
    if (user.Id != 0 && await db.Users.AnyAsync(u => u.Id == user.Id))
        return Results.Conflict("User with this Id already exists.");

    db.Users.Add(user);

    await db.SaveChangesAsync();

    await publishEndpoint.Publish(new UserCreatedEvent(user.Id, user.Name, user.Email));

    return Results.Created($"/api/users/{user.Id}", user);
});

app.MapGet("/api/users/with-products-rmq", async (UserDbContext db, IRequestClient<ProductQuery.GetProductsByUserId> productRequestClient) =>
{
    var users = await db.Users.ToListAsync();

    if (users == null || users.Count == 0)
        return Results.NotFound("No users found.");

    var result = new List<object>();

    foreach (var user in users)
    {
        try
        {
            var response = await productRequestClient.GetResponse<ProductQuery.UserProductsResponse>(new ProductQuery.GetProductsByUserId(user.Id));
            result.Add(new
            {
                User = user,
                response.Message.Products
            });
        }
        catch (Exception ex)
        {
            result.Add(new
            {
                User = user,
                Products = new List<ProductDto>(),
                Error = ex.Message
            });
        }
    }

    return Results.Ok(result);
});

app.Run();
