using System.IO;
using Savdonoma.Data;
using System.Windows;
using Savdonoma.Views;
using Savdonoma.Services;
using Savdonoma.ViewModels;
using Savdonoma.Import;
using Microsoft.Data.Sqlite;
using System.Windows.Markup;
using Savdonoma.Core.Licensing;
using Savdonoma.Core.Logic;
using Savdonoma.Data.Licensing;
using Microsoft.EntityFrameworkCore;
using Savdonoma.Data.Services.Sales;
using Savdonoma.Data.Services.Reports;
using Savdonoma.Data.Services.Products;
using Savdonoma.Data.Services.Categories;
using Savdonoma.Data.Services.Authservice;
using Savdonoma.Data.Services.UserService;
using Savdonoma.Data.Services.SystemSetting;
using Microsoft.Extensions.DependencyInjection;

namespace Savdonoma
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; } = null!;
        private Mutex? _instanceMutex;
        private bool _ownsInstanceMutex;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Faollashtirish oynasi yopilganda dastur o'zi o'chib ketmasligi uchun
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _instanceMutex = new Mutex(
                initiallyOwned: true,
                name: @"Local\Savdonoma",
                createdNew: out _ownsInstanceMutex);

            if (!_ownsInstanceMutex)
            {
                _instanceMutex.Dispose();
                _instanceMutex = null;
                MessageBox.Show(
                    "Savdonoma allaqachon ishga tushgan. Bitta nusxadan foydalaning.",
                    "Savdonoma",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                Shutdown();
                return;
            }

            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage("ru-RU"))); // tilni o'zgartirish

            Directory.CreateDirectory(AppPaths.DataFolder);

            var sc = new ServiceCollection();

            // Baza
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = AppPaths.DbPath,
                DefaultTimeout = 1
            }.ToString();

            sc.AddDbContextFactory<AppDbContext>(o =>
                o.UseSqlite(connectionString));

            // Servislar
            sc.AddTransient<IProductService, ProductService>();
            sc.AddTransient<ISaleService, SaleService>();
            sc.AddTransient<IReportService, ReportService>();
            sc.AddTransient<ICategoryService, CategoryService>();
            sc.AddSingleton<IMachineIdProvider, MachineIdProvider>();
            sc.AddSingleton<ILicenseService, LicenseService>();
            sc.AddTransient<IAuthservice, AuthService>();
            sc.AddTransient<IUserService, UserService>();
            sc.AddSingleton<CurrentUserService>();
            sc.AddTransient<ISystemSettingsService, SystemSettingsService>();
            sc.AddTransient<ProductReader>();
            sc.AddTransient<ProductImportService>();

            // ViewModel'lar
            sc.AddSingleton<MainViewModel>();
            sc.AddTransient<SaleViewModel>();
            sc.AddSingleton<ProductsViewModel>();
            sc.AddSingleton<ReportsViewModel>();
            sc.AddSingleton<LicenseViewModel>();
            sc.AddTransient<AuthViewModel>();
            sc.AddTransient<EmployeesViewModel>();

            // Oynalar
            sc.AddTransient<ActivationWindow>();
            sc.AddTransient<AuthWindow>();
            sc.AddTransient<MainWindow>();

            Services = sc.BuildServiceProvider();

            try
            {
                await InitializeDatabaseAsync();
            }
            catch (Exception ex)
            {
                var message = IsDatabaseLocked(ex)
                    ? "Baza boshqa dastur yoki Savdonoma nusxasi tomonidan band. " +
                      "Boshqa Savdonoma oynalari va bazani ishlatayotgan dasturlarni yoping, keyin qayta ishga tushiring."
                    : ex.ToString();
                MessageBox.Show(message, "Bazani yaratishda xatolik", MessageBoxButton.OK, MessageBoxImage.Error);

                Shutdown();
                return;
            }

            // Litsenziya: faollashtirilmagan yoki fayl buzilgan bo'lsa, faollashtirish oynasi
            var license = Services.GetRequiredService<ILicenseService>();
            if (license.Current.Status is LicenseStatus.NoLicense or LicenseStatus.Invalid)
            {
                var activation = Services.GetRequiredService<ActivationWindow>();
                if (activation.ShowDialog() != true)
                {
                    Shutdown();
                    return;
                }
            }

            var auth = Services.GetRequiredService<AuthWindow>();
            if (auth.ShowDialog() != true)
            {
                Shutdown();
                return;
            }

            var settingsService = Services.GetRequiredService<ISystemSettingsService>();

            try
            {
                var exists = await settingsService.ReadExist(CancellationToken.None);

                if (!exists)
                {
                    var setupWindow = new InventorySetupWindow();
                    if (setupWindow.ShowDialog() != true)
                    {
                        Shutdown();
                        return;
                    }

                    await settingsService.SaveAsync(
                        setupWindow.InventoryEnabled,
                        CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Sozlamani saqlashda xatolik", MessageBoxButton.OK,
                MessageBoxImage.Error);

                Shutdown();
                return;
            }

            // Muddati tugagan (Expired) bo'lsa, dastur ochiladi: faqat ko'rish rejimi va qizil banner
            var main = Services.GetRequiredService<MainWindow>();
            MainWindow = main;
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            main.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_ownsInstanceMutex)
                _instanceMutex?.ReleaseMutex();

            _instanceMutex?.Dispose();
            base.OnExit(e);
        }

        private static async Task InitializeDatabaseAsync()
        {
            var factory = Services.GetRequiredService<IDbContextFactory<AppDbContext>>();

            for (var attempt = 0; ; attempt++)
            {
                try
                {
                    using var db = factory.CreateDbContext();
                    await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
                    await db.Database.MigrateAsync();
                    await DbSeeder.SeedAsync(db);
                    await RepairImportedProductsAsync(db);
                    return;
                }
                catch (Exception ex) when (IsDatabaseLocked(ex) && attempt < 2)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(300 * (attempt + 1)));
                }
            }
        }

        private static async Task RepairImportedProductsAsync(AppDbContext db)
        {
            var importedProducts = await db.Products
                .Where(product => !product.IsActive && product.UpdatedAt == null)
                .ToListAsync();

            if (importedProducts.Count == 0)
                return;

            foreach (var product in importedProducts)
            {
                product.IsActive = true;
                if (string.IsNullOrWhiteSpace(product.NameSearch))
                    product.NameSearch = NameNormalizer.Normalize(product.Name);
            }

            await db.SaveChangesAsync();
        }

        private static bool IsDatabaseLocked(Exception exception)
        {
            for (Exception? current = exception; current != null; current = current.InnerException)
            {
                if (current is SqliteException sqliteException
                    && sqliteException.SqliteErrorCode is 5 or 6)
                    return true;
            }

            return false;
        }
    }
}