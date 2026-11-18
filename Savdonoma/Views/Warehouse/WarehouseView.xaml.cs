using System.Windows.Controls;
using System.Windows.Input;
using Savdonoma.ViewModels.Warehouse;

namespace Savdonoma.Views.Warehouse
{
    public partial class WarehouseView : UserControl
    {
        public WarehouseView()
        {
            InitializeComponent();
        }

        private void Documents_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is WarehouseViewModel vm && vm.SelectedDocument is not null)
                vm.OpenDocumentCommand.Execute(vm.SelectedDocument);
        }
    }
}
