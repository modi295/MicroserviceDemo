using MassTransit;
using Contracts;

namespace UserService.API.Consumers
{
    public class ProductCreatedConsumer : IConsumer<ProductCreatedEvent>
    {
        public Task Consume(ConsumeContext<ProductCreatedEvent> context)
        {
            var product = context.Message;
            Console.WriteLine($"📦 UserService received ProductCreatedEvent: {product.Name} - ${product.Price}");
            return Task.CompletedTask;
        }
    }
}