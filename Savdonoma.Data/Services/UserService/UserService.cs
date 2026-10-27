using Savdonoma.Services;
using Savdonoma.Core.Enums;
using Savdonoma.Core.Entity;
using Savdonoma.Core.Licensing;
using Microsoft.EntityFrameworkCore;
using Savdonoma.Data.Services.Authservice;

namespace Savdonoma.Data.Services.UserService
{
    public class UserService : IUserService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly CurrentUserService _currentUser;
        private readonly ILicenseService _license;

        public UserService(IDbContextFactory<AppDbContext> factory,
            CurrentUserService currentUser,
            ILicenseService license)
        {
            _factory = factory;
            _currentUser = currentUser;
            _license = license;
        }

        public async Task<bool> HasUsersAsync(CancellationToken cancellation)
        {
            await using var db = await _factory.CreateDbContextAsync(cancellation);
            return await db.Users.AnyAsync(cancellation);
        }

        public async Task<AuthResultDto> RegisterFirstOwnerAsync(CreateUserDto dto, CancellationToken cancellation)
        {
            ValidateDetails(dto);

            await using var db = await _factory.CreateDbContextAsync(cancellation);
            if (await db.Users.AnyAsync(cancellation))
                throw new InvalidOperationException("Ro‘yxatdan o‘tish allaqachon yakunlangan.");

            var userName = NormalizeUserName(dto.UserName);
            if (await db.Users.AnyAsync(x => x.UserName == userName, cancellation))
                throw new InvalidOperationException("Bu login band.");

            var owner = new User
            {
                FullName = dto.FullName.Trim(),
                UserName = userName,
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                OrgName = dto.OrgName.Trim(),
                UserRole = UserRole.Boshliq,
                IsActive = true,
                SelfRegister = true,
                CreatedAt = DateTime.UtcNow
            };

            db.Users.Add(owner);
            await db.SaveChangesAsync(cancellation);

            return ToAuthResult(owner);
        }

        public async Task<List<UserDto>> GetListAsync(CancellationToken cancellation)
        {
            EnsureOwner();
            var orgName = _currentUser.OrgName!;
            await using var db = await _factory.CreateDbContextAsync(cancellation);

            return await db.Users
                .Where(x => x.OrgName == orgName && x.UserRole != UserRole.Boshliq)
                .OrderBy(x => x.FullName)
                .Select(x => ToDto(x))
                .ToListAsync(cancellation);
        }

        public async Task<UserDto> GetAsync(int id, CancellationToken cancellation)
        {
            EnsureOwner();
            var orgName = _currentUser.OrgName!;
            await using var db = await _factory.CreateDbContextAsync(cancellation);

            var user = await db.Users
                .SingleOrDefaultAsync(
                    x => x.Id == id && x.OrgName == orgName && x.UserRole != UserRole.Boshliq,
                    cancellation);

            return user is null
                ? throw new InvalidOperationException("Xodim topilmadi.")
                : ToDto(user);
        }

        public async Task<UserDto> CreateEmployeeAsync(CreateUserDto dto, CancellationToken cancellation)
        {
            EnsureOwner();
            _license.EnsureCanWrite();
            if (string.IsNullOrWhiteSpace(dto.FullName)
                || string.IsNullOrWhiteSpace(dto.UserName)
                || string.IsNullOrEmpty(dto.Password))
                throw new InvalidOperationException("Barcha maydonlarni to‘ldiring.");
            if (dto.UserRole == UserRole.Boshliq || !Enum.IsDefined(dto.UserRole))
                throw new InvalidOperationException("Xodim uchun tanlangan rol noto‘g‘ri.");

            var userName = NormalizeUserName(dto.UserName);
            await using var db = await _factory.CreateDbContextAsync(cancellation);
            if (await db.Users.AnyAsync(x => x.UserName == userName, cancellation))
                throw new InvalidOperationException("Bu login band.");

            var employee = new User
            {
                FullName = dto.FullName.Trim(),
                UserName = userName,
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                OrgName = _currentUser.OrgName!,
                UserRole = dto.UserRole,
                IsActive = true,
                SelfRegister = false,
                CreatedAt = DateTime.UtcNow
            };

            db.Users.Add(employee);
            await db.SaveChangesAsync(cancellation);
            return ToDto(employee);
        }

        public async Task DeleteAsync(int id, CancellationToken cancellation)
        {
            EnsureOwner();
            _license.EnsureCanWrite();
            var orgName = _currentUser.OrgName!;
            await using var db = await _factory.CreateDbContextAsync(cancellation);

            var user = await db.Users.SingleOrDefaultAsync(x => x.Id == id && x.OrgName == orgName && x.UserRole != UserRole.Boshliq, cancellation);
            if (user is null)
                throw new InvalidOperationException("Xodim topilmadi.");

            db.Users.Remove(user);
            await db.SaveChangesAsync(cancellation);
        }

        public async Task<UserDto> UpdateAsync(UpdateUserDto dto, CancellationToken cancellation)
        {
            EnsureOwner();
            _license.EnsureCanWrite();
            var orgName = _currentUser.OrgName!;
            await using var db = await _factory.CreateDbContextAsync(cancellation);

            var user = await db.Users.SingleOrDefaultAsync(x => x.Id == dto.Id && x.OrgName == orgName && x.UserRole != UserRole.Boshliq, cancellation);
            if (user is null)
                throw new InvalidOperationException("Xodim topilmadi.");

            if (!string.IsNullOrWhiteSpace(dto.UserName))
            {
                var userName = NormalizeUserName(dto.UserName);
                if (await db.Users.AnyAsync(x => x.Id != dto.Id && x.UserName == userName, cancellation))
                    throw new InvalidOperationException("Bu login band.");
                user.UserName = userName;
            }

            if (!string.IsNullOrWhiteSpace(dto.FullName))
                user.FullName = dto.FullName.Trim();

            if (!string.IsNullOrEmpty(dto.Password))
                user.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            if (dto.UserRole.HasValue)
            {
                if (dto.UserRole == UserRole.Boshliq || !Enum.IsDefined(dto.UserRole.Value))
                    throw new InvalidOperationException("Xodim uchun tanlangan rol noto‘g‘ri.");
                user.UserRole = dto.UserRole.Value;
            }

            user.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellation);
            return ToDto(user);
        }

        private void EnsureOwner()
        {
            if (!_currentUser.IsOwner || string.IsNullOrWhiteSpace(_currentUser.OrgName))
                throw new UnauthorizedAccessException("Bu amal faqat do‘kon egasi uchun ruxsat etilgan.");
        }

        private static void ValidateDetails(CreateUserDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.FullName)
                || string.IsNullOrWhiteSpace(dto.UserName)
                || string.IsNullOrWhiteSpace(dto.OrgName)
                || string.IsNullOrEmpty(dto.Password))
                throw new InvalidOperationException("Barcha maydonlarni to‘ldiring.");
        }

        private static string NormalizeUserName(string userName) =>
            userName.Trim().ToLowerInvariant();

        private static AuthResultDto ToAuthResult(User user) => new()
        {
            UserId = user.Id,
            UserName = user.UserName,
            OrgName = user.OrgName,
            UserRole = user.UserRole
        };

        private static UserDto ToDto(User user) => new()
        {
            Id = user.Id,
            FullName = user.FullName,
            UserName = user.UserName,
            OrgName = user.OrgName,
            UserRole = user.UserRole,
            Password = user.Password
        };
    }
}
