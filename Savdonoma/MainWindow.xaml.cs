using Savdonoma.BarcodeService;
using Savdonoma.ViewModels;
using System.Windows;

namespace Savdonoma
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
    {
        public MainWindow(MainViewModel vm, IBarcodeScannerService scanner)
        {
            DataContext = vm;
            InitializeComponent();
            scanner.Attach(this);
        }

        private void ContactButton_Click(object sender, RoutedEventArgs e)
        {
            // Yopiq menyuda bosilsa, avval menyuni ochib, kontaktlarni ko'rsatamiz
            if (DataContext is MainViewModel { IsMainMenuExpanded: false } vm)
            {
                vm.IsMainMenuExpanded = true;
                ContactDetailsPanel.Visibility = Visibility.Visible;
                return;
            }

            ContactDetailsPanel.Visibility =
                ContactDetailsPanel.Visibility == Visibility.Visible
                    ? Visibility.Collapsed
                    : Visibility.Visible;
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}