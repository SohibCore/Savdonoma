using Savdonoma.Core.Enums;
using Savdonoma.Core.Entity;
using Microsoft.EntityFrameworkCore;
using Savdonoma.Data.Services.Authservice;

namespace Savdonoma.Data.Services.UserService
{
    public class UserService : IUserService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly IAuthservice _service;
        public UserService(IDbContextFactory<AppDbContext> factory, IAuthservice authservice)
        {
            _factory = factory;
            _service = authservice;
        }
        public Task<List<UserDto>> GetListAsync(CancellationToken cancellation)
        {
            using var db = _factory.CreateDbContext();

            var users = db.Users
                .Select(x => new UserDto
                {
                    Id = x.Id,
                    FullName = x.FullName,
                    UserName = x.UserName,
                    UserRole = x.UserRole,
                }).ToListAsync(cancellation);

            return users;
        }

        public async Task<UserDto> GetAsync(int Id, CancellationToken cancellation)
        {
            using var db = _factory.CreateDbContext();

            var user = await db.Users
                .Where(x => x.Id == Id)
                .Select(x => new UserDto
                {
                    Id = x.Id,
                    FullName = x.FullName,
                    UserName = x.UserName,
                    UserRole = x.UserRole,
                }).FirstOrDefaultAsync(cancellation);

            if (user is null)
                throw new Exception("Foydalanuvchi topilmadi.");

            return user;
        }

        public async Task<UserDto> CreateAsync(CreateUserDto dto, CancellationToken cancellation)
        {
            using var db = _factory.CreateDbContext();

            var userName = dto.UserName.Trim().ToLowerInvariant();

            var user = new User
            {
                FullName = dto.FullName,
                UserName = userName,
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                OrgName = dto.OrgName,
                UserRole = dto.UserRole,
                IsActive = true,
            };

            if (user.UserRole == UserRole.Boshliq)
                user.SelfRegister = true;

            await db.Users.AddAsync(user, cancellation);
            await db.SaveChangesAsync(cancellation);

            return new UserDto
            {
                FullName = user.FullName,
                UserName = user.UserName,
                OrgName = user.OrgName,
                UserRole = user.UserRole
            };
        }

        public async Task DeleteAsync(int Id, CancellationToken cancellation)
        {
            using var db = _factory.CreateDbContext();

            var user = await db.Users.SingleOrDefaultAsync(x => x.Id == Id) ?? throw new Exception("Foydalanuvchi topilmadi");

            db.Users.Remove(user);
            await db.SaveChangesAsync(cancellation);
        }

        public async Task<UserDto> UpdateAsync(UpdateUserDto dto, CancellationToken cancellation)
        {
            using var db = _factory.CreateDbContext();

            var user = await db.Users.SingleOrDefaultAsync(x => x.Id == dto.Id) ?? throw new Exception("Foydalanuvchi topilmadi.");

            if (!string.IsNullOrWhiteSpace(dto.UserName))
            {
                var userName = dto.UserName.Trim().ToLowerInvariant();
                user.UserName = userName;
            }

            if (!string.IsNullOrWhiteSpace(dto.FullName))
                user.FullName = dto.FullName;

            if (!string.IsNullOrWhiteSpace(dto.OrgName))
                user.OrgName = dto.OrgName;

            if (!string.IsNullOrWhiteSpace(dto.Password))
                user.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            await db.SaveChangesAsync(cancellation);

            return new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                UserName = user.UserName,
                UserRole = user.UserRole,
            };
        }
    }
}
