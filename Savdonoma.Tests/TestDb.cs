using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Savdonoma.Core.Licensing;
using Savdonoma.Data;

namespace Savdonoma.Tests;

// Xotiradagi SQLite baza: haqiqiy migratsiyalar noldan qo'llanadi
public sealed class TestDb : IDbContextFactory<AppDbContext>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public TestDb()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var db = CreateDbContext();
        db.Database.Migrate();
    }

    public AppDbContext CreateDbContext() => new(_options);

    public void Dispose() => _connection.Dispose();
}

public sealed class FakeLicense : ILicenseService
{
    public LicenseState Current { get; } = new(LicenseStatus.Valid, "Test", null, 0, "", IsPerpetual: true);
    public string MachineId => "TEST-0000-0000-0000";
    public event EventHandler<LicenseState>? StatusChanged { add { } remove { } }
    public LicenseState Refresh() => Current;
    public LicenseState Activate(string licenseKey) => Current;
    public void EnsureCanWrite() { }
}
