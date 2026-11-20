using Savdonoma.Core.Enums;

namespace Savdonoma.Data.Services.DashboardService
{
    // Bitta xodimning tanlangan davrdagi savdosi
    public class EmployeeSalesDto
    {
        public int? UserId { get; set; }           // null - xodimga bog'lanmagan eski sotuvlar
        public string FullName { get; set; } = "";
        public UserRole? Role { get; set; }
        public bool IsActive { get; set; }

        public int SalesCount { get; set; }        // tugallangan cheklar soni
        public long TotalAmount { get; set; }      // tugallangan cheklar summasi
        public long AverageCheck { get; set; }
        public long CashTotal { get; set; }
        public long CardTotal { get; set; }
        public int CancelledCount { get; set; }
    }
}
