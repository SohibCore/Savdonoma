using System.Windows;

namespace Savdonoma.Views
{
    public partial class InventorySetupWindow : Wpf.Ui.Controls.FluentWindow
    {
        public bool InventoryEnabled => InventoryCheckBox.IsChecked == true;

        public InventorySetupWindow()
        {
            InitializeComponent();
        }

        private void Continue_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
