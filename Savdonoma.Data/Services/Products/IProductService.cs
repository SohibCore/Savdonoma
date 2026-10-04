namespace Savdonoma.Data.Services.Products
{
    public interface IProductService
    {
        Task<List<ProductDto>> SearchAsync(string? text, int take = 50);
        Task<List<ProductDto>> GetListAsync();
        Task<ProductDto> GetAsync(int Id);
        Task<ProductDto> CreateAsync(CreateProductDto dto);
        Task<ProductDto> UpdateAsync(int Id, UpdateProductDto dto);
        Task DeactivateAsync(int Id);
    }
}
