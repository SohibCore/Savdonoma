public class ProductImportResult
{
    public int Added { get; set; }
    public List<string> Skipped { get; } = new();  // takrorlar
    public List<string> Errors { get; } = new();   // xato qatorlar

    internal List<ValidProductRow> ValidRows { get; } = new();
    public int ReadyCount => ValidRows.Count;       // "N ta qo'shiladi" uchun
}
internal class ValidProductRow
{
    public string Name { get; init; } = "";
    public decimal Price { get; init; }
    public string Unit { get; init; } = "";
    public int? CategoryId { get; init; }
    public string? Barcode { get; init; }
    public decimal Quantity { get; init; }
}