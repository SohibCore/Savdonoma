using Microsoft.EntityFrameworkCore;
using Savdonoma.Core.Entity;
using Savdonoma.Core.Enums;
using Savdonoma.Data.Services.Authservice;
using Savdonoma.Data.Services.Reports;
using Savdonoma.Data.Services.Sales;
using Savdonoma.Data.Services.Sales.SaleItems;
using Savdonoma.Data.Services.SystemSetting;
using Savdonoma.Services;

namespace Savdonoma.Tests;

// Sotuvchi chekni bekor qila olmaydi va hisobotda faqat o'z sotuvlarini ko'radi
public class SaleRoleTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly CurrentUserService _owner;
    private readonly CurrentUserService _manager;
    private readonly CurrentUserService _seller1;
    private readonly CurrentUserService _seller2;
    private readonly int _productId;

    public SaleRoleTests()
    {
        _owner = Add("boss", UserRole.Boshliq);
        _manager = Add("men", UserRole.Manager);
        _seller1 = Add("ali", UserRole.Sotuvchi);
        _seller2 = Add("vali", UserRole.Sotuvchi);

        using var db = _db.CreateDbContext();
        db.SysSettings.Add(new SystemSettings { InventoryEnabled = false, CreatedAt = DateTime.UtcNow });
        var product = new Product
        {
            Name = "Non",
            NameSearch = "Non",
            Price = 4000,
            CostPrice = 3000,
            Unit = Unit.Dona,
            Barcode = "4780000000001",
            MxikCode = "MXIK-TEST",
            IsActive = true
        };
        db.Products.Add(product);
        db.SaveChanges();
        _productId = product.Id;
    }

    public void Dispose() => _db.Dispose();

    private CurrentUserService Add(string userName, UserRole role)
    {
        using var db = _db.CreateDbContext();
        var user = new User
        {
            FullName = userName, UserName = userName, Password = "x", OrgName = "S",
            UserRole = role, IsActive = true, SelfRegister = role == UserRole.Boshliq,
        };
        db.Users.Add(user);
        db.SaveChanges();

        if (role == UserRole.Boshliq)
        {
            var organization = new Organization
            {
                OwnerId = user.Id,
                Name = user.OrgName,
                Address = "",
                Phone = "",
                Website = ""
            };
            db.Organizations.Add(organization);
            db.SaveChanges();
            user.OrgId = organization.Id;
            db.SaveChanges();
        }

        var current = new CurrentUserService();
        current.SignIn(new AuthResultDto { UserId = user.Id, UserName = userName, OrgName = "S", UserRole = role });
        return current;
    }

    private SaleService Sales(CurrentUserService user) => new(_db, new SystemSettingsService(_db), new FakeLicense(), user);
    private ReportService Reports(CurrentUserService user) => new(_db, user);

    private async Task<SaleDto> Sell(CurrentUserService seller, decimal quantity)
        => await Sales(seller).CreateAsync(new CreateSaleDto
        {
            PaymentMethod = PaymentMethod.Naqd,
            UserId = seller.UserId,
            Items = { new CreateSaleItemDto { ProductId = _productId, Quantity = quantity } }
        }, CancellationToken.None);

    [Fact]
    public async Task Seller_cannot_cancel_any_sale_but_owner_and_manager_can()
    {
        var own = await Sell(_seller1, 1);
        var other = await Sell(_seller2, 1);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Sales(_seller1).CancelAsync(own.Id, null, CancellationToken.None));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Sales(_seller1).CancelAsync(other.Id, null, CancellationToken.None));

        await Sales(_manager).CancelAsync(own.Id, "Xato urildi", CancellationToken.None);
        await Sales(_owner).CancelAsync(other.Id, null, CancellationToken.None);

        await using var db = _db.CreateDbContext();
        Assert.All(await db.Sales.ToListAsync(), s => Assert.Equal(SaleStatus.Cancelled, s.SaleStatus));
    }

    [Fact]
    public async Task Seller_report_shows_only_own_sales_while_owner_and_manager_see_all()
    {
        await Sell(_seller1, 1);   //  4 000
        await Sell(_seller1, 2);   //  8 000
        await Sell(_seller2, 5);   // 20 000
        var today = DateTime.Today;

        // Sotuvchi 1: faqat o'zining 2 ta cheki
        var mySummary = await Reports(_seller1).GetSummaryAsync(today, today, CancellationToken.None);
        Assert.Equal(12_000, mySummary.TotalAmount);
        Assert.Equal(2, mySummary.SalesCount);
        Assert.Equal(2, (await Reports(_seller1).GetSalesAsync(today, today, CancellationToken.None)).Count);
        Assert.Equal(3, (await Reports(_seller1).GetTopProductsAsync(today, today, CancellationToken.None)).Single().Quantity);

        // Rahbar va menejer: hammasi
        foreach (var boss in new[] { _owner, _manager })
        {
            var all = await Reports(boss).GetSummaryAsync(today, today, CancellationToken.None);
            Assert.Equal(32_000, all.TotalAmount);
            Assert.Equal(3, all.SalesCount);
            Assert.Equal(8, (await Reports(boss).GetTopProductsAsync(today, today, CancellationToken.None)).Single().Quantity);
        }
    }

    [Fact]
    public async Task Seller_cannot_open_details_of_another_sellers_sale()
    {
        var own = await Sell(_seller1, 1);
        var other = await Sell(_seller2, 1);

        Assert.Single(await Sales(_seller1).GetDetailAsync(own.Id, CancellationToken.None));
        Assert.Empty(await Sales(_seller1).GetDetailAsync(other.Id, CancellationToken.None));
        Assert.Single(await Sales(_manager).GetDetailAsync(other.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Receipt_loads_store_product_and_payment_details_for_preview()
    {
        await using (var db = _db.CreateDbContext())
        {
            var owner = await db.Organizations.SingleAsync(x => x.OwnerId == _owner.UserId);
            owner.Address = "Toshkent shahri";
            owner.INN = "123456789";
            await db.SaveChangesAsync();
        }

        var sale = await Sales(_owner).CreateAsync(new CreateSaleDto
        {
            PaymentMethod = PaymentMethod.Karta,
            UserId = _owner.UserId,
            Items = { new CreateSaleItemDto { ProductId = _productId, Quantity = 1 } }
        }, CancellationToken.None);

        var receipt = await Sales(_owner).GetReceipt(sale.Id, CancellationToken.None);
        var item = Assert.Single(receipt.Items);

        Assert.Equal("Toshkent shahri", receipt.Address);
        Assert.Equal("123456789", receipt.INN);
        Assert.Equal("4780000000001", item.Barcode);
        Assert.Equal("MXIK-TEST", item.Mxik);
        Assert.Equal(Unit.Dona, item.Unit);
        Assert.Equal(0, receipt.Cash);
        Assert.Equal(4000, receipt.Card);
    }
}
