using Savdonoma.Services;
using Savdonoma.Core.Entity;
using Savdonoma.Core.Licensing;
using Microsoft.EntityFrameworkCore;

namespace Savdonoma.Data.Services.Stock
{
    public class SupplierInput
    {
        public string Name { get; set; } = "";
        public string? Phone { get; set; }
        public string? Inn { get; set; }
        public string? Note { get; set; }
    }

    public class SupplierDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Phone { get; set; }
        public string? Inn { get; set; }
        public string? Note { get; set; }
        public bool IsActive { get; set; }
        public long Debt { get; set; }
    }

    public class SupplierPaymentDto
    {
        public int Id { get; set; }
        public long Amount { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public interface ISupplierService
    {
        Task<List<SupplierDto>> GetListAsync(bool includeInactive, CancellationToken cancellation);
        Task<SupplierDto> CreateAsync(SupplierInput input, CancellationToken cancellation);
        Task<SupplierDto> UpdateAsync(int id, SupplierInput input, CancellationToken cancellation);
        Task<SupplierDto> SetActiveAsync(int id, bool isActive, CancellationToken cancellation);

        // Yetkazib beruvchiga to'lov: qarz kamayadi
        Task<SupplierDto> AddPaymentAsync(int id, long amount, string? note, CancellationToken cancellation);
        Task<List<SupplierPaymentDto>> GetPaymentsAsync(int id, CancellationToken cancellation);
    }

    public class SupplierService : ISupplierService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly CurrentUserService _currentUser;
        private readonly ILicenseService _license;

        public SupplierService(IDbContextFactory<AppDbContext> factory, CurrentUserService currentUser, ILicenseService license)
        {
            _factory = factory;
            _currentUser = currentUser;
            _license = license;
        }

        public async Task<List<SupplierDto>> GetListAsync(bool includeInactive, CancellationToken cancellation)
        {
            EnsureCanManage();
            await using var db = await _factory.CreateDbContextAsync(cancellation);

            var suppliers = await db.Suppliers
                .AsNoTracking()
                .Where(x => includeInactive || x.IsActive)
                .ToListAsync(cancellation);

            return suppliers
                .OrderByDescending(x => x.IsActive)
                .ThenBy(x => x.Name)
                .Select(ToDto)
                .ToList();
        }

        public async Task<SupplierDto> CreateAsync(SupplierInput input, CancellationToken cancellation)
        {
            EnsureCanManage();
            _license.EnsureCanWrite();
            var name = ValidateName(input.Name);

            await using var db = await _factory.CreateDbContextAsync(cancellation);
            await EnsureNameFreeAsync(db, name, exceptId: null, cancellation);

            var supplier = new Supplier
            {
                Name = name,
                Phone = Clean(input.Phone),
                Inn = Clean(input.Inn),
                Note = Clean(input.Note),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };

            db.Suppliers.Add(supplier);
            await db.SaveChangesAsync(cancellation);
            return ToDto(supplier);
        }

        public async Task<SupplierDto> UpdateAsync(int id, SupplierInput input, CancellationToken cancellation)
        {
            EnsureCanManage();
            _license.EnsureCanWrite();
            var name = ValidateName(input.Name);

            await using var db = await _factory.CreateDbContextAsync(cancellation);
            var supplier = await FindAsync(db, id, cancellation);
            await EnsureNameFreeAsync(db, name, exceptId: id, cancellation);

            supplier.Name = name;
            supplier.Phone = Clean(input.Phone);
            supplier.Inn = Clean(input.Inn);
            supplier.Note = Clean(input.Note);
            supplier.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(cancellation);
            return ToDto(supplier);
        }

        public async Task<SupplierDto> SetActiveAsync(int id, bool isActive, CancellationToken cancellation)
        {
            EnsureCanManage();
            _license.EnsureCanWrite();

            await using var db = await _factory.CreateDbContextAsync(cancellation);
            var supplier = await FindAsync(db, id, cancellation);

            if (supplier.IsActive != isActive)
            {
                supplier.IsActive = isActive;
                supplier.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellation);
            }

            return ToDto(supplier);
        }

        public async Task<SupplierDto> AddPaymentAsync(int id, long amount, string? note, CancellationToken cancellation)
        {
            EnsureCanManage();
            _license.EnsureCanWrite();
            if (amount <= 0)
                throw new InvalidOperationException("To'lov summasi 0 dan katta bo'lishi kerak.");

            await using var db = await _factory.CreateDbContextAsync(cancellation);
            var supplier = await FindAsync(db, id, cancellation);
            var now = DateTime.UtcNow;

            db.SupplierPayments.Add(new SupplierPayment
            {
                SupplierId = supplier.Id,
                Amount = amount,
                Note = Clean(note),
                UserId = _currentUser.UserId,
                CreatedAt = now,
            });

            // Ortiqcha to'lov manfiy qarz (oldindan to'lov) bo'lib qoladi
            supplier.Debt -= amount;
            supplier.UpdatedAt = now;

            await db.SaveChangesAsync(cancellation);
            return ToDto(supplier);
        }

        public async Task<List<SupplierPaymentDto>> GetPaymentsAsync(int id, CancellationToken cancellation)
        {
            EnsureCanManage();
            await using var db = await _factory.CreateDbContextAsync(cancellation);

            var payments = await db.SupplierPayments
                .AsNoTracking()
                .Where(x => x.SupplierId == id)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new SupplierPaymentDto
                {
                    Id = x.Id,
                    Amount = x.Amount,
                    Note = x.Note,
                    CreatedAt = x.CreatedAt,
                })
                .ToListAsync(cancellation);

            foreach (var payment in payments)
                payment.CreatedAt = payment.CreatedAt.ToLocalTime();

            return payments;
        }

        private static async Task<Supplier> FindAsync(AppDbContext db, int id, CancellationToken cancellation)
            => await db.Suppliers.SingleOrDefaultAsync(x => x.Id == id, cancellation)
               ?? throw new InvalidOperationException("Yetkazib beruvchi topilmadi.");

        private static async Task EnsureNameFreeAsync(AppDbContext db, string name, int? exceptId, CancellationToken cancellation)
        {
            var lowered = name.ToLower();
            var exists = await db.Suppliers.AnyAsync(x => x.Id != exceptId && x.Name.ToLower() == lowered, cancellation);
            if (exists)
                throw new InvalidOperationException("Bunday nomli yetkazib beruvchi allaqachon bor.");
        }

        private static string ValidateName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("Yetkazib beruvchi nomini kiriting.");
            return name.Trim();
        }

        private static string? Clean(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private void EnsureCanManage()
        {
            if (!_currentUser.CanManageStock)
                throw new UnauthorizedAccessException("Bu amal faqat rahbar va menejer uchun ruxsat etilgan.");
        }

        private static SupplierDto ToDto(Supplier x) => new()
        {
            Id = x.Id,
            Name = x.Name,
            Phone = x.Phone,
            Inn = x.Inn,
            Note = x.Note,
            IsActive = x.IsActive,
            Debt = x.Debt,
        };
    }
}
