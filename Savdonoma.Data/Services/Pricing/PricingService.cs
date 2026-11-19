using Savdonoma.Services;
using Savdonoma.Core.Logic;
using Savdonoma.Core.Entity;
using Savdonoma.Core.Licensing;
using Microsoft.EntityFrameworkCore;

namespace Savdonoma.Data.Services.Pricing
{
    // Narx qoidalari: interfeys tavsiya narxini o'zi (PriceCalculator bilan) hisoblashi uchun
    public class PricingRules
    {
        public decimal? DefaultMarkupPercent { get; set; }
        public int RoundingStep { get; set; } = PriceCalculator.DefaultRoundingStep;

        // Kategoriya Id -> ustama (%), faqat ustamasi borlar
        public Dictionary<int, decimal> CategoryMarkups { get; set; } = new();

        public decimal? ResolveMarkup(decimal? productMarkup, int? categoryId)
            => PriceCalculator.ResolveMarkup(
                productMarkup,
                categoryId is not null && CategoryMarkups.TryGetValue(categoryId.Value, out var m) ? m : null,
                DefaultMarkupPercent);

        public long? SuggestSalePrice(long purchasePrice, decimal? productMarkup, int? categoryId)
            => PriceCalculator.SuggestSalePrice(purchasePrice, ResolveMarkup(productMarkup, categoryId), RoundingStep);
    }

    public interface IPricingService
    {
        Task<PricingRules> GetRulesAsync(CancellationToken cancellation);

        // Faqat rahbar
        Task SaveDefaultsAsync(decimal? defaultMarkupPercent, int roundingStep, CancellationToken cancellation);
        Task SetCategoryMarkupAsync(int categoryId, decimal? markupPercent, CancellationToken cancellation);
    }

    public class PricingService : IPricingService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly CurrentUserService _currentUser;
        private readonly ILicenseService _license;

        public PricingService(IDbContextFactory<AppDbContext> factory, CurrentUserService currentUser, ILicenseService license)
        {
            _factory = factory;
            _currentUser = currentUser;
            _license = license;
        }

        public async Task<PricingRules> GetRulesAsync(CancellationToken cancellation)
        {
            await using var db = await _factory.CreateDbContextAsync(cancellation);
            return await LoadRulesAsync(db, cancellation);
        }

        // StockService ham shu qoidalardan foydalanadi
        internal static async Task<PricingRules> LoadRulesAsync(AppDbContext db, CancellationToken cancellation)
        {
            var settings = await db.SysSettings.AsNoTracking().FirstOrDefaultAsync(cancellation);
            var categories = await db.Categories
                .AsNoTracking()
                .Where(x => x.MarkupPercent != null)
                .Select(x => new { x.Id, x.MarkupPercent })
                .ToListAsync(cancellation);

            return new PricingRules
            {
                DefaultMarkupPercent = settings?.DefaultMarkupPercent,
                RoundingStep = settings?.PriceRoundingStep ?? PriceCalculator.DefaultRoundingStep,
                CategoryMarkups = categories.ToDictionary(x => x.Id, x => x.MarkupPercent!.Value),
            };
        }

        public async Task SaveDefaultsAsync(decimal? defaultMarkupPercent, int roundingStep, CancellationToken cancellation)
        {
            EnsureOwner();
            _license.EnsureCanWrite();
            PriceCalculator.ValidateMarkup(defaultMarkupPercent);
            PriceCalculator.ValidateRoundingStep(roundingStep);

            await using var db = await _factory.CreateDbContextAsync(cancellation);
            var settings = await db.SysSettings.FirstOrDefaultAsync(cancellation);
            var now = DateTime.UtcNow;

            if (settings is null)
            {
                settings = new Savdonoma.Core.Entity.SystemSettings { CreatedAt = now };
                db.SysSettings.Add(settings);
            }

            settings.DefaultMarkupPercent = defaultMarkupPercent;
            settings.PriceRoundingStep = roundingStep;
            settings.UpdatedAt = now;
            await db.SaveChangesAsync(cancellation);
        }

        public async Task SetCategoryMarkupAsync(int categoryId, decimal? markupPercent, CancellationToken cancellation)
        {
            EnsureOwner();
            _license.EnsureCanWrite();
            PriceCalculator.ValidateMarkup(markupPercent);

            await using var db = await _factory.CreateDbContextAsync(cancellation);
            var category = await db.Categories.SingleOrDefaultAsync(x => x.Id == categoryId, cancellation)
                ?? throw new InvalidOperationException("Kategoriya topilmadi.");

            category.MarkupPercent = markupPercent;
            category.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellation);
        }

        private void EnsureOwner()
        {
            if (!_currentUser.IsOwner)
                throw new UnauthorizedAccessException("Narx qoidalarini faqat rahbar o'zgartira oladi.");
        }
    }
}
