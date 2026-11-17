namespace Savdonoma.Data
{
    public static class AppPaths
    {
        public static string DataFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Savdonoma");

        public static string DbPath => Path.Combine(DataFolder, "savdonoma.db");

        public static string BackupFolder => Path.Combine(DataFolder, "Backups");
    }
}
