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

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}