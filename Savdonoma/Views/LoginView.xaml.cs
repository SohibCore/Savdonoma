using System.Windows;
using System.Windows.Controls;
using Savdonoma.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Savdonoma.Views
{
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
        }

        private void PasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is AuthViewModel viewModel && sender is PasswordBox passwordBox)
                viewModel.Password = passwordBox.Password;
        }

        private void ForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            var window = App.Services.GetRequiredService<PasswordResetWindow>();
            window.Owner = Window.GetWindow(this);

            if (window.ShowDialog() == true && DataContext is AuthViewModel viewModel)
            {
                PasswordInput.Clear();
                viewModel.OnPasswordReset(window.ViewModel.ResetUserName);
            }
        }
    }
}
