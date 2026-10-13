using System.Windows.Controls;
using Savdonoma.ViewModels;

namespace Savdonoma.Views
{
    public partial class EmployeesView : UserControl
    {
        public EmployeesView()
        {
            InitializeComponent();
        }

        private void EmployeePassword_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is EmployeesViewModel viewModel && sender is PasswordBox passwordBox)
                viewModel.Password = passwordBox.Password;
        }
    }
}
