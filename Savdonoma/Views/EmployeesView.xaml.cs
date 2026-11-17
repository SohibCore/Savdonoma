using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Savdonoma.ViewModels;

namespace Savdonoma.Views
{
    public partial class EmployeesView : UserControl
    {
        private EmployeesViewModel? _viewModel;

        public EmployeesView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        // PasswordBox'ni bog'lab bo'lmaydi: ViewModel parolni tozalaganda maydonni ham tozalaymiz
        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_viewModel is not null)
                _viewModel.PropertyChanged -= ViewModel_PropertyChanged;

            _viewModel = DataContext as EmployeesViewModel;

            if (_viewModel is not null)
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_viewModel is null) return;

            if (e.PropertyName == nameof(EmployeesViewModel.Password)
                && _viewModel.Password.Length == 0 && EmployeePassword.Password.Length > 0)
                EmployeePassword.Clear();

            if (e.PropertyName == nameof(EmployeesViewModel.CurrentPassword)
                && _viewModel.CurrentPassword.Length == 0 && CurrentPasswordInput.Password.Length > 0)
                CurrentPasswordInput.Clear();
        }

        private void EmployeePassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is EmployeesViewModel viewModel && sender is PasswordBox passwordBox)
                viewModel.Password = passwordBox.Password;
        }

        private void CurrentPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is EmployeesViewModel viewModel && sender is PasswordBox passwordBox)
                viewModel.CurrentPassword = passwordBox.Password;
        }
    }
}
