using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;

namespace Savdonoma.ViewModels
{
    public partial class MainViewModel : ObservableObject // UI ga qandaydir o'zgarishlar bo'lsa, xabar beradi
    {
        private readonly IServiceProvider _sp;
        private int _saleSessionNumber;

        public ObservableCollection<SaleViewModel> SaleSessions { get; } = new();

        [ObservableProperty] // CurrentPage o‘zgarganda UI ham o‘zgarishi kerak
        private object? currentPage;

        [ObservableProperty]
        private SaleViewModel? currentSaleSession;

        [ObservableProperty]
        private string pageTitle = "Sotuv terminali";

        [ObservableProperty]
        private string pageSubtitle = "Mahsulot tanlang va savdoni yakunlang";

        [ObservableProperty]
        private bool isSaleActive = true;

        [ObservableProperty]
        private bool isProductsActive;

        [ObservableProperty]
        private bool isReportsActive;

        public string TodayText => DateTime.Now.ToString("dd.MM.yyyy • dddd");

        public MainViewModel(IServiceProvider sp)
        {
            _sp = sp;
            AddSaleSession();
        }

        partial void OnCurrentSaleSessionChanged(SaleViewModel? value)
        {
            if (IsSaleActive && value != null)
                CurrentPage = value;
        }

        [RelayCommand]
        private void ShowSale()
        {
            IsSaleActive = true;
            IsProductsActive = false;
            IsReportsActive = false;
            CurrentSaleSession ??= SaleSessions.FirstOrDefault();
            if (CurrentSaleSession == null)
                AddSaleSession();
            else
                CurrentPage = CurrentSaleSession;

            PageTitle = "Sotuv terminali";
            PageSubtitle = "Mahsulot tanlang va savdoni yakunlang";
        }

        [RelayCommand]
        private void AddSaleSession()
        {
            var session = _sp.GetRequiredService<SaleViewModel>();
            session.SessionTitle = $"Sotuv {++_saleSessionNumber}";
            SaleSessions.Add(session);
            IsSaleActive = true;
            IsProductsActive = false;
            IsReportsActive = false;
            CurrentSaleSession = session;
            CurrentPage = session;
            PageTitle = "Sotuv terminali";
            PageSubtitle = "Mahsulot tanlang va savdoni yakunlang";
        }

        [RelayCommand]
        private void CloseSaleSession(SaleViewModel? session)
        {
            if (session == null || SaleSessions.Count <= 1)
                return;

            var index = SaleSessions.IndexOf(session);
            SaleSessions.Remove(session);

            if (CurrentSaleSession == session)
                CurrentSaleSession = SaleSessions[Math.Min(index, SaleSessions.Count - 1)];
        }

        [RelayCommand]
        private void ShowProducts()
        {
            CurrentPage = _sp.GetRequiredService<ProductsViewModel>();
            PageTitle = "Mahsulotlar";
            PageSubtitle = "Katalog, narx va qoldiqni boshqaring";
            IsSaleActive = false;
            IsProductsActive = true;
            IsReportsActive = false;
        }

        [RelayCommand]
        private void ShowReports()
        {
            CurrentPage = _sp.GetRequiredService<ReportsViewModel>();
            PageTitle = "Hisobotlar";
            PageSubtitle = "Kunlik, haftalik va oylik tahlil";
            IsSaleActive = false;
            IsProductsActive = false;
            IsReportsActive = true;
            _ = ((ReportsViewModel)CurrentPage).LoadAsync();
        }
    }
}
