namespace Savdonoma.Data.Services.Reports
{
    public class SalesSummaryDto
    {
        public long TotalAmount { get; set; }
        public int SalesCount { get; set; }
        public long CashAmount { get; set; }
        public long CardAmount { get; set; }
        public long AverageCheck { get; set; }
        public int CancelledCount { get; set; }
    }
}
