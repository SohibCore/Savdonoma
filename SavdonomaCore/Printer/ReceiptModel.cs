using Savdonoma.Core.Enums;

public class ReceiptItem
{
    public string Name { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }   // birlik narxi

    public Unit? Unit { get; set; }
    public string? Mxik { get; set; }
    public string? Barcode { get; set; }
    public decimal VatRate { get; set; } = 12;

    public decimal LineTotal => Quantity * Price;
    public decimal VatAmount =>
        Math.Round(LineTotal * VatRate / (100 + VatRate), 2);
    public string UnitDisplay => Unit.HasValue
        ? $"O'lchov birligi: {Unit.Value}"
        : "";
    public string MxikDisplay => string.IsNullOrWhiteSpace(Mxik)
        ? ""
        : $"MXIK: {Mxik}";
    public string BarcodeDisplay => string.IsNullOrWhiteSpace(Barcode)
        ? ""
        : $"Shtrix kod: {Barcode}";
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
    public decimal TotalVat => Items.Sum(i => i.VatAmount);
    public decimal Change => Paid - Total;

    public decimal VatRate { get; set; } = 12;
    public decimal Cash { get; set; }
    public decimal Card { get; set; }

}