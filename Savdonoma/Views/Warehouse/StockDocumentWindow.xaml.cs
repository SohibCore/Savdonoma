using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using Savdonoma.Data.Services.Products;
using Savdonoma.ViewModels.Warehouse;

namespace Savdonoma.Views.Warehouse
{
    public partial class StockDocumentWindow : Wpf.Ui.Controls.FluentWindow
    {
        private readonly StockDocumentEditorViewModel _vm;

        public StockDocumentEditorViewModel ViewModel => _vm;

        public StockDocumentWindow(StockDocumentEditorViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            DataContext = vm;

            vm.CloseRequested += Close;
            vm.ProductScanned += OnProductScanned;
            Closed += (_, _) =>
            {
                vm.CloseRequested -= Close;
                vm.ProductScanned -= OnProductScanned;
            };
            Loaded += (_, _) => ScanBox.Focus();
        }

        // "+ Mahsulot qo'shish": avval mahsulot tanlanadi, keyin miqdor va narx kiritiladi
        private void AddLine_Click(object sender, RoutedEventArgs e) => OpenLineDialog(null, null, null);

        // Skanerlangan mahsulot: tanlash qadami o'tkazib yuboriladi
        private void OnProductScanned(ProductDto product, decimal? quantity)
        {
            var existing = _vm.FindLine(product.Id);
            OpenLineDialog(existing, existing is null ? product : null, quantity);
        }

        private void EditLine_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { Tag: StockLineViewModel line })
                OpenLineDialog(line, null, null);
        }

        private void Lines_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_vm.IsEditable && sender is DataGrid { SelectedItem: StockLineViewModel line })
                OpenLineDialog(line, null, null);
        }

        private void OpenLineDialog(StockLineViewModel? existing, ProductDto? product, decimal? quantity)
        {
            if (!_vm.IsEditable)
                return;

            var result = StockLineDialog.ShowFor(this, _vm, existing, product, quantity);
            if (result is not null)
            {
                if (existing is not null)
                    _vm.ReplaceLine(existing, result);
                else
                    _vm.AddOrMergeLine(result);
            }

            ScanBox.Focus();
        }

        private void CancelDocument_Click(object sender, RoutedEventArgs e)
        {
            var reason = TextPromptWindow.Ask(this,
                "Hujjatni bekor qilish",
                "Bekor qilish sababini yozing. Qoldiq va tannarx hujjatdan oldingi holatga qaytadi.");

            if (reason is not null)
                _vm.CancelDocumentCommand.Execute(reason);
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
