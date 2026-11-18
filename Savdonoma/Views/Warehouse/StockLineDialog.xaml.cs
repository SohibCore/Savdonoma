using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.ComponentModel;
using Savdonoma.Data.Services.Products;
using Savdonoma.ViewModels.Warehouse;

namespace Savdonoma.Views.Warehouse
{
    // Hujjatga mahsulot qo'shish / qatorni tahrirlash oynasi
    public partial class StockLineDialog : Wpf.Ui.Controls.FluentWindow
    {
        private readonly StockLineDialogViewModel _vm;

        private StockLineDialog(StockLineDialogViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            DataContext = vm;

            vm.Finished += ok => DialogResult = ok;
            vm.PropertyChanged += Vm_PropertyChanged;
            Loaded += async (_, _) =>
            {
                await vm.InitializeAsync();
                FocusCurrentStep();
            };
        }

        // Yangi qator qo'shish (product berilsa, tanlash qadami o'tkazib yuboriladi).
        // Natija: qo'shilgan/tahrirlangan qator yoki null (bekor qilindi).
        public static StockLineViewModel? ShowFor(Window owner, StockDocumentEditorViewModel document,
            StockLineViewModel? existing = null, ProductDto? product = null, decimal? quantity = null)
        {
            var vm = new StockLineDialogViewModel(document, existing);
            if (existing is null && product is not null)
                vm.Choose(product, quantity);

            var dialog = new StockLineDialog(vm) { Owner = owner };
            return dialog.ShowDialog() == true ? vm.Line : null;
        }

        private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(StockLineDialogViewModel.Line))
                Dispatcher.BeginInvoke(FocusCurrentStep, DispatcherPriority.Input);
        }

        // Tanlashda qidiruv maydoni, ma'lumot kiritishda miqdor maydoni fokusda bo'ladi
        private void FocusCurrentStep()
        {
            if (_vm.IsFilling)
            {
                QuantityBox.Focus();
                QuantityBox.SelectAll();
            }
            else
            {
                SearchBox.Focus();
            }
        }

        // Qidiruvda pastga strelka: ro'yxatga o'tish
        private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down && ProductList.Items.Count > 0)
            {
                ProductList.SelectedIndex = Math.Max(0, ProductList.SelectedIndex);
                ProductList.UpdateLayout();
                (ProductList.ItemContainerGenerator.ContainerFromIndex(ProductList.SelectedIndex) as UIElement)?.Focus();
                e.Handled = true;
            }
        }

        private void ProductList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && ProductList.SelectedItem is ProductDto product)
            {
                _vm.ChooseProductCommand.Execute(product);
                e.Handled = true;
            }
        }

        private void ProductList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ProductList.SelectedItem is ProductDto product)
                _vm.ChooseProductCommand.Execute(product);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
