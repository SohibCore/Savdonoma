using Savdonoma.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Savdonoma.Tests;

public class BackupTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "savdonoma-tests-" + Guid.NewGuid().ToString("N"));
    private string DbPath => Path.Combine(_dir, "savdonoma.db");
    private string BackupDir => Path.Combine(_dir, "Backups");

    public BackupTests()
    {
        Directory.CreateDirectory(_dir);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={DbPath};Pooling=False")
            .Options;
        using var db = new AppDbContext(options);
        db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
        db.Database.Migrate();
        db.Categories.Add(new Savdonoma.Core.Entity.Category { Name = "Ichimliklar", IsActive = true });
        db.SaveChanges();
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(_dir, recursive: true); } catch (IOException) { }
    }

    [Fact]
    public void Daily_backup_is_created_once_per_day_and_contains_data()
    {
        var now = new DateTime(2026, 11, 17, 10, 0, 0);

        var first = DatabaseBackupService.CreateDailyBackupIfNeeded(DbPath, BackupDir, now);
        var second = DatabaseBackupService.CreateDailyBackupIfNeeded(DbPath, BackupDir, now.AddHours(3));

        Assert.NotNull(first);
        Assert.Null(second);
        Assert.Equal("savdonoma-20261117.db", Path.GetFileName(first));

        using var conn = new SqliteConnection($"Data Source={first};Mode=ReadOnly;Pooling=False");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT NAME FROM SYS_CATEGORY";
        Assert.Equal("Ichimliklar", cmd.ExecuteScalar());
    }

    [Fact]
    public void Old_daily_backups_are_cleaned_up()
    {
        var start = new DateTime(2026, 1, 1);
        for (var i = 0; i < DatabaseBackupService.DailyBackupsToKeep + 5; i++)
            DatabaseBackupService.CreateDailyBackupIfNeeded(DbPath, BackupDir, start.AddDays(i));

        var files = Directory.GetFiles(BackupDir, "savdonoma-*.db").Select(Path.GetFileName).OrderBy(x => x).ToList();
        Assert.Equal(DatabaseBackupService.DailyBackupsToKeep, files.Count);
        Assert.Equal("savdonoma-20260106.db", files[0]); // eng eski 5 tasi o'chirilgan
    }

    [Fact]
    public void Missing_database_is_not_backed_up()
    {
        Assert.Null(DatabaseBackupService.CreateDailyBackupIfNeeded(Path.Combine(_dir, "yoq.db"), BackupDir, DateTime.Now));
        Assert.Null(DatabaseBackupService.CreateMigrationBackup(Path.Combine(_dir, "yoq.db"), BackupDir, DateTime.Now));
    }
}
