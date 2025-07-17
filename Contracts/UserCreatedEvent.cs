namespace Contracts
{
    public record UserCreatedEvent(int Id, string Name, string Email);
    public record ProductCreatedEvent(int Id, string Name, decimal Price, int UserId);

}