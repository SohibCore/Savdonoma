using ClosedXML.Excel;
using Savdonoma.Core.Entity;
using Savdonoma.Core.Enums;
using Savdonoma.Import;

namespace Savdonoma.Tests;

public class ImportTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"import-{Guid.NewGuid():N}.xlsx");

    public void Dispose()
    {
        _db.Dispose();
        File.Delete(_file);
    }

    private void WriteFile(params string[][] rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Mahsulotlar");
        string[] headers = { "Mahsulot", "Qoldiq", "Narx", "Birlik", "Shtrix-kod", "KategoriyaId" };
        for (var c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];

        for (var r = 0; r < rows.Length; r++)
            for (var c = 0; c < rows[r].Length; c++)
                ws.Cell(r + 2, c + 1).Value = rows[r][c];

        wb.SaveAs(_file);
    }

    [Fact]
    public async Task Duplicate_barcodes_are_reported_instead_of_crashing_commit()
    {
        using (var db = _db.CreateDbContext())
        {
            db.Products.Add(new Product
            {
                Name = "Mavjud", NameSearch = "Mavjud", Price = 1000, Unit = Unit.Dona,
                IsActive = true, Barcode = "4780000000001",
            });
            db.SaveChanges();
        }

        WriteFile(
            new[] { "Yangi 1", "5", "2000", "dona", "4780000000001", "" }, // bazada bor
            new[] { "Yangi 2", "5", "2000", "dona", " 4780000000002 ", "" },
            new[] { "Yangi 3", "5", "2000", "dona", "4780000000002", "" }, // fayl ichida takror
            new[] { "Yangi 4", "5", "2000", "dona", "", "" });

        var service = new ProductImportService(_db, new ProductReader(), new FakeLicense());
        var result = await service.ValidateAsync(_file);

        Assert.Equal(new[] { "Yangi 2", "Yangi 4" }, result.ValidRows.Select(x => x.Name));
        Assert.Equal("4780000000002", result.ValidRows[0].Barcode);
        Assert.Null(result.ValidRows[1].Barcode);
        Assert.Equal(2, result.Errors.Count(e => e.Contains("barcode")));

        await service.CommitAsync(result);
        Assert.Equal(2, result.Added);
    }
}
