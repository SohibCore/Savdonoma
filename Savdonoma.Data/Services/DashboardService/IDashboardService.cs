using Savdonoma.Data.Services.Reports;

namespace Savdonoma.Data.Services.DashboardService
{
    public interface IDashboardService
    {
        // Tanlangan kunlar oralig'idagi savdo: jami, cheklar soni, o'rtacha chek, naqd/karta bo'linishi
        Task<TodayDetailDto> GetDashboardDateAsync(DateTime from, DateTime to, CancellationToken cancellation);

        // Minimal qoldiqdan kam qolgan mahsulotlar
        Task<WarningDto> GetTodayWarningAsync(CancellationToken cancellation, int take = 8);

        // Eng ko'p sotilgan mahsulotlar (summa bo'yicha)
        Task<List<TopProductDto>> GetTopProductsAsync(DateTime from, DateTime to, CancellationToken cancellation, int take = 5);

        // Xodimlar kesimida savdo (faqat rahbar va menejer uchun; sotuvchiga bo'sh ro'yxat qaytadi)
        Task<List<EmployeeSalesDto>> GetEmployeeSalesAsync(DateTime from, DateTime to, CancellationToken cancellation);
    }
}
