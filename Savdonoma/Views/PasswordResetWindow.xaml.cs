using System.Windows;
using System.Windows.Controls;
using Savdonoma.ViewModels;

namespace Savdonoma.Views
{
    public partial class PasswordResetWindow : Wpf.Ui.Controls.FluentWindow
    {
        private readonly PasswordResetViewModel _vm;

        public PasswordResetViewModel ViewModel => _vm;

        public PasswordResetWindow(PasswordResetViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            DataContext = vm;

            vm.Completed += OnCompleted;
            Closed += (_, _) => vm.Completed -= OnCompleted;
            Loaded += async (_, _) => await vm.InitializeAsync();
        }

        private void OnCompleted() => DialogResult = true;

        private void NewPassword_Changed(object sender, RoutedEventArgs e)
            => _vm.NewPassword = ((PasswordBox)sender).Password;

        private void ConfirmPassword_Changed(object sender, RoutedEventArgs e)
            => _vm.ConfirmPassword = ((PasswordBox)sender).Password;
    }
}
