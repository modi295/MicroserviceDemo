namespace Contracts
{
     public record ProductQuery
    {
        public record GetProductsByUserId(int UserId);
        public record UserProductsResponse(int UserId, List<ProductDto> Products);
    }
}