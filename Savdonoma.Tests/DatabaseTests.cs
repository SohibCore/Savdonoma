using Microsoft.EntityFrameworkCore;
using Savdonoma.Core.Entity;
using Savdonoma.Core.Enums;
using Savdonoma.Core.Licensing;
using Savdonoma.Data.Licensing;
using Savdonoma.Data.Services.Authservice;
using Savdonoma.Data.Services.Reports;
using Savdonoma.Data.Services.UserService;
using Savdonoma.Services;

namespace Savdonoma.Tests;

public class DatabaseTests : IDisposable
{
    private readonly TestDb _db = new();

    public void Dispose() => _db.Dispose();

    private User AddUser(string userName, UserRole role)
    {
        using var db = _db.CreateDbContext();
        var user = new User
        {
            FullName = userName,
            UserName = userName,
            Password = BCrypt.Net.BCrypt.HashPassword("eski"),
            OrgName = "TestStore",
            UserRole = role,
            IsActive = true,
            SelfRegister = role == UserRole.Boshliq,
            CreatedAt = DateTime.UtcNow,
        };
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    private Product AddProduct(string name = "Non", long price = 4000)
    {
        using var db = _db.CreateDbContext();
        var product = new Product
        {
            Name = name, NameSearch = name, Price = price, Unit = Unit.Dona,
            IsActive = true, Quantity = 10, CreatedAt = DateTime.UtcNow,
        };
        db.Products.Add(product);
        db.SaveChanges();
        return product;
    }

    private void AddSale(Product product, SaleStatus status, PaymentMethod method, long total, int? userId = null)
    {
        using var db = _db.CreateDbContext();
        db.Sales.Add(new Sale
        {
            Number = Guid.NewGuid().ToString("N")[..10],
            TotalAmount = total,
            PaymentMethod = method,
            SaleStatus = status,
            CreatedAt = DateTime.UtcNow,
            UserId = userId,
            SaleItems =
            {
                new SaleItem
                {
                    ProductId = product.Id, ProductName = product.Name, UnitPrice = total,
                    Quantity = 1, LineTotal = total, Unit = Unit.Dona, CreatedAt = DateTime.UtcNow,
                }
            }
        });
        db.SaveChanges();
    }

    private UserService OwnerUserService(User owner)
    {
        var current = new CurrentUserService();
        current.SignIn(new AuthResultDto
        {
            UserId = owner.Id, UserName = owner.UserName, OrgName = owner.OrgName, UserRole = owner.UserRole,
        });
        return new UserService(_db, current, new FakeLicense());
    }

    private static CurrentUserService Signed(UserRole role, int userId = 1)
    {
        var current = new CurrentUserService();
        current.SignIn(new AuthResultDto { UserId = userId, UserName = "u", OrgName = "TestStore", UserRole = role });
        return current;
    }

    [Fact]
    public async Task First_owner_registration_saves_store_address_and_inn()
    {
        var service = new UserService(_db, new CurrentUserService(), new FakeLicense());

        await service.RegisterFirstOwnerAsync(new CreateUserDto
        {
            FullName = "Do‘kon egasi",
            UserName = "owner",
            Password = "1234",
            OrgName = "Do‘kon",
            INN = "123456789",
            Address = "Toshkent shahri",
        }, CancellationToken.None);

        await using var check = _db.CreateDbContext();
        var owner = await check.Users.SingleAsync();
        Assert.Equal("123456789", owner.INN);
        Assert.Equal("Toshkent shahri", owner.Address);
    }

    // ---------- Audit log ----------

    [Fact]
    public async Task Audit_update_records_entity_id_and_changed_fields_only()
    {
        var product = AddProduct(price: 4000);

        await using (var db = _db.CreateDbContext())
        {
            var p = await db.Products.SingleAsync(x => x.Id == product.Id);
            p.Price = 4500;
            await db.SaveChangesAsync();
        }

        await using var check = _db.CreateDbContext();
        var log = await check.AuditLogs.SingleAsync();
        Assert.Equal(AuditAction.UPDATE, log.Log);
        Assert.Equal("Product", log.EntityName);
        Assert.Equal(product.Id.ToString(), log.EntityId);
        Assert.Equal("{\"Price\":4000}", log.OldValue);
        Assert.Equal("{\"Price\":4500}", log.NewValue);
        Assert.NotNull(log.CreatedAt);
    }

    [Fact]
    public async Task Audit_uses_real_entity_name_and_masks_password()
    {
        var user = AddUser("kassir", UserRole.Sotuvchi);

        await using (var db = _db.CreateDbContext())
        {
            var u = await db.Users.SingleAsync(x => x.Id == user.Id);
            u.Password = BCrypt.Net.BCrypt.HashPassword("yangi");
            await db.SaveChangesAsync();
        }

        await using var check = _db.CreateDbContext();
        var log = await check.AuditLogs.SingleAsync();
        Assert.Equal("User", log.EntityName);
        Assert.Equal("{\"Password\":\"***\"}", log.OldValue);
        Assert.Equal("{\"Password\":\"***\"}", log.NewValue);
    }

    [Fact]
    public async Task Audit_delete_stores_old_values_without_failing()
    {
        await using (var db = _db.CreateDbContext())
        {
            db.Categories.Add(new Category { Name = "Test", IsActive = true });
            await db.SaveChangesAsync();
        }

        await using (var db = _db.CreateDbContext())
        {
            db.Categories.Remove(await db.Categories.SingleAsync());
            await db.SaveChangesAsync();
        }

        await using var check = _db.CreateDbContext();
        var log = await check.AuditLogs.SingleAsync();
        Assert.Equal(AuditAction.DELETE, log.Log);
        Assert.Equal("Category", log.EntityName);
        Assert.Contains("\"Name\":\"Test\"", log.OldValue);
        Assert.Null(log.NewValue);
    }

    [Fact]
    public async Task Audit_skips_updates_without_real_change()
    {
        var product = AddProduct(price: 4000);

        await using (var db = _db.CreateDbContext())
        {
            var p = await db.Products.SingleAsync(x => x.Id == product.Id);
            db.Entry(p).Property(x => x.Price).IsModified = true; // qiymat o'zgarmagan
            await db.SaveChangesAsync();
        }

        await using var check = _db.CreateDbContext();
        Assert.Empty(await check.AuditLogs.ToListAsync());
    }

    // ---------- Xodimni o'chirish ----------

    [Fact]
    public async Task Deleting_employee_without_sales_removes_it()
    {
        var owner = AddUser("boss", UserRole.Boshliq);
        var cashier = AddUser("kassir", UserRole.Sotuvchi);

        var result = await OwnerUserService(owner).DeleteAsync(cashier.Id, CancellationToken.None);

        Assert.Equal(UserDeleteResult.Deleted, result);
        await using var check = _db.CreateDbContext();
        Assert.False(await check.Users.AnyAsync(x => x.Id == cashier.Id));
    }

    [Fact]
    public async Task Deleting_employee_with_sales_deactivates_it()
    {
        var owner = AddUser("boss", UserRole.Boshliq);
        var cashier = AddUser("kassir", UserRole.Sotuvchi);
        AddSale(AddProduct(), SaleStatus.Completed, PaymentMethod.Naqd, 4000, cashier.Id);

        var service = OwnerUserService(owner);
        var result = await service.DeleteAsync(cashier.Id, CancellationToken.None);

        Assert.Equal(UserDeleteResult.Deactivated, result);
        await using var check = _db.CreateDbContext();
        var stored = await check.Users.SingleAsync(x => x.Id == cashier.Id);
        Assert.False(stored.IsActive);

        // Odatiy ro'yxatda faqat rahbar qoladi, nofaollar alohida ko'rsatiladi
        var activeOnly = await service.GetListAsync(includeInactive: false, CancellationToken.None);
        Assert.Equal(new[] { "boss" }, activeOnly.Select(x => x.UserName));
        var withInactive = await service.GetListAsync(includeInactive: true, CancellationToken.None);
        Assert.Equal(new[] { "boss", "kassir" }, withInactive.Select(x => x.UserName));
        Assert.False(withInactive[1].IsActive);

        // Nofaol xodim tizimga kira olmaydi
        var auth = new AuthService(_db);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            auth.Login(new LoginDto { UserName = "kassir", Password = "eski" }, CancellationToken.None));

        // Uning logini band, xabar qayta faollashtirishni taklif qiladi
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateEmployeeAsync(
            new CreateUserDto { FullName = "Yangi", UserName = "kassir", Password = "1234", UserRole = UserRole.Sotuvchi },
            CancellationToken.None));
        Assert.Contains("nofaol", ex.Message);

