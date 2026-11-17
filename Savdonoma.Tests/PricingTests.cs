using Microsoft.EntityFrameworkCore;
using Savdonoma.Core.Entity;
using Savdonoma.Core.Enums;
using Savdonoma.Core.Logic;
using Savdonoma.Data.Services.Authservice;
using Savdonoma.Data.Services.Pricing;
using Savdonoma.Data.Services.Products;
using Savdonoma.Data.Services.Stock;
using Savdonoma.Data.Services.SystemSetting;
using Savdonoma.Services;

namespace Savdonoma.Tests;

public class PriceCalculatorTests
{
    [Theory]
    [InlineData(2000, 25, 100, 2500)]
    [InlineData(10_000, 25, 100, 12_500)]
    [InlineData(9_870, 25, 100, 12_400)]   // 12 337.5 -> yuqoriga 12 400
    [InlineData(9_870, 25, 500, 12_500)]
    [InlineData(9_870, 25, 1000, 13_000)]
    [InlineData(1_000, 0, 100, 1_000)]     // 0% ustama: tannarxga teng
    [InlineData(1_234, 10, 1, 1_358)]      // 1357.4 -> 1358
    public void Suggested_price_is_cost_plus_markup_rounded_up(long cost, double markup, int step, long expected)
        => Assert.Equal(expected, PriceCalculator.SuggestSalePrice(cost, (decimal)markup, step));

    [Fact]
    public void No_markup_or_free_goods_give_no_suggestion()
    {
        Assert.Null(PriceCalculator.SuggestSalePrice(2000, null, 100));
        Assert.Null(PriceCalculator.SuggestSalePrice(0, 25, 100));
    }

    [Fact]
    public void Markup_is_resolved_product_then_category_then_default()
    {
        Assert.Equal(8, PriceCalculator.ResolveMarkup(8, 30, 20));
        Assert.Equal(30, PriceCalculator.ResolveMarkup(null, 30, 20));
        Assert.Equal(20, PriceCalculator.ResolveMarkup(null, null, 20));
        Assert.Null(PriceCalculator.ResolveMarkup(null, null, null));
        Assert.Equal(0, PriceCalculator.ResolveMarkup(0, 30, 20)); // 0% ham aniq tanlov
    }

    [Fact]
    public void Markup_and_margin_differ()
    {
        Assert.Equal(25m, PriceCalculator.MarkupPercent(12_500, 10_000));
        Assert.Equal(20m, PriceCalculator.MarginPercent(12_500, 10_000));
        Assert.Null(PriceCalculator.MarkupPercent(12_500, 0)); // tannarx noma'lum
    }

    [Fact]
    public void Invalid_rules_are_rejected()
    {
        Assert.Throws<ArgumentException>(() => PriceCalculator.ValidateMarkup(-1));
        Assert.Throws<ArgumentException>(() => PriceCalculator.ValidateMarkup(1001));
        Assert.Throws<ArgumentException>(() => PriceCalculator.ValidateRoundingStep(300));
        PriceCalculator.ValidateRoundingStep(500);
        PriceCalculator.ValidateMarkup(null);
    }
}

