using Savdonoma.Data.Services.UserService;

namespace Savdonoma.Data.Services.Authservice
{
    public interface IAuthservice
    {
        Task<AuthResultDto> Login(LoginDto dto, CancellationToken cancellation);
    }
}
