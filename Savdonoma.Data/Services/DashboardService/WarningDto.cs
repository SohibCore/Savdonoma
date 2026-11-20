using Savdonoma.Data.Services.Stock;

namespace Savdonoma.Data.Services.DashboardService
{
    public class WarningDto
    {
        // Minimal qoldiqqa yetgan yoki undan kam qolgan mahsulotlar soni
        public int LowStockCount { get; set; }

        // Shulardan butunlay tugaganlari (qoldiq <= 0)
        public int OutOfStockCount { get; set; }

        // Eng ko'p yetishmayotganlari (ro'yxat qisqartirilgan)
        public List<LowStockDto> LowStockItems { get; set; } = new();
    }
}
