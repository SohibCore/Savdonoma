using System.ComponentModel.DataAnnotations;

namespace Savdonoma.Core.Enums
{
    public enum SaleStatus
    {
        [Display(Name = "Tugallandi")]
        Completed = 1,

        [Display(Name = "Tugallanmagan")]
        Cancelled = 2
    }
}
