using System.Windows.Controls;
using System.Windows;

namespace Savdonoma.Views
{
    /// <summary>
    /// Interaction logic for SaleView.xaml
    /// </summary>
    public partial class SaleView : UserControl
    {
        public SaleView()
        {
            InitializeComponent();
        }

        private void SaleProductsList_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            StretchLastColumn((ListView)sender);
        }

        private static void StretchLastColumn(ListView list)
        {
            if (list.View is not GridView view || view.Columns.Count == 0)
                return;

            var fixedWidth = view.Columns.Take(view.Columns.Count - 1).Sum(column => column.Width);
            var availableWidth = list.ActualWidth - fixedWidth - SystemParameters.VerticalScrollBarWidth;
            view.Columns[^1].Width = Math.Max(100, availableWidth);
        }
    }
}
