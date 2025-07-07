using Microsoft.EntityFrameworkCore;
using UserService.Infrastructure.Data;
using UserService.Domain.Entities;
using UserService.API.Models;
using MassTransit;
using UserService.API.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient("ProductService", client =>
{
    client.BaseAddress = new Uri("http://localhost:5291");
});

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host("rabbitmq", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/api/users", async (UserDbContext db) =>
    await db.Users.ToListAsync());

// app.MapPost("/api/users", async (User user, UserDbContext db) =>
// {
//     db.Users.Add(user);
//     await db.SaveChangesAsync();
//     return Results.Created($"/api/users/{user.Id}", user);
// });

app.MapGet("/api/users/with-products", async (UserDbContext db, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient("ProductService");

    try
    {
        var users = await db.Users.ToListAsync();

        if (users == null || !users.Any())
            return Results.NotFound("No users found.");

        // 📦 Fetch all products from ProductService
        var products = await client.GetFromJsonAsync<List<ProductDto>>("/api/products");

        if (products == null)
            products = new List<ProductDto>();

        var result = users.Select(user => new
        {
            User = user,
            Products = products.Where(p => p.UserId == user.Id).ToList()
        });

        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.Problem("Failed to fetch products: " + ex.Message);
    }
});

app.MapPost("/api/users", async (User userDto, UserDbContext db, IPublishEndpoint publishEndpoint) =>
{
    var user = new User { Name = userDto.Name, Email = userDto.Email };
    db.Users.Add(user);
    await db.SaveChangesAsync();

    await publishEndpoint.Publish(new UserCreatedEvent(user.Id, user.Name, user.Email));

    return Results.Created($"/api/users/{user.Id}", user);
});

app.Run();
