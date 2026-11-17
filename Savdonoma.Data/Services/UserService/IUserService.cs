using Savdonoma.Data.Services.Authservice;

namespace Savdonoma.Data.Services.UserService
{
    public interface IUserService
    {
        Task<bool> HasUsersAsync(CancellationToken cancellation);
        Task<AuthResultDto> RegisterFirstOwnerAsync(CreateUserDto dto, CancellationToken cancellation);

        // Rahbarning o'zi va xodimlar. includeInactive = true bo'lsa, nofaol xodimlar ham qaytadi.
        Task<List<UserDto>> GetListAsync(bool includeInactive, CancellationToken cancellation);
        Task<UserDto> GetAsync(int Id, CancellationToken cancellation);
        Task<UserDto> CreateEmployeeAsync(CreateUserDto dto, CancellationToken cancellation);
        Task<UserDto> UpdateAsync(UpdateUserDto dto, CancellationToken cancellation);
        Task<UserDeleteResult> DeleteAsync(int Id, CancellationToken cancellation);
        Task<UserDto> ReactivateAsync(int Id, CancellationToken cancellation);
    }
}
