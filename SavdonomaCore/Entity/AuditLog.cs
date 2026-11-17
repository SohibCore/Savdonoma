using Savdonoma.Core.Enums;

namespace Savdonoma.Core.Entity
{
    public class AuditLog
    {
        public int Id { get; set; }
        public AuditAction Log { get; set; }

        // O'zgargan maydonlar JSON ko'rinishida: {"Price": 12000}
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }

        // Qaysi jadval va qaysi yozuv: "Product", "15"
        public string EntityName { get; set; } = null!;
        public string? EntityId { get; set; }

        // UTC vaqt
        public DateTime? CreatedAt { get; set; }
    }
}
