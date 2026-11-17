using Savdonoma.Core.Enums;
using Savdonoma.Core.Licensing;
using Microsoft.EntityFrameworkCore;

namespace Savdonoma.Data.Licensing
{
    public interface IPasswordResetService
    {
        string MachineId { get; }

        // Har bir oyna uchun yangi so'rov kodi
        string RequestCode { get; }

        Task<string?> GetOwnerUserNameAsync(CancellationToken cancellation);

        // Kod to'g'ri bo'lsa, do'kon egasining parolini yangilaydi va uning loginini qaytaradi
        Task<string> ResetOwnerPasswordAsync(string resetCode, string newPassword, CancellationToken cancellation);
    }

    public class PasswordResetService : IPasswordResetService
    {
        public const int MinPasswordLength = 4;

        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly string _publicKey;
        private readonly Func<DateTime> _utcNow;

        public string MachineId { get; }
        public string RequestCode { get; } = PasswordResetCode.NewRequestCode();

        public PasswordResetService(IDbContextFactory<AppDbContext> factory, ILicenseService license)
            : this(factory, license.MachineId, LicenseKeys.PublicKey, () => DateTime.UtcNow)
        {
        }

        // Testlar uchun
        public PasswordResetService(IDbContextFactory<AppDbContext> factory, string machineId, string publicKey, Func<DateTime> utcNow)
        {
            _factory = factory;
            MachineId = machineId;
            _publicKey = publicKey;
            _utcNow = utcNow;
        }

        public async Task<string?> GetOwnerUserNameAsync(CancellationToken cancellation)
        {
            await using var db = await _factory.CreateDbContextAsync(cancellation);
            return await db.Users
                .AsNoTracking()
                .Where(x => x.UserRole == UserRole.Boshliq)
                .OrderBy(x => x.Id)
                .Select(x => x.UserName)
                .FirstOrDefaultAsync(cancellation);
        }

        public async Task<string> ResetOwnerPasswordAsync(string resetCode, string newPassword, CancellationToken cancellation)
        {
            if (string.IsNullOrWhiteSpace(resetCode))
                throw new InvalidOperationException("Tiklash kodini kiriting.");

            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < MinPasswordLength)
                throw new InvalidOperationException($"Yangi parol kamida {MinPasswordLength} ta belgidan iborat bo'lishi kerak.");

            var today = LicenseEvaluator.TodayInTashkent(_utcNow());
            if (!PasswordResetCode.Verify(resetCode, MachineId, RequestCode, today, _publicKey))
                throw new UnauthorizedAccessException(
                    "Tiklash kodi noto'g'ri yoki muddati o'tgan. Kod aynan shu oynadagi so'rov kodi uchun berilgan bo'lishi kerak.");

            await using var db = await _factory.CreateDbContextAsync(cancellation);
            var owner = await db.Users
                .Where(x => x.UserRole == UserRole.Boshliq)
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync(cancellation)
                ?? throw new InvalidOperationException("Do'kon egasi topilmadi.");

            owner.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            owner.IsActive = true;
            owner.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellation);

            return owner.UserName;
        }
    }
}
