using Savdonoma.Data.Services.Authservice;

namespace Savdonoma.Data.Services.UserService
{
    public interface IUserService
    {
        Task<bool> HasUsersAsync(CancellationToken cancellation);
        Task<AuthResultDto> RegisterFirstOwnerAsync(CreateUserDto dto, CancellationToken cancellation);
        Task<List<UserDto>> GetListAsync(CancellationToken cancellation);
        Task<UserDto> GetAsync(int Id, CancellationToken cancellation);
        Task<UserDto> CreateEmployeeAsync(CreateUserDto dto, CancellationToken cancellation);
        Task<UserDto> UpdateAsync(UpdateUserDto dto, CancellationToken cancellation);
        Task DeleteAsync(int Id, CancellationToken cancellation);
    }
}
