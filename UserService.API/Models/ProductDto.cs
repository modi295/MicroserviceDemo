namespace UserService.API.Models;

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int UserId { get; set; }

}
