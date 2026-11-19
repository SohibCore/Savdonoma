using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Savdonoma.ViewModels;

namespace Savdonoma.Views
{
    public partial class OrganizationView : UserControl
    {
        private OrganizationViewModel? _viewModel;

        public OrganizationView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_viewModel is not null)
                _viewModel.PropertyChanged -= ViewModel_PropertyChanged;

            _viewModel = DataContext as OrganizationViewModel;
            if (_viewModel is not null)
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OrganizationViewModel.OwnerCurrentPassword)
                && _viewModel?.OwnerCurrentPassword.Length == 0
                && OwnerCurrentPasswordInput.Password.Length > 0)
                OwnerCurrentPasswordInput.Clear();
        }

        private void OwnerCurrentPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is OrganizationViewModel viewModel && sender is PasswordBox passwordBox)
                viewModel.OwnerCurrentPassword = passwordBox.Password;
        }
    }
}
