using System.Globalization;
using Microsoft.Data.Sqlite;

namespace Savdonoma.Data
{
    // SQLite bazaning zaxira nusxalarini yaratadi va eskilarini tozalaydi.
    // SQLite Backup API ishlatiladi: WAL rejimida ham nusxa to'liq va izchil bo'ladi.
    public static class DatabaseBackupService
    {
        public const int DailyBackupsToKeep = 30;
        public const int MigrationBackupsToKeep = 10;

        private const string DailyPrefix = "savdonoma-";
        private const string MigrationPrefix = "pre-migration-";

        // Bugun uchun nusxa hali olinmagan bo'lsa, oladi. Yaratilgan fayl yo'lini qaytaradi.
        public static string? CreateDailyBackupIfNeeded(string dbPath, string backupFolder, DateTime now)
        {
            if (!File.Exists(dbPath))
                return null;

            Directory.CreateDirectory(backupFolder);
            var target = Path.Combine(backupFolder, $"{DailyPrefix}{now:yyyyMMdd}.db");
            if (File.Exists(target))
                return null;

            Backup(dbPath, target);
            Cleanup(backupFolder, DailyPrefix, DailyBackupsToKeep);
            return target;
        }

        // Migratsiya qo'llashdan oldin alohida nusxa
        public static string? CreateMigrationBackup(string dbPath, string backupFolder, DateTime now)
        {
            if (!File.Exists(dbPath))
                return null;

            Directory.CreateDirectory(backupFolder);
            var target = Path.Combine(backupFolder,
                $"{MigrationPrefix}{now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}.db");

            Backup(dbPath, target);
            Cleanup(backupFolder, MigrationPrefix, MigrationBackupsToKeep);
            return target;
        }

        public static void Backup(string sourcePath, string targetPath)
        {
            var tempPath = targetPath + ".tmp";
            if (File.Exists(tempPath))
                File.Delete(tempPath);

            using (var source = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = sourcePath,
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = false,
            }.ToString()))
            using (var target = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = tempPath,
                Pooling = false,
            }.ToString()))
            {
                source.Open();
                target.Open();
                source.BackupDatabase(target);
            }

            // Nusxa to'liq yozilgandan keyingina asosiy nomga o'tkazamiz
            File.Move(tempPath, targetPath, overwrite: true);
        }

        private static void Cleanup(string backupFolder, string prefix, int keep)
        {
            var old = new DirectoryInfo(backupFolder)
                .GetFiles($"{prefix}*.db")
                .OrderByDescending(f => f.Name, StringComparer.Ordinal)
                .Skip(keep);

            foreach (var file in old)
            {
                try { file.Delete(); }
                catch (IOException) { /* keyingi safar o'chiriladi */ }
                catch (UnauthorizedAccessException) { }
            }
        }
    }
}
