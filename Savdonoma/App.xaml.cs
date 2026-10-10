using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Savdonoma.Core.Licensing;
using Savdonoma.Data;
using Savdonoma.Data.Licensing;
using Savdonoma.Data.Services.Categories;
using Savdonoma.Data.Services.Products;
using Savdonoma.Data.Services.Reports;
using Savdonoma.Data.Services.Sales;
using Savdonoma.ViewModels;
using Savdonoma.Views;
using System.IO;
using System.Windows;
using System.Windows.Markup;

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
            sc.AddSingleton<ILicenseService, LicenseService>();   // singleton: bitta taymer

            // ViewModel'lar
            sc.AddSingleton<MainViewModel>();
            sc.AddTransient<SaleViewModel>();
            sc.AddSingleton<ProductsViewModel>();
            sc.AddSingleton<ReportsViewModel>();
            sc.AddSingleton<LicenseViewModel>();

            // Oynalar
            sc.AddTransient<ActivationWindow>();
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
                    return;
                }
                catch (Exception ex) when (IsDatabaseLocked(ex) && attempt < 2)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(300 * (attempt + 1)));
                }
            }
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