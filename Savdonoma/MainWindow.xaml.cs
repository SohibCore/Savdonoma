using Savdonoma.ViewModels;
using System.Windows;

namespace Savdonoma
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
    {
        public MainWindow(MainViewModel vm)
        {
            DataContext = vm;
            InitializeComponent();
        }

        private void ContactButton_Click(object sender, RoutedEventArgs e)
        {
            ContactDetailsPanel.Visibility =
                ContactDetailsPanel.Visibility == Visibility.Visible
                    ? Visibility.Collapsed
                    : Visibility.Visible;
        }
    }
}