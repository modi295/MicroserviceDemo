using MassTransit;
using Contracts;

namespace ProductService.API.Consumers
{
    public class UserCreatedConsumer : IConsumer<UserCreatedEvent>
    {
        public Task Consume(ConsumeContext<UserCreatedEvent> context)
        {
            var user = context.Message;
            Console.WriteLine($"📨 Received UserCreatedEvent: {user.Name} - {user.Email}");
            return Task.CompletedTask;
        }
    }
}