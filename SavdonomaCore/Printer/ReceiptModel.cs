using Savdonoma.Core.Enums;

public class ReceiptItem
{
    public string Name { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }   // birlik narxi

    // ReceiptItem
    public Unit? Unit { get; set; }
    public string? Mxik { get; set; }
    public string? Barcode { get; set; }

}

public class ReceiptModel
{
    public string ShopName { get; set; } = "";
    public int ReceiptNumber { get; set; }
    public string Address { get; set; } = "";
    public string INN { get; set; } = "";
    public string Cashier { get; set; } = "";

    public DateTime Date { get; set; }
    public List<ReceiptItem> Items { get; set; } = new();
    public PaymentMethod PaymentType { get; set; }
    public decimal Paid { get; set; }

    public decimal Total => Items.Sum(i => i.Quantity * i.Price);
    public decimal Change => Paid - Total;

    public decimal VatRate { get; set; } = 12;
    public decimal Cash { get; set; }
    public decimal Card { get; set; }

}