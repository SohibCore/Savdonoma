using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

namespace Savdonoma.Data
{
    public static class AppPaths
    {
        public static string DataFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Savdonoma");

        public static string DbPath => Path.Combine(DataFolder, "savdonoma.db");
    }
}
