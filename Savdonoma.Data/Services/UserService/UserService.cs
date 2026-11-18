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
        public const int MinPasswordLength = 4;

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
                INN = string.IsNullOrWhiteSpace(dto.INN) ? "NULL" : dto.INN.Trim(),
                Address = string.IsNullOrWhiteSpace(dto.Address) ? "NULL" : dto.Address.Trim(),
                UserRole = UserRole.Boshliq,
                IsActive = true,
                SelfRegister = true,
                CreatedAt = DateTime.UtcNow
            };

            db.Users.Add(owner);
            await db.SaveChangesAsync(cancellation);

            return ToAuthResult(owner);
        }

        public async Task<List<UserDto>> GetListAsync(bool includeInactive, CancellationToken cancellation)
        {
            EnsureOwner();
            var orgName = _currentUser.OrgName!;
            var currentUserId = _currentUser.UserId;
            await using var db = await _factory.CreateDbContextAsync(cancellation);

            var users = await db.Users
                .AsNoTracking()
                .Where(x => x.OrgName == orgName
                         && (x.UserRole != UserRole.Boshliq || x.Id == currentUserId)
                         && (includeInactive || x.IsActive))
                .ToListAsync(cancellation);

            // Avval rahbar, keyin faol xodimlar, oxirida nofaollar
            return users
                .OrderByDescending(x => x.UserRole == UserRole.Boshliq)
                .ThenByDescending(x => x.IsActive)
                .ThenBy(x => x.FullName)
                .Select(ToDto)
                .ToList();
        }

        public async Task<UserDto> GetAsync(int id, CancellationToken cancellation)
        {
            EnsureOwner();
            await using var db = await _factory.CreateDbContextAsync(cancellation);
            var user = await FindManageableAsync(db, id, cancellation);

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
            ValidatePassword(dto.Password);

            var userName = NormalizeUserName(dto.UserName);
            await using var db = await _factory.CreateDbContextAsync(cancellation);
            await EnsureUserNameFreeAsync(db, userName, exceptId: null, cancellation);

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

        public async Task<UserDeleteResult> DeleteAsync(int id, CancellationToken cancellation)
        {
            EnsureOwner();
            _license.EnsureCanWrite();

            if (id == _currentUser.UserId)
                throw new InvalidOperationException("O‘zingizni o‘chira olmaysiz.");

            var orgName = _currentUser.OrgName!;
            await using var db = await _factory.CreateDbContextAsync(cancellation);

            var user = await db.Users.SingleOrDefaultAsync(x => x.Id == id && x.OrgName == orgName && x.UserRole != UserRole.Boshliq, cancellation);
            if (user is null)
                throw new InvalidOperationException("Xodim topilmadi.");

            // Xodimning sotuvlari bo'lsa, tarix saqlanib qolishi uchun o'chirmaymiz, nofaol qilamiz
            var hasSales = await db.Sales.AnyAsync(s => s.UserId == user.Id, cancellation);
            if (hasSales)
            {
                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellation);
                return UserDeleteResult.Deactivated;
            }

            db.Users.Remove(user);
            await db.SaveChangesAsync(cancellation);
            return UserDeleteResult.Deleted;
        }

        public async Task<UserDto> ReactivateAsync(int id, CancellationToken cancellation)
        {
            EnsureOwner();
            _license.EnsureCanWrite();
            var orgName = _currentUser.OrgName!;
            await using var db = await _factory.CreateDbContextAsync(cancellation);

            var user = await db.Users.SingleOrDefaultAsync(x => x.Id == id && x.OrgName == orgName && x.UserRole != UserRole.Boshliq, cancellation)
                ?? throw new InvalidOperationException("Xodim topilmadi.");

            if (!user.IsActive)
            {
                user.IsActive = true;
                user.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellation);
            }

            return ToDto(user);
        }

        public async Task<UserDto> UpdateAsync(UpdateUserDto dto, CancellationToken cancellation)
        {
            EnsureOwner();
            var isSelf = dto.Id == _currentUser.UserId;

            // Rahbar o'z login/parolini litsenziya muddati o'tgan bo'lsa ham o'zgartira olishi kerak
            if (!isSelf)
                _license.EnsureCanWrite();

            await using var db = await _factory.CreateDbContextAsync(cancellation);
            var user = await FindManageableAsync(db, dto.Id, cancellation)
                ?? throw new InvalidOperationException("Xodim topilmadi.");

            var newUserName = string.IsNullOrWhiteSpace(dto.UserName) ? null : NormalizeUserName(dto.UserName);
            var userNameChanged = newUserName is not null && newUserName != user.UserName;
            var passwordChanged = !string.IsNullOrEmpty(dto.Password);

            if (passwordChanged)
                ValidatePassword(dto.Password!);

            if (user.UserRole == UserRole.Boshliq)
            {
                if (dto.UserRole.HasValue && dto.UserRole != UserRole.Boshliq)
                    throw new InvalidOperationException("Rahbarning rolini o‘zgartirib bo‘lmaydi.");

                // Kompyuter ochiq qolib ketsa ham begona odam rahbar parolini almashtira olmasin
                if ((userNameChanged || passwordChanged)
                    && (string.IsNullOrEmpty(dto.CurrentPassword)
                        || !BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.Password)))
                    throw new UnauthorizedAccessException("Login yoki parolni o‘zgartirish uchun joriy parolingizni to‘g‘ri kiriting.");
            }
            else if (dto.UserRole.HasValue)
            {
                if (dto.UserRole == UserRole.Boshliq || !Enum.IsDefined(dto.UserRole.Value))
                    throw new InvalidOperationException("Xodim uchun tanlangan rol noto‘g‘ri.");
                user.UserRole = dto.UserRole.Value;
            }

            if (userNameChanged)
            {
                await EnsureUserNameFreeAsync(db, newUserName!, exceptId: user.Id, cancellation);
                user.UserName = newUserName!;
            }

            if (!string.IsNullOrWhiteSpace(dto.FullName))
                user.FullName = dto.FullName.Trim();

            if (passwordChanged)
                user.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            user.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellation);

            // Joriy sessiyadagi ma'lumotlar ham yangilansin
            if (isSelf)
                _currentUser.SignIn(ToAuthResult(user));

            return ToDto(user);
        }

        // Rahbar boshqara oladigan foydalanuvchi: o'z do'konidagi xodim yoki o'zi
        private Task<User?> FindManageableAsync(AppDbContext db, int id, CancellationToken cancellation)
        {
            var orgName = _currentUser.OrgName!;
            var currentUserId = _currentUser.UserId;
            return db.Users.SingleOrDefaultAsync(
                x => x.Id == id && x.OrgName == orgName
                  && (x.UserRole != UserRole.Boshliq || x.Id == currentUserId),
                cancellation);
        }

        private static async Task EnsureUserNameFreeAsync(AppDbContext db, string userName, int? exceptId, CancellationToken cancellation)
        {
            var existing = await db.Users
                .AsNoTracking()
                .Where(x => x.UserName == userName && x.Id != exceptId)
                .Select(x => new { x.IsActive })
                .FirstOrDefaultAsync(cancellation);

            if (existing is null)
                return;

            throw new InvalidOperationException(existing.IsActive
                ? "Bu login band."
                : "Bu login nofaol xodimga tegishli. \"Nofaollarni ko‘rsatish\" orqali uni qayta faollashtirishingiz mumkin.");
        }

        private void EnsureOwner()
        {
            if (!_currentUser.IsOwner || string.IsNullOrWhiteSpace(_currentUser.OrgName))
                throw new UnauthorizedAccessException("Bu amal faqat do‘kon egasi uchun ruxsat etilgan.");
        }

        private static void ValidatePassword(string password)
        {
            if (password.Length < MinPasswordLength)
                throw new InvalidOperationException($"Parol kamida {MinPasswordLength} ta belgidan iborat bo‘lishi kerak.");
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
            IsActive = user.IsActive,
            Password = ""
        };
    }
}
