using ClosedXML.Excel;

namespace Savdonoma.Import
{
    public class ProductReader
    {
        private static readonly string[] headers =
            { "Mahsulot", "Qoldiq", "Narx", "Birlik", "Shtrix-kod", "KategoriyaId" };

        public List<ProductImportDto> Read(string filePath)
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var workbook = new XLWorkbook(stream);

            var sheet = workbook.Worksheet(1);
            var range = sheet.RangeUsed();

            if (range is null)
                throw new InvalidTemplateException("Fayl bo'sh.");

            var rows = range.RowsUsed().ToList();

            ValidateHeader(rows[0]);

            var result = new List<ProductImportDto>();

            foreach (var row in rows.Skip(1))
            {
                var categoryText = Clean(row.Cell(6).GetString());

                int? categoryId = null;
                if (categoryText is not null && int.TryParse(categoryText, out var parsedId))
                    categoryId = parsedId;

                var item = new ProductImportDto
                {
                    RowNumber = row.RowNumber(),
                    Name = Clean(row.Cell(1).GetString()),
                    Quantity = Clean(row.Cell(2).GetString()),
                    Price = Clean(row.Cell(3).GetString()),
                    Unit = Clean(row.Cell(4).GetString()),
                    Barcode = Clean(row.Cell(5).GetString()),
                    CategoryIdText = categoryText,  
                    CategoryId = categoryId
                };

                // Butunlay bo'sh qatorlarni tashlab ketamiz
                if (item.Name is null && item.Price is null)
                    continue;

                result.Add(item);
            }

            return result;
        }

        public static void ValidateHeader(IXLRangeRow headerRow)
        {
            for (int i = 0; i < headers.Length; i++)
            {
                var actual = headerRow.Cell(i + 1).GetString().Trim();

                if (!string.Equals(actual, headers[i], StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidTemplateException(
                        $"Noto'g'ri shablon: {i + 1}-ustun \"{headers[i]}\" bo'lishi kerak, " +
                        $"lekin \"{actual}\" topildi.");
                }
            }
        }

        // Bo'sh qiymatni null qiladi, ortiqcha bo'shliqlarni kesadi
        private static string? Clean(string value)
        {
            var trimmed = value.Trim();
            return trimmed.Length == 0 ? null : trimmed;
        }
    }
}