using Savdonoma.Core.Enums;

public class SaleDto
{
    public int Id { get; set; }
    public string Number { get; set; } = null!;
    public DateTime? CreatedAt { get; set; }
    public long TotalAmount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public SaleStatus Status { get; set; }
}