using Savdonoma.Core.Enums;

namespace Savdonoma.Core.Entity
{
    public class AuditLog
    {
        public int Id { get; set; }
        public AuditAction Log { get; set; }
        public string OldValue { get; set; } = null!;
        public string NewValue { get; set; } = null!;
        public string EntityName { get; set; } = null!;
        public DateTime? CreatedAt { get; set; }
    }
}
