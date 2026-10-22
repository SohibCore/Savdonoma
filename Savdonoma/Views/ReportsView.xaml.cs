using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Savdonoma.Data.Services.Reports;

namespace Savdonoma.Views
{
    /// <summary>
    /// Interaction logic for ReportsView.xaml
    /// </summary>
    public partial class ReportsView : UserControl
    {
        public ReportsView()
        {
            InitializeComponent();
        }

        private void SalesList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Clicked elementdan yuqoriga qarab ListViewItem ni topamiz
            var depObj = e.OriginalSource as DependencyObject;
            while (depObj != null && depObj is not ListViewItem)
                depObj = System.Windows.Media.VisualTreeHelper.GetParent(depObj);

            if (depObj is ListViewItem lvi && lvi.DataContext is SaleRowDto row)
            {
                row.IsExpanded = !row.IsExpanded;
                e.Handled = true;   // ListViewItem selection'ni bloklash shart emas, lekin double-fire oldini oladi
            }
        }
    }
}
