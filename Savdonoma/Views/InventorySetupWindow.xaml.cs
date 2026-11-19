using System.Windows;

namespace Savdonoma.Views
{
    public partial class InventorySetupWindow : Wpf.Ui.Controls.FluentWindow
    {
        public bool InventoryEnabled => WithStockRadio.IsChecked == true;

        public InventorySetupWindow(bool inventoryEnabled = true, bool isInitialSetup = false)
        {
            InitializeComponent();
            WithStockRadio.IsChecked = inventoryEnabled;
            WithoutStockRadio.IsChecked = !inventoryEnabled;
            ContinueButton.Content = isInitialSetup ? "Davom etish" : "Saqlash";
        }

        private void Continue_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
