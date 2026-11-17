using Savdonoma.Core.Entity;

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

        // Asosiy yoki qo'shimcha barcode bo'yicha
        public Task<Product?> FindByBarcodeAsync(string code);

        // Oddiy barcode yoki tarozi barcode'i (ichida vazn bilan)
        Task<BarcodeLookupResult?> ResolveBarcodeAsync(string code, CancellationToken cancellation);

        // Qo'shimcha barcode'lar
        Task<List<string>> GetExtraBarcodesAsync(int productId, CancellationToken cancellation);
        Task AddBarcodeAsync(int productId, string barcode, CancellationToken cancellation);
        Task RemoveBarcodeAsync(int productId, string barcode, CancellationToken cancellation);
    }
}
