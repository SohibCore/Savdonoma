namespace Savdonoma.Data.Services.DashboardService
{
    public class TodayDetailDto
    {
        public long TotalAmount { get; set; }
        public int SalesCount { get; set; }
        public long AverageCheck { get; set; }
        public int CancelledCount { get; set; }

        // Count - cheklar soni, Total - summa (so'm)
        public int CashCount { get; set; }
        public long CashTotal { get; set; }

        public int CardCount { get; set; }
        public long CardTotal { get; set; }
    }
}
