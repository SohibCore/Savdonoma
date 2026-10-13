using System.Windows.Controls;
using Savdonoma.ViewModels;

namespace Savdonoma.Views
{
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
        }

        private void PasswordInput_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is AuthViewModel viewModel && sender is PasswordBox passwordBox)
                viewModel.Password = passwordBox.Password;
        }
    }
}
