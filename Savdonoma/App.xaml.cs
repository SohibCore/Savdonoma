using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Savdonoma.Data;
using Savdonoma.Data.Services.Products;
using Savdonoma.Data.Services.Reports;
using Savdonoma.Data.Services.Sales;
using Savdonoma.ViewModels;
using System.IO;
using System.Windows;
using System.Windows.Markup;

namespace Savdonoma
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage("ru-RU"))); //tilni o'gartirish

            Directory.CreateDirectory(AppPaths.DataFolder);

            var sc = new ServiceCollection();

            // Baza
            sc.AddDbContextFactory<AppDbContext>(o =>
                o.UseSqlite($"Data Source={AppPaths.DbPath}"));

            // Servislar
            sc.AddTransient<IProductService, ProductService>();
            sc.AddTransient<ISaleService, SaleService>();
            sc.AddTransient<IReportService, ReportService>();

            // ViewModel'lar
            sc.AddSingleton<MainViewModel>();
            sc.AddSingleton<SaleViewModel>();
            sc.AddSingleton<ProductsViewModel>();
            sc.AddSingleton<ReportsViewModel>();

            // Oyna
            sc.AddTransient<MainWindow>();

            Services = sc.BuildServiceProvider();

            try
            {
                using var db = Services
                    .GetRequiredService<IDbContextFactory<AppDbContext>>()
                    .CreateDbContext();
                db.Database.Migrate();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Bazani yaratishda xato:\n" + ex.Message,
                    "Savdonoma", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            Services.GetRequiredService<MainWindow>().Show();
        }
    }
}