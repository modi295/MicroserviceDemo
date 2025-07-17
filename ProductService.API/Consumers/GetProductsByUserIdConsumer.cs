using MassTransit;
using Contracts;
using ProductService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ProductService.API.Consumers
{
    public class GetProductsByUserIdConsumer : IConsumer<ProductQuery.GetProductsByUserId>
    {
        private readonly ProductDbContext _db;

        public GetProductsByUserIdConsumer(ProductDbContext db)
        {
            _db = db;
        }

        public async Task Consume(ConsumeContext<ProductQuery.GetProductsByUserId> context)
        {
            var userId = context.Message.UserId;

            var products = await _db.Products
                .Where(p => p.UserId == userId)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    UserId = p.UserId
                })
                .ToListAsync();

            await context.RespondAsync(new ProductQuery.UserProductsResponse(userId, products));
        }
    }
}
