using System.ComponentModel;
using System.Windows;
using Savdonoma.ViewModels;

namespace Savdonoma.Views
{
    public partial class AuthWindow : Window
    {
        private readonly AuthViewModel _viewModel;

        public AuthWindow(AuthViewModel viewModel)
        {
            _viewModel = viewModel;
            DataContext = viewModel;
            InitializeComponent();
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            Loaded += AuthWindow_Loaded;
        }

        private async void AuthWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.InitializeAsync();
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AuthViewModel.IsAuthenticated) && _viewModel.IsAuthenticated)
                DialogResult = true;
        }
    }
}
