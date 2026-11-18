using System.Windows;
using Savdonoma.Core.Printer;

namespace Savdonoma.Views
{
    public partial class ReceiptPreviewWindow : Window
    {
        private readonly ReceiptModel _receipt;
        private readonly IReceiptPrinter _printer;

        public ReceiptPreviewWindow(ReceiptModel receipt, IReceiptPrinter printer)
        {
            InitializeComponent();
            _receipt = receipt;
            _printer = printer;
            DataContext = receipt;
        }

        private async void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            PrintButton.IsEnabled = false;
            PrintStatus.Text = "";

            try
            {
                var printed = await Task.Run(() =>
                    _printer.PrintCheck(_receipt, CancellationToken.None));

                PrintStatus.Text = printed
                    ? "Chek printerga yuborildi."
                    : "Chek printerga yuborilmadi. Printer nomi va ulanishini tekshiring.";
            }
            catch (Exception ex)
            {
                PrintStatus.Text = $"Chek chiqarishda xatolik: {ex.Message}";
            }
            finally
            {
                PrintButton.IsEnabled = true;
            }
        }
    }
}