public class PricingServiceTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly CurrentUserService _owner = new();
    private readonly CurrentUserService _manager = new();

    public PricingServiceTests()
    {
        using var db = _db.CreateDbContext();
        var owner = new User { FullName = "b", UserName = "boss", Password = "x", OrgName = "S", UserRole = UserRole.Boshliq, IsActive = true, SelfRegister = true };
        var manager = new User { FullName = "m", UserName = "men", Password = "x", OrgName = "S", UserRole = UserRole.Manager, IsActive = true, SelfRegister = false };
        db.Users.AddRange(owner, manager);
        db.SysSettings.Add(new SystemSettings { InventoryEnabled = true, CreatedAt = DateTime.UtcNow });
        db.Categories.Add(new Category { Id = 1, Name = "Shirinliklar", IsActive = true });
        db.Categories.Add(new Category { Id = 2, Name = "Ichimliklar", IsActive = true });
        db.SaveChanges();

        _owner.SignIn(new AuthResultDto { UserId = owner.Id, UserName = "boss", OrgName = "S", UserRole = UserRole.Boshliq });
        _manager.SignIn(new AuthResultDto { UserId = manager.Id, UserName = "men", OrgName = "S", UserRole = UserRole.Manager });
    }

    public void Dispose() => _db.Dispose();

    private PricingService Pricing(CurrentUserService user) => new(_db, user, new FakeLicense());
    private StockService Stock(CurrentUserService user) => new(_db, user, new SystemSettingsService(_db), new FakeLicense());
    private ProductService Products() => new(_db, _owner, new SystemSettingsService(_db), new FakeLicense());

    private async Task<ProductDto> NewProduct(string name, int categoryId, long price, long cost, decimal quantity, decimal? markup = null)
        => await Products().CreateAsync(new CreateProductDto
        {
            Name = name, Unit = Unit.Dona, Price = price, CostPrice = cost, Quantity = quantity,
            CategoryId = categoryId, MarkupPercent = markup,
        }, CancellationToken.None);

    private async Task<StockDocumentDto> Receive(int productId, decimal quantity, long cost, long? newPrice = null)
    {
        var draft = await Stock(_manager).CreateDraftAsync(StockDocumentType.Receipt, new StockDocumentInput
        {
            Lines = { new StockLineInput { ProductId = productId, Quantity = quantity, CostPrice = cost, NewSalePrice = newPrice } }
        }, CancellationToken.None);
        return await Stock(_manager).PostAsync(draft.Id, CancellationToken.None);
    }

    private Product Load(int id)
    {
        using var db = _db.CreateDbContext();
        return db.Products.AsNoTracking().Single(x => x.Id == id);
    }

    [Fact]
    public async Task New_batch_reprices_all_stock_from_new_purchase_price()
    {
        // Misol: 10 dona 1000 so'mdan, narx 1250 (25%). Keyin 10 dona 2000 so'mdan keldi.
        await Pricing(_owner).SaveDefaultsAsync(25, 100, CancellationToken.None);
        var tea = await NewProduct("Choy", categoryId: 2, price: 1250, cost: 1000, quantity: 10);

        var draft = await Stock(_manager).CreateDraftAsync(StockDocumentType.Receipt, new StockDocumentInput
        {
            Lines = { new StockLineInput { ProductId = tea.Id, Quantity = 10, CostPrice = 2000 } }
        }, CancellationToken.None);

        // Qoralamada taklif ko'rinadi, narx hali o'zgarmagan
        var line = draft.Lines.Single();
        Assert.Equal(2500, line.NewSalePrice);
        Assert.Equal(1250, line.CurrentSalePrice);
        Assert.Equal(25m, line.NewMarkupPercent);
        Assert.False(line.IsBelowCost);
        Assert.Equal(1250, Load(tea.Id).Price);

        await Stock(_manager).PostAsync(draft.Id, CancellationToken.None);

        var stored = Load(tea.Id);
        Assert.Equal(2500, stored.Price);      // 20 donaning hammasi yangi narxda
        Assert.Equal(1500, stored.CostPrice);  // foyda uchun o'rtacha tannarx
        Assert.Equal(20, stored.Quantity);
    }

    [Fact]
    public async Task Product_markup_overrides_category_which_overrides_default()
    {
        await Pricing(_owner).SaveDefaultsAsync(20, 100, CancellationToken.None);
        await Pricing(_owner).SetCategoryMarkupAsync(1, 30, CancellationToken.None);

        var candy = await NewProduct("Konfet", categoryId: 1, price: 1, cost: 0, quantity: 0);
        var cigarette = await NewProduct("Sigaret", categoryId: 1, price: 1, cost: 0, quantity: 0, markup: 8);
        var water = await NewProduct("Suv", categoryId: 2, price: 1, cost: 0, quantity: 0);

        await Receive(candy.Id, 1, 10_000);
        await Receive(cigarette.Id, 1, 10_000);
        await Receive(water.Id, 1, 10_000);

        Assert.Equal(13_000, Load(candy.Id).Price);     // kategoriya 30%
        Assert.Equal(10_800, Load(cigarette.Id).Price); // mahsulot 8%
        Assert.Equal(12_000, Load(water.Id).Price);     // umumiy 20%

        var rules = await Pricing(_manager).GetRulesAsync(CancellationToken.None);
        Assert.Equal(20, rules.DefaultMarkupPercent);
        Assert.Equal(30, rules.CategoryMarkups[1]);
        Assert.Equal(13_000, rules.SuggestSalePrice(10_000, null, 1));
    }

    [Fact]
    public async Task Price_typed_by_seller_wins_over_suggestion_and_below_cost_is_flagged()
    {
        await Pricing(_owner).SaveDefaultsAsync(25, 100, CancellationToken.None);
        var tea = await NewProduct("Choy", categoryId: 2, price: 1250, cost: 1000, quantity: 10);

        var draft = await Stock(_manager).CreateDraftAsync(StockDocumentType.Receipt, new StockDocumentInput
        {
            Lines = { new StockLineInput { ProductId = tea.Id, Quantity = 10, CostPrice = 2000, NewSalePrice = 1900 } }
        }, CancellationToken.None);

        Assert.Equal(1900, draft.Lines.Single().NewSalePrice);
        Assert.True(draft.Lines.Single().IsBelowCost);

        await Stock(_manager).PostAsync(draft.Id, CancellationToken.None);
        Assert.Equal(1900, Load(tea.Id).Price);
    }

    [Fact]
    public async Task Without_any_markup_price_does_not_change()
    {
        var tea = await NewProduct("Choy", categoryId: 2, price: 1250, cost: 1000, quantity: 10);

        var posted = await Receive(tea.Id, 10, 2000);

        Assert.Null(posted.Lines.Single().NewSalePrice);
        Assert.Equal(1250, Load(tea.Id).Price);
    }

    [Fact]
    public async Task Rounding_step_is_applied()
    {
        await Pricing(_owner).SaveDefaultsAsync(25, 500, CancellationToken.None);
        var tea = await NewProduct("Choy", categoryId: 2, price: 1, cost: 0, quantity: 0);

        await Receive(tea.Id, 1, 9_870); // 12 337.5 -> 12 500

        Assert.Equal(12_500, Load(tea.Id).Price);
    }

    [Fact]
    public async Task Only_owner_changes_pricing_rules()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            Pricing(_manager).SaveDefaultsAsync(10, 100, CancellationToken.None));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            Pricing(_manager).SetCategoryMarkupAsync(1, 10, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Pricing(_owner).SaveDefaultsAsync(10, 300, CancellationToken.None));

        // Kategoriya ustamasini olib tashlash (null) umumiyga qaytaradi
        await Pricing(_owner).SetCategoryMarkupAsync(1, 30, CancellationToken.None);
        await Pricing(_owner).SetCategoryMarkupAsync(1, null, CancellationToken.None);
        Assert.Empty((await Pricing(_owner).GetRulesAsync(CancellationToken.None)).CategoryMarkups);
    }
}
