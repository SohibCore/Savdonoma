using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace Savdonoma.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IServiceProvider _sp;

        [ObservableProperty]
        private object? currentPage;

        public MainViewModel(IServiceProvider sp)
        {
            _sp = sp;
            ShowSale();
        }

        [RelayCommand] private void ShowSale() => CurrentPage = _sp.GetRequiredService<SaleViewModel>();
        [RelayCommand] private void ShowProducts() => CurrentPage = _sp.GetRequiredService<ProductsViewModel>();
        [RelayCommand] private void ShowReports() => CurrentPage = _sp.GetRequiredService<ReportsViewModel>();
    }
}