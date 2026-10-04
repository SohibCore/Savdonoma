using System.IO;
using Savdonoma.Data;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Savdonoma
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Directory.CreateDirectory(AppPaths.DataFolder);

            var sc = new ServiceCollection();

            sc.AddDbContextFactory<AppDbContext>(x => x.UseSqlite($"Data Source={AppPaths.DbPath}"));
            sc.AddTransient<MainWindow>();

            ServiceProvider = sc.BuildServiceProvider();

            try
            {
                using var db = ServiceProvider
                    .GetRequiredService<IDbContextFactory<AppDbContext>>()
                    .CreateDbContext();

                db.Database.Migrate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database migration failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }

            ServiceProvider.GetRequiredService<MainWindow>().Show();
        }
    }

}
