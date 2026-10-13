using Microsoft.EntityFrameworkCore;

namespace Savdonoma.Data.Services.Authservice
{
    public class AuthService : IAuthservice
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        public AuthService(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<AuthResultDto> Login(LoginDto dto, CancellationToken cancellation)
        {
            await using var db = _factory.CreateDbContext();

            if (string.IsNullOrWhiteSpace(dto.UserName) || string.IsNullOrEmpty(dto.Password))
                throw new UnauthorizedAccessException("Login yoki parol noto‘g‘ri.");

            var userName = dto.UserName.Trim().ToLowerInvariant();

            var user = await db.Users.SingleOrDefaultAsync(x => x.UserName == userName && x.IsActive, cancellation);

            if (user == null)
                throw new UnauthorizedAccessException("Login yoki parol noto‘g‘ri.");

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
            {
                throw new UnauthorizedAccessException("Login yoki parol noto‘g‘ri.");
            }

            return new AuthResultDto
            {
                UserName = userName,
                UserId = user.Id,
                OrgName = user.OrgName,
                UserRole = user.UserRole,
            };
        }
    }
}
