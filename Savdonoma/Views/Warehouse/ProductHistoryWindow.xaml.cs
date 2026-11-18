using System.Windows;
using Savdonoma.Core.Enums;
using Savdonoma.Data.Services.Stock;
using Microsoft.Extensions.DependencyInjection;

namespace Savdonoma.Views.Warehouse
{
    // Bitta mahsulotning ombor harakatlari (kirim, sotuv, hisobdan chiqarish...)
    public partial class ProductHistoryWindow : Wpf.Ui.Controls.FluentWindow
    {
        public record Row(DateTime CreatedAt, StockMovementType Type, decimal Quantity, decimal BalanceAfter,
            long CostPrice, string Reference, string? Note)
        {
            public bool IsOut => Quantity < 0;
        }

        private ProductHistoryWindow()
        {
            InitializeComponent();
        }

        public static void Open(IServiceProvider sp, int productId, string productName)
        {
            var window = new ProductHistoryWindow { Owner = Application.Current.MainWindow };
            window.HeaderText.Text = productName;
            window.SubText.Text = "Yuklanmoqda...";
            window.Loaded += async (_, _) => await window.LoadAsync(sp.GetRequiredService<IStockService>(), productId);
            window.ShowDialog();
        }

        private async Task LoadAsync(IStockService stock, int productId)
        {
            try
            {
                var movements = await stock.GetProductHistoryAsync(productId, CancellationToken.None);
                MovementsGrid.ItemsSource = movements.Select(m => new Row(
                    m.CreatedAt, m.Type, m.Quantity, m.BalanceAfter, m.CostPrice,
                    m.DocumentNumber ?? (m.SaleNumber is null ? "" : "Chek " + m.SaleNumber),
                    m.Note)).ToList();

                SubText.Text = movements.Count == 0
                    ? "Bu mahsulot bo'yicha harakat yo'q."
                    : "Oxirgi harakatlar yuqorida. \"Narx\" ustunida: kirimda kirim narxi, boshqalarda o'sha paytdagi tannarx.";
            }
            catch (Exception ex)
            {
                SubText.Text = ex.InnerException?.Message ?? ex.Message;
            }
        }
    }
}
