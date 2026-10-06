namespace Savdonoma.Data.Services.Categories
{
    public interface ICategoryService
    {
        Task<List<CategroyDto>> GetListAsync(CancellationToken cancellation);
        Task<CategroyDto> GetAsync(int Id, CancellationToken cancellation);
        Task<CategroyDto> CreateAsync(CreateCategoryDto dto, CancellationToken cancellation);
        Task<CategroyDto> UpdateAsync(UpdateCategoryDto dto, CancellationToken cancellation);
        Task<bool> Delete(int id, CancellationToken cancellation);
    }
}
