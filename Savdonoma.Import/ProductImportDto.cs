public class ProductImportDto
{
    public int RowNumber { get; init; }
    public string? Name { get; init; }
    public string? Quantity { get; init; }
    public string? Price { get; init; }
    public string? Unit { get; init; }
    public string? Barcode { get; init; }
    public string? CategoryIdText { get; init; }   // Excel'dagi xom matn
    public int? CategoryId { get; init; }          // muvaffaqiyatli o'girilgan qiymat
}