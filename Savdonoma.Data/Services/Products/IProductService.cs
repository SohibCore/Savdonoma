namespace Savdonoma.Data.Services.Products
{
    public interface IProductService
    {
        Task<List<ProductDto>> SearchAsync(string? text, CancellationToken cancellation, int take = 50);
        Task<List<ProductDto>> GetListAsync(CancellationToken cancellation);
        Task<ProductDto> GetAsync(int Id, CancellationToken cancellation);
        Task<ProductDto> CreateAsync(CreateProductDto dto, CancellationToken cancellation);
        Task<ProductDto> UpdateAsync(UpdateProductDto dto, CancellationToken cancellation);
        Task DeactivateAsync(int Id, CancellationToken cancellation);
    }
}
