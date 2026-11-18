using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Savdonoma.Core.Entity;
using Savdonoma.Core.Enums;
using Savdonoma.Core.Logic;
using Savdonoma.Data.Services.Authservice;
using Savdonoma.Data.Services.Products;
using Savdonoma.Data.Services.Sales;
using Savdonoma.Data.Services.Sales.SaleItems;
using Savdonoma.Data.Services.Stock;
using Savdonoma.Data.Services.SystemSetting;
using Savdonoma.Services;

namespace Savdonoma.Tests;

public class StockTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly CurrentUserService _owner;
    private readonly CurrentUserService _manager;
    private readonly CurrentUserService _seller;

    public StockTests()
    {
        _owner = SignedIn(AddUser("boss", UserRole.Boshliq), UserRole.Boshliq);
        _manager = SignedIn(AddUser("menejer", UserRole.Manager), UserRole.Manager);
        _seller = SignedIn(AddUser("kassir", UserRole.Sotuvchi), UserRole.Sotuvchi);
        SetInventoryMode(true);
    }

    public void Dispose() => _db.Dispose();

    // ---------- Yordamchilar ----------

    private int AddUser(string userName, UserRole role)
    {
        using var db = _db.CreateDbContext();
        var user = new User
        {
            FullName = userName, UserName = userName, Password = "x", OrgName = "TestStore",
            UserRole = role, IsActive = true, SelfRegister = false, CreatedAt = DateTime.UtcNow,
        };
        db.Users.Add(user);
        db.SaveChanges();
        return user.Id;
    }

    private static CurrentUserService SignedIn(int id, UserRole role)
    {
        var current = new CurrentUserService();
        current.SignIn(new AuthResultDto { UserId = id, UserName = role.ToString(), OrgName = "TestStore", UserRole = role });
        return current;
    }

    private void SetInventoryMode(bool enabled)
    {
        using var db = _db.CreateDbContext();
        var settings = db.SysSettings.FirstOrDefault();
        if (settings is null)
            db.SysSettings.Add(new SystemSettings { InventoryEnabled = enabled, CreatedAt = DateTime.UtcNow });
        else
            settings.InventoryEnabled = enabled;
        db.SaveChanges();
    }

    private StockService Stock(CurrentUserService user) => new(_db, user, new SystemSettingsService(_db), new FakeLicense());
    private ProductService Products(CurrentUserService user) => new(_db, user, new SystemSettingsService(_db), new FakeLicense());
    private SaleService Sales(CurrentUserService user) => new(_db, new SystemSettingsService(_db), new FakeLicense(), user);
    private SupplierService Suppliers(CurrentUserService user) => new(_db, user, new FakeLicense());

    private async Task<ProductDto> NewProduct(string name, decimal quantity = 0, long price = 5000, long? cost = null)
        => await Products(_owner).CreateAsync(new CreateProductDto
        {
            Name = name, Price = price, Unit = Unit.Dona, Quantity = quantity, CostPrice = cost,
        }, CancellationToken.None);

    private Product Load(int productId)
    {
        using var db = _db.CreateDbContext();
        return db.Products.AsNoTracking().Single(x => x.Id == productId);
    }

    private void AssertLedgerConsistent()
    {
        using var db = _db.CreateDbContext();
        var sums = db.StockMovements.AsNoTracking().ToList()
            .GroupBy(x => x.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

        foreach (var product in db.Products.AsNoTracking().ToList())
            Assert.Equal(product.Quantity, sums.GetValueOrDefault(product.Id));
    }

    private static StockDocumentInput Lines(params StockLineInput[] lines) => new() { Lines = lines.ToList() };

    // ---------- Kirim ----------

    [Fact]
    public async Task Receipt_increases_stock_updates_average_cost_price_and_supplier_debt()
    {
        var product = await NewProduct("Choy", quantity: 10, cost: 1000);
        var supplier = await Suppliers(_manager).CreateAsync(new SupplierInput { Name = "Ulgurji savdo" }, CancellationToken.None);

        var stock = Stock(_manager);
        var draft = await stock.CreateDraftAsync(StockDocumentType.Receipt, new StockDocumentInput
        {
            SupplierId = supplier.Id,
            Lines = { new StockLineInput { ProductId = product.Id, Quantity = 10, CostPrice = 2000, NewSalePrice = 6000 } }
        }, CancellationToken.None);

        Assert.Equal(StockDocumentStatus.Draft, draft.Status);
        Assert.Equal(10, Load(product.Id).Quantity); // qoralama qoldiqqa ta'sir qilmaydi
        Assert.Equal(20_000, draft.TotalCost);

        var posted = await stock.PostAsync(draft.Id, CancellationToken.None);

        Assert.Equal(StockDocumentStatus.Posted, posted.Status);
        var stored = Load(product.Id);
        Assert.Equal(20, stored.Quantity);
        Assert.Equal(1500, stored.CostPrice);   // (10*1000 + 10*2000) / 20
        Assert.Equal(6000, stored.Price);       // yangi sotuv narxi
        Assert.Equal(20_000, (await Suppliers(_owner).GetListAsync(false, CancellationToken.None)).Single().Debt);

        var history = await stock.GetProductHistoryAsync(product.Id, CancellationToken.None);
        Assert.Equal(StockMovementType.Receipt, history[0].Type);
        Assert.Equal(10, history[0].Quantity);
        Assert.Equal(20, history[0].BalanceAfter);
        Assert.Equal(2000, history[0].CostPrice);
        Assert.Equal(posted.Number, history[0].DocumentNumber);
        Assert.Equal(StockMovementType.OpeningBalance, history[1].Type);

        AssertLedgerConsistent();
    }

    [Fact]
    public async Task Cancelling_receipt_restores_stock_cost_and_debt_and_is_owner_only()
    {
        var product = await NewProduct("Shakar", quantity: 10, cost: 1000);
        var supplier = await Suppliers(_owner).CreateAsync(new SupplierInput { Name = "Shakar zavodi" }, CancellationToken.None);
        var draft = await Stock(_manager).CreateDraftAsync(StockDocumentType.Receipt, new StockDocumentInput
        {
            SupplierId = supplier.Id,
            Lines = { new StockLineInput { ProductId = product.Id, Quantity = 10, CostPrice = 2000 } }
        }, CancellationToken.None);
        await Stock(_manager).PostAsync(draft.Id, CancellationToken.None);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            Stock(_manager).CancelAsync(draft.Id, "Xato", CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Stock(_owner).CancelAsync(draft.Id, "  ", CancellationToken.None)); // sabab majburiy

        var cancelled = await Stock(_owner).CancelAsync(draft.Id, "Noto'g'ri narx", CancellationToken.None);

        Assert.Equal(StockDocumentStatus.Cancelled, cancelled.Status);
        Assert.Equal("Noto'g'ri narx", cancelled.CancelReason);
        var stored = Load(product.Id);
        Assert.Equal(10, stored.Quantity);
        Assert.Equal(1000, stored.CostPrice);
        Assert.Equal(0, (await Suppliers(_owner).GetListAsync(false, CancellationToken.None)).Single().Debt);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Stock(_owner).CancelAsync(draft.Id, "Yana", CancellationToken.None));

        AssertLedgerConsistent();
    }

    [Fact]
    public async Task Receipt_cannot_be_cancelled_when_goods_already_sold()
    {
        var product = await NewProduct("Un");
        var draft = await Stock(_owner).CreateDraftAsync(StockDocumentType.Receipt,
            Lines(new StockLineInput { ProductId = product.Id, Quantity = 5, CostPrice = 1000 }), CancellationToken.None);
        await Stock(_owner).PostAsync(draft.Id, CancellationToken.None);

        await Sales(_seller).CreateAsync(new CreateSaleDto
        {
            PaymentMethod = PaymentMethod.Naqd, UserId = _seller.UserId,
            Items = { new CreateSaleItemDto { ProductId = product.Id, Quantity = 3 } }
        }, CancellationToken.None);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Stock(_owner).CancelAsync(draft.Id, "Xato", CancellationToken.None));
        Assert.Contains("sotilgan", ex.Message);
        Assert.Equal(2, Load(product.Id).Quantity);
    }

    // ---------- Hisobdan chiqarish ----------

    [Fact]
    public async Task Write_off_checks_stock_only_in_inventory_mode()
    {
        var product = await NewProduct("Sut", quantity: 2, cost: 8000);
        var stock = Stock(_manager);

        var tooMuch = await stock.CreateDraftAsync(StockDocumentType.WriteOff,
            Lines(new StockLineInput { ProductId = product.Id, Quantity = 5 }), CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(() => stock.PostAsync(tooMuch.Id, CancellationToken.None));
        Assert.Equal(2, Load(product.Id).Quantity);

        // Qoldiqsiz rejimda tekshirilmaydi
        SetInventoryMode(false);
        var posted = await stock.PostAsync(tooMuch.Id, CancellationToken.None);
        Assert.Equal(-3, Load(product.Id).Quantity);
        Assert.Equal(40_000, posted.TotalCost); // 5 * 8000 tannarx bo'yicha

        AssertLedgerConsistent();
    }

    // ---------- Inventarizatsiya ----------

    [Fact]
    public async Task Inventory_records_difference_and_can_be_cancelled()
    {
        var a = await NewProduct("Guruch", quantity: 10, cost: 12000);
        var b = await NewProduct("Makaron", quantity: 4, cost: 7000);
        var stock = Stock(_manager);

        var draft = await stock.CreateDraftAsync(StockDocumentType.Inventory, Lines(
            new StockLineInput { ProductId = a.Id, Quantity = 7 },    // kamomad 3
            new StockLineInput { ProductId = b.Id, Quantity = 4 }),   // farq yo'q
            CancellationToken.None);

        Assert.Equal(10, draft.Lines[0].ExpectedQuantity); // qoralamada joriy qoldiq ko'rinadi
        Assert.Equal(-3, draft.Lines[0].Difference);

        var posted = await stock.PostAsync(draft.Id, CancellationToken.None);
        Assert.Equal(7, Load(a.Id).Quantity);
        Assert.Equal(4, Load(b.Id).Quantity);
        Assert.Equal(-36_000, posted.TotalCost);
        Assert.Equal(10, posted.Lines[0].ExpectedQuantity);

        var history = await stock.GetProductHistoryAsync(b.Id, CancellationToken.None);
        Assert.DoesNotContain(history, x => x.Type == StockMovementType.InventoryAdjustment); // 0 farq yozilmaydi

        await Stock(_owner).CancelAsync(draft.Id, "Qayta sanaymiz", CancellationToken.None);
        Assert.Equal(10, Load(a.Id).Quantity);

        AssertLedgerConsistent();
    }

    // ---------- Sotuv ----------

    [Fact]
    public async Task Sale_and_its_cancel_are_written_to_ledger_with_cost_price()
    {
        var product = await NewProduct("Pishloq", quantity: 10, price: 50_000, cost: 35_000);

        var sale = await Sales(_seller).CreateAsync(new CreateSaleDto
        {
            PaymentMethod = PaymentMethod.Karta, UserId = _seller.UserId,
            Items = { new CreateSaleItemDto { ProductId = product.Id, Quantity = 1.5m } }
        }, CancellationToken.None);

        Assert.Equal(8.5m, Load(product.Id).Quantity);
        await using (var db = _db.CreateDbContext())
        {
            var item = await db.SaleItems.SingleAsync(x => x.SaleId == sale.Id);
            Assert.Equal(35_000, item.CostPrice);
        }

        await Sales(_owner).CancelAsync(sale.Id, "Mijoz qaytdi", CancellationToken.None);
        Assert.Equal(10, Load(product.Id).Quantity);

        var history = await Stock(_owner).GetProductHistoryAsync(product.Id, CancellationToken.None);
        Assert.Equal(new[] { StockMovementType.SaleCancel, StockMovementType.Sale, StockMovementType.OpeningBalance },
            history.Select(x => x.Type));
        Assert.Equal(sale.Number, history[1].SaleNumber);

        AssertLedgerConsistent();
    }

    [Fact]
    public async Task Without_inventory_mode_sale_is_not_blocked_but_still_recorded()
    {
        SetInventoryMode(false);
        var product = await NewProduct("Non");

        await Sales(_seller).CreateAsync(new CreateSaleDto
        {
            PaymentMethod = PaymentMethod.Naqd, UserId = _seller.UserId,
            Items = { new CreateSaleItemDto { ProductId = product.Id, Quantity = 3 } }
        }, CancellationToken.None);

        Assert.Equal(-3, Load(product.Id).Quantity);
        AssertLedgerConsistent();
    }

    [Fact]
    public async Task Legacy_sale_without_movements_restores_stock_only_in_inventory_mode()
    {
        var product = await NewProduct("Eski", quantity: 5);

        // Jurnalgacha qilingan sotuvni taqlid qilamiz: harakat yozilmagan
        int saleId;
        await using (var db = _db.CreateDbContext())
        {
            var sale = new Sale
            {
                Number = "OLD-1", TotalAmount = 5000, PaymentMethod = PaymentMethod.Naqd,
                SaleStatus = SaleStatus.Completed, CreatedAt = DateTime.UtcNow,
                SaleItems = { new SaleItem { ProductId = product.Id, ProductName = "Eski", UnitPrice = 5000, Quantity = 1, LineTotal = 5000, Unit = Unit.Dona } }
            };
            db.Sales.Add(sale);
            await db.SaveChangesAsync();
            saleId = sale.Id;
        }

        SetInventoryMode(false);
        await Sales(_owner).CancelAsync(saleId, null, CancellationToken.None);
        Assert.Equal(5, Load(product.Id).Quantity); // qoldiqsiz rejimda eski sotuv qoldiqni kamaytirmagan edi
    }

    // ---------- Ruxsatlar va tekshiruvlar ----------

    [Fact]
    public async Task Seller_cannot_manage_stock()
    {
        var product = await NewProduct("Tuz");
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Stock(_seller).CreateDraftAsync(StockDocumentType.Receipt,
            Lines(new StockLineInput { ProductId = product.Id, Quantity = 1, CostPrice = 100 }), CancellationToken.None));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            Suppliers(_seller).CreateAsync(new SupplierInput { Name = "X" }, CancellationToken.None));
    }

    [Fact]
    public async Task Draft_validation_rejects_bad_input()
    {
        var product = await NewProduct("Yog'");
        var stock = Stock(_manager);

        await Assert.ThrowsAsync<InvalidOperationException>(() => stock.CreateDraftAsync(
            StockDocumentType.Receipt, new StockDocumentInput(), CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => stock.CreateDraftAsync(StockDocumentType.Receipt, Lines(
            new StockLineInput { ProductId = product.Id, Quantity = 1, CostPrice = 1 },
            new StockLineInput { ProductId = product.Id, Quantity = 2, CostPrice = 1 }), CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => stock.CreateDraftAsync(StockDocumentType.Receipt,
            Lines(new StockLineInput { ProductId = product.Id, Quantity = 0, CostPrice = 1 }), CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => stock.CreateDraftAsync(StockDocumentType.Receipt,
            Lines(new StockLineInput { ProductId = product.Id, Quantity = 1, CostPrice = -5 }), CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => stock.CreateDraftAsync(StockDocumentType.WriteOff,
            new StockDocumentInput { SupplierId = 1, Lines = { new StockLineInput { ProductId = product.Id, Quantity = 1 } } },
            CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => stock.CreateDraftAsync(StockDocumentType.Receipt,
            Lines(new StockLineInput { ProductId = 9999, Quantity = 1 }), CancellationToken.None));
    }

    [Fact]
    public async Task Drafts_can_be_edited_and_deleted_but_posted_documents_cannot()
    {
        var product = await NewProduct("Kofe");
        var stock = Stock(_manager);
        var draft = await stock.CreateDraftAsync(StockDocumentType.Receipt,
            Lines(new StockLineInput { ProductId = product.Id, Quantity = 1, CostPrice = 100 }), CancellationToken.None);

        var updated = await stock.UpdateDraftAsync(draft.Id, new StockDocumentInput
        {
            Note = "Tuzatildi",
            Lines = { new StockLineInput { ProductId = product.Id, Quantity = 3, CostPrice = 200 } }
        }, CancellationToken.None);
        Assert.Equal(600, updated.TotalCost);
        Assert.Single(updated.Lines);

        await stock.PostAsync(draft.Id, CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(() => stock.UpdateDraftAsync(draft.Id, updated is null ? new() : new StockDocumentInput
        {
            Lines = { new StockLineInput { ProductId = product.Id, Quantity = 9, CostPrice = 1 } }
        }, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => stock.DeleteDraftAsync(draft.Id, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => stock.PostAsync(draft.Id, CancellationToken.None));

        var second = await stock.CreateDraftAsync(StockDocumentType.Receipt,
            Lines(new StockLineInput { ProductId = product.Id, Quantity = 1, CostPrice = 100 }), CancellationToken.None);
        await stock.DeleteDraftAsync(second.Id, CancellationToken.None);
        await using var db = _db.CreateDbContext();
        Assert.False(await db.StockDocuments.AnyAsync(x => x.Id == second.Id));
        Assert.False(await db.StockDocumentLines.AnyAsync(x => x.DocumentId == second.Id));
    }

    [Fact]
    public async Task Document_numbers_are_sequential_per_type_and_day()
    {
        var product = await NewProduct("Asal");
        var stock = Stock(_manager);
        var today = DateTime.Now.ToString("yyyyMMdd");

        var r1 = await stock.CreateDraftAsync(StockDocumentType.Receipt, Lines(new StockLineInput { ProductId = product.Id, Quantity = 1 }), CancellationToken.None);
        var r2 = await stock.CreateDraftAsync(StockDocumentType.Receipt, Lines(new StockLineInput { ProductId = product.Id, Quantity = 1 }), CancellationToken.None);
        var w1 = await stock.CreateDraftAsync(StockDocumentType.WriteOff, Lines(new StockLineInput { ProductId = product.Id, Quantity = 1 }), CancellationToken.None);
        var i1 = await stock.CreateDraftAsync(StockDocumentType.Inventory, Lines(new StockLineInput { ProductId = product.Id, Quantity = 1 }), CancellationToken.None);

        Assert.Equal($"KR-{today}-0001", r1.Number);
        Assert.Equal($"KR-{today}-0002", r2.Number);
        Assert.Equal($"HCH-{today}-0001", w1.Number);
        Assert.Equal($"INV-{today}-0001", i1.Number);

        var list = await stock.GetListAsync(new StockDocumentFilter { Type = StockDocumentType.Receipt }, CancellationToken.None);
        Assert.Equal(2, list.Count);
    }

    // ---------- Mahsulot ----------

    [Fact]
    public async Task Manual_quantity_change_on_product_card_goes_through_ledger()
    {
        var product = await NewProduct("Sovun", quantity: 3);

        await Products(_owner).UpdateAsync(new UpdateProductDto { Id = product.Id, Price = 5000, Quantity = 8 }, CancellationToken.None);

        var history = await Stock(_owner).GetProductHistoryAsync(product.Id, CancellationToken.None);
        Assert.Equal(StockMovementType.ManualAdjustment, history[0].Type);
        Assert.Equal(5, history[0].Quantity);
        AssertLedgerConsistent();
    }

    [Fact]
    public async Task Product_details_are_updated_only_when_requested()
    {
        var product = await NewProduct("Kolbasa", cost: 30_000);
        var service = Products(_owner);

        await service.UpdateAsync(new UpdateProductDto
        {
            Id = product.Id, Price = 45_000, UpdateDetails = true,
            CostPrice = 31_000, MinQuantity = 2, ScaleCode = "00042", MxikCode = "10201001001000000", IsMarked = true,
        }, CancellationToken.None);

        var stored = Load(product.Id);
        Assert.Equal(31_000, stored.CostPrice);
        Assert.Equal(2, stored.MinQuantity);
        Assert.Equal("42", stored.ScaleCode);
        Assert.True(stored.IsMarked);

        // UpdateDetails = false: eski chaqiruvlar qo'shimcha maydonlarni o'chirib yubormaydi
        await service.UpdateAsync(new UpdateProductDto { Id = product.Id, Price = 46_000 }, CancellationToken.None);
        Assert.Equal("42", Load(product.Id).ScaleCode);
        Assert.Equal(2, Load(product.Id).MinQuantity);
    }

    [Fact]
    public async Task Extra_barcodes_are_found_and_must_be_unique_across_products()
    {
        var a = await Products(_owner).CreateAsync(new CreateProductDto { Name = "Cola 1L", Price = 9000, Unit = Unit.Dona, Barcode = "4780001" }, CancellationToken.None);
        var b = await NewProduct("Fanta 1L");
        var service = Products(_owner);

        await service.AddBarcodeAsync(a.Id, "4780002", CancellationToken.None);
        Assert.Equal(a.Id, (await service.FindByBarcodeAsync("4780002"))!.Id);
        Assert.Equal(new[] { "4780002" }, await service.GetExtraBarcodesAsync(a.Id, CancellationToken.None));
        Assert.Single(await service.SearchAsync("4780002", CancellationToken.None));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddBarcodeAsync(b.Id, "4780001", CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddBarcodeAsync(b.Id, "4780002", CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(
            new CreateProductDto { Name = "Sprite", Price = 9000, Unit = Unit.Dona, Barcode = "4780002" }, CancellationToken.None));

        await service.RemoveBarcodeAsync(a.Id, "4780002", CancellationToken.None);
        Assert.Null(await service.FindByBarcodeAsync("4780002"));
    }

    [Fact]
    public async Task Scale_barcode_resolves_product_and_weight()
    {
        var service = Products(_owner);
        var meat = await service.CreateAsync(new CreateProductDto
        {
            Name = "Mol go'shti", Price = 120_000, Unit = Unit.Kg, ScaleCode = "123",
        }, CancellationToken.None);

        var code = TestEan.Build("22" + "00123" + "01250"); // PLU 123, 1.250 kg
        var result = await service.ResolveBarcodeAsync(code, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(meat.Id, result!.Product.Id);
        Assert.Equal(1.25m, result.Quantity);

        // Oddiy barcode bo'lsa vazn yo'q
        var cola = await service.CreateAsync(new CreateProductDto { Name = "Cola", Price = 9000, Unit = Unit.Dona, Barcode = "4780000000001" }, CancellationToken.None);
        var plain = await service.ResolveBarcodeAsync("4780000000001", CancellationToken.None);
        Assert.Equal(cola.Id, plain!.Product.Id);
        Assert.Null(plain.Quantity);

        Assert.Null(await service.ResolveBarcodeAsync(TestEan.Build("22" + "00999" + "01000"), CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(
            new CreateProductDto { Name = "Qo'y go'shti", Price = 110_000, Unit = Unit.Kg, ScaleCode = "00123" }, CancellationToken.None));
    }

    [Fact]
    public async Task Low_stock_lists_products_at_or_below_minimum()
    {
        var service = Products(_owner);
        var low = await NewProduct("Kam", quantity: 2);
        var ok = await NewProduct("Yetarli", quantity: 20);
        await NewProduct("Chegarasiz", quantity: 0);
        await service.UpdateAsync(new UpdateProductDto { Id = low.Id, Price = 5000, UpdateDetails = true, MinQuantity = 5 }, CancellationToken.None);
        await service.UpdateAsync(new UpdateProductDto { Id = ok.Id, Price = 5000, UpdateDetails = true, MinQuantity = 5 }, CancellationToken.None);

        var list = await Stock(_owner).GetLowStockAsync(CancellationToken.None);

        Assert.Equal(new[] { "Kam" }, list.Select(x => x.Name));
    }

    // ---------- Yetkazib beruvchi ----------

    [Fact]
    public async Task Supplier_payments_reduce_debt_and_names_are_unique()
    {
        var suppliers = Suppliers(_manager);
        var s = await suppliers.CreateAsync(new SupplierInput { Name = "Agro", Phone = " +998901234567 " }, CancellationToken.None);
        Assert.Equal("+998901234567", s.Phone);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            suppliers.CreateAsync(new SupplierInput { Name = "agro" }, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            suppliers.AddPaymentAsync(s.Id, 0, null, CancellationToken.None));

        var product = await NewProduct("Kartoshka");
        var draft = await Stock(_manager).CreateDraftAsync(StockDocumentType.Receipt, new StockDocumentInput
        {
            SupplierId = s.Id,
            Lines = { new StockLineInput { ProductId = product.Id, Quantity = 100, CostPrice = 3000 } }
        }, CancellationToken.None);
        await Stock(_manager).PostAsync(draft.Id, CancellationToken.None);

        var afterPayment = await suppliers.AddPaymentAsync(s.Id, 250_000, "Naqd", CancellationToken.None);
        Assert.Equal(50_000, afterPayment.Debt);
        Assert.Single(await suppliers.GetPaymentsAsync(s.Id, CancellationToken.None));

        // Nofaol yetkazib beruvchidan yangi kirim qilib bo'lmaydi
        await suppliers.SetActiveAsync(s.Id, false, CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Stock(_manager).CreateDraftAsync(StockDocumentType.Receipt,
            new StockDocumentInput { SupplierId = s.Id, Lines = { new StockLineInput { ProductId = product.Id, Quantity = 1 } } },
            CancellationToken.None));
    }
}

// ---------- Sof mantiq ----------

public class StockLogicTests
{
    [Theory]
    [InlineData(10, 1000, 10, 2000, 1500)]
    [InlineData(0, 1000, 5, 3000, 3000)]     // qoldiq yo'q: kirim narxi
    [InlineData(-4, 1000, 5, 3000, 3000)]    // manfiy qoldiq: kirim narxi
    [InlineData(3, 1000, 0, 9000, 1000)]     // 0 kirim: o'zgarmaydi
    [InlineData(20, 0, 100, 15000, 15000)]   // eski tannarx noma'lum (0): kirim narxi olinadi, 12 500 emas
    [InlineData(1, 1000, 2, 1001, 1001)]     // 1000.67 -> 1001
    public void Average_cost_after_receipt(double qty, long cost, double received, long receivedCost, long expected)
        => Assert.Equal(expected, CostCalculator.AfterReceipt((decimal)qty, cost, (decimal)received, receivedCost));

    [Theory]
    [InlineData(20, 1500, 10, 2000, 1000)]
    [InlineData(10, 1500, 10, 2000, 1500)]   // hech narsa qolmaydi: o'zgarmaydi
    [InlineData(5, 100, 2, 5000, 100)]       // natija manfiy: o'zgarmaydi
    public void Average_cost_after_receipt_cancel(double qty, long cost, double cancelled, long cancelledCost, long expected)
        => Assert.Equal(expected, CostCalculator.AfterReceiptCancel((decimal)qty, cost, (decimal)cancelled, cancelledCost));

    [Fact]
    public void Scale_barcode_is_parsed()
    {
        Assert.True(ScaleBarcode.TryParse(TestEan.Build("2100042" + "00750"), out var code, out var weight));
        Assert.Equal("42", code);
        Assert.Equal(0.75m, weight);
    }

    [Theory]
    [InlineData("4780000000001")]  // 2 bilan boshlanmaydi
    [InlineData("210004200750")]   // 12 ta raqam
    [InlineData("2100042007509")]  // nazorat raqami noto'g'ri
    [InlineData("21000420075A1")]
    [InlineData(null)]
    public void Invalid_scale_barcodes_are_rejected(string? code)
        => Assert.False(ScaleBarcode.TryParse(code, out _, out _));

    [Fact]
    public void Zero_weight_is_rejected()
        => Assert.False(ScaleBarcode.TryParse(TestEan.Build("2100042" + "00000"), out _, out _));
}

public static class TestEan
{
    // 12 ta raqamga EAN-13 nazorat raqamini qo'shadi
    public static string Build(string twelveDigits)
    {
        var sum = 0;
        for (var i = 0; i < 12; i++)
            sum += (twelveDigits[i] - '0') * (i % 2 == 0 ? 1 : 3);
        return twelveDigits + (10 - sum % 10) % 10;
    }
}

// ---------- Migratsiya: mavjud ma'lumotlar ko'chirilishi ----------

public class Phase1MigrationTests
{
    [Fact]
    public void Existing_quantities_become_opening_balance_and_settings_dates_are_moved()
    {
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<Savdonoma.Data.AppDbContext>().UseSqlite(connection).Options;

        using (var db = new Savdonoma.Data.AppDbContext(options))
        {
            // 0-bosqich holatigacha
            db.GetService<IMigrator>().Migrate("20261117084544_Phase0_AuditAndUserNameUnique");

            db.Database.ExecuteSqlRaw(
                "INSERT INTO SYS_PRODUCT (ID, UNIT, PRICE, IS_ACTIVE, NAME, NAME_SEARCH, QUANTITY) VALUES " +
                "(1, 'Dona', 5000, 1, 'Bor', 'Bor', 12.5), (2, 'Dona', 5000, 1, 'Yoq', 'Yoq', 0);");
            db.Database.ExecuteSqlRaw(
                "INSERT INTO SYSTEM_SETTINGS (ID, INVENTORY_ENABLED, UPDATED_AT, ORG_NAME) VALUES (1, 1, '2026-10-13 12:00:00', 'Do''kon');");
        }

        using (var db = new Savdonoma.Data.AppDbContext(options))
        {
            db.Database.Migrate();

            var movement = db.StockMovements.AsNoTracking().Single();
            Assert.Equal(1, movement.ProductId);
            Assert.Equal(StockMovementType.OpeningBalance, movement.Type);
            Assert.Equal(12.5m, movement.Quantity);
            Assert.Equal(12.5m, movement.BalanceAfter);

            var settings = db.SysSettings.AsNoTracking().Single();
            Assert.Equal(new DateTime(2026, 10, 13, 12, 0, 0), settings.CreatedAt);
            Assert.Null(settings.UpdatedAt);
            Assert.Equal("Do'kon", settings.ShopName);

            Assert.Equal(12.5m, db.Products.AsNoTracking().Single(x => x.Id == 1).Quantity);
        }
    }
}