        // Qayta faollashtirilgach yana kira oladi
        await service.ReactivateAsync(cashier.Id, CancellationToken.None);
        var login = await auth.Login(new LoginDto { UserName = "kassir", Password = "eski" }, CancellationToken.None);
        Assert.Equal(cashier.Id, login.UserId);
    }

    [Fact]
    public async Task User_list_shows_owner_first_without_password_hash()
    {
        var owner = AddUser("boss", UserRole.Boshliq);
        AddUser("kassir", UserRole.Sotuvchi);

        var list = await OwnerUserService(owner).GetListAsync(includeInactive: false, CancellationToken.None);

        Assert.Equal(new[] { "boss", "kassir" }, list.Select(x => x.UserName));
        Assert.True(list[0].IsOwner);
        Assert.Equal("Rahbar", list[0].RoleDisplay);
        Assert.All(list, x => Assert.Equal("", x.Password));
    }

    [Fact]
    public async Task Owner_cannot_delete_self_or_change_own_role()
    {
        var owner = AddUser("boss", UserRole.Boshliq);
        var service = OwnerUserService(owner);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteAsync(owner.Id, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(
            new UpdateUserDto { Id = owner.Id, UserRole = UserRole.Sotuvchi }, CancellationToken.None));
    }

    [Fact]
    public async Task Owner_changing_login_or_password_requires_current_password()
    {
        var owner = AddUser("boss", UserRole.Boshliq);
        var service = OwnerUserService(owner);

        // Joriy parolsiz yoki noto'g'ri parol bilan bo'lmaydi
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.UpdateAsync(
            new UpdateUserDto { Id = owner.Id, UserName = "rahbar", Password = "yangi123" }, CancellationToken.None));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.UpdateAsync(
            new UpdateUserDto { Id = owner.Id, Password = "yangi123", CurrentPassword = "xato" }, CancellationToken.None));

        // Faqat ismni o'zgartirish uchun joriy parol shart emas
        await service.UpdateAsync(new UpdateUserDto { Id = owner.Id, FullName = "Bosh Rahbar", UserName = "boss" },
            CancellationToken.None);

        // To'g'ri joriy parol bilan login va parol o'zgaradi
        await service.UpdateAsync(new UpdateUserDto
        {
            Id = owner.Id, UserName = "Rahbar", Password = "yangi123", CurrentPassword = "eski",
        }, CancellationToken.None);

        var login = await new AuthService(_db).Login(
            new LoginDto { UserName = "rahbar", Password = "yangi123" }, CancellationToken.None);
        Assert.Equal(owner.Id, login.UserId);

        await using var check = _db.CreateDbContext();
        Assert.Equal("Bosh Rahbar", (await check.Users.SingleAsync(x => x.Id == owner.Id)).FullName);
    }

    [Fact]
    public async Task Owner_cannot_see_or_edit_owner_of_other_shop()
    {
        var owner = AddUser("boss", UserRole.Boshliq);
        var other = AddUser("boss2", UserRole.Boshliq);
        var service = OwnerUserService(owner);

        var list = await service.GetListAsync(includeInactive: true, CancellationToken.None);
        Assert.DoesNotContain(list, x => x.Id == other.Id);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(
            new UpdateUserDto { Id = other.Id, FullName = "X" }, CancellationToken.None));
    }

    [Fact]
    public void Duplicate_user_name_is_rejected_by_database()
    {
        AddUser("kassir", UserRole.Sotuvchi);
        Assert.Throws<DbUpdateException>(() => AddUser("kassir", UserRole.Sotuvchi));
    }

    // ---------- Hisobot ----------

    [Fact]
    public async Task Summary_excludes_cancelled_sales_from_amounts()
    {
        var product = AddProduct();
        AddSale(product, SaleStatus.Completed, PaymentMethod.Naqd, 10_000);
        AddSale(product, SaleStatus.Completed, PaymentMethod.Karta, 20_000);
        AddSale(product, SaleStatus.Cancelled, PaymentMethod.Naqd, 50_000);

        var today = DateTime.Today;
        var summary = await new ReportService(_db, Signed(UserRole.Boshliq)).GetSummaryAsync(today, today, CancellationToken.None);

        Assert.Equal(30_000, summary.TotalAmount);
        Assert.Equal(2, summary.SalesCount);
        Assert.Equal(10_000, summary.CashAmount);
        Assert.Equal(20_000, summary.CardAmount);
        Assert.Equal(15_000, summary.AverageCheck);
        Assert.Equal(1, summary.CancelledCount);
    }

    // ---------- Parolni tiklash ----------

    [Fact]
    public async Task Owner_password_is_reset_with_valid_code_only()
    {
        var owner = AddUser("boss", UserRole.Boshliq);
        AddUser("kassir", UserRole.Sotuvchi);

        var keys = LicenseCrypto.GenerateKeyPair();
        var now = new DateTime(2026, 11, 17, 6, 0, 0, DateTimeKind.Utc);
        var service = new PasswordResetService(_db, "AB12-CD34-EF56-7890", keys.PublicKey, () => now);

        Assert.Equal("boss", await service.GetOwnerUserNameAsync(CancellationToken.None));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.ResetOwnerPasswordAsync("notogri-kod", "yangi123", CancellationToken.None));

        var code = PasswordResetCode.Create(service.MachineId, service.RequestCode,
            LicenseEvaluator.TodayInTashkent(now), keys.PrivateKey);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ResetOwnerPasswordAsync(code, "123", CancellationToken.None)); // juda qisqa

        var userName = await service.ResetOwnerPasswordAsync(code, "yangi123", CancellationToken.None);
        Assert.Equal("boss", userName);

        var result = await new AuthService(_db).Login(
            new LoginDto { UserName = "boss", Password = "yangi123" }, CancellationToken.None);
        Assert.Equal(owner.Id, result.UserId);
    }

    [Fact]
    public async Task Reset_code_for_another_request_is_rejected()
    {
        AddUser("boss", UserRole.Boshliq);
        var keys = LicenseCrypto.GenerateKeyPair();
        var now = DateTime.UtcNow;

        var first = new PasswordResetService(_db, "AB12-CD34-EF56-7890", keys.PublicKey, () => now);
        var second = new PasswordResetService(_db, "AB12-CD34-EF56-7890", keys.PublicKey, () => now);

        var codeForFirst = PasswordResetCode.Create(first.MachineId, first.RequestCode,
            LicenseEvaluator.TodayInTashkent(now), keys.PrivateKey);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            second.ResetOwnerPasswordAsync(codeForFirst, "yangi123", CancellationToken.None));
    }
}
