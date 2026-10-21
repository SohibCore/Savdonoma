using Savdonoma.Core.Enums;

public class ReceiptItem
{
    public string Name { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }   // birlik narxi
}

public class ReceiptModel
{
    public string ShopName { get; set; } = "";
    public int ReceiptNumber { get; set; }

    public DateTime Date { get; set; }
    public string Cashier { get; set; } = "";

    public List<ReceiptItem> Items { get; set; } = new();

    public PaymentMethod PaymentType { get; set; } 
    public decimal Paid { get; set; }

    public decimal Total => Items.Sum(i => i.Quantity * i.Price);
    public decimal Change => Paid - Total;
}