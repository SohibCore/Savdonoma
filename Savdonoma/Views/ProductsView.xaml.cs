using System.Windows.Controls;
using System.Windows;

namespace Savdonoma.Views
{
    /// <summary>
    /// Interaction logic for ProductsView.xaml
    /// </summary>
    public partial class ProductsView : UserControl
    {
        public ProductsView()
        {
            InitializeComponent();
        }
        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void ProductsList_SizeChanged(object sender, SizeChangedEventArgs e)
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
