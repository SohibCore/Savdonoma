namespace Savdonoma.Core.Logic
{
    public static class NameNormalizer
    {
        public static string Normalize(string nameSearch)
        {
            if (string.IsNullOrWhiteSpace(nameSearch))
                return string.Empty;

            var words = nameSearch
                .Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            return string.Join(' ', words.Select(NormalizeWord));
        }
        public static string NormalizeWord(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return name;

            var result = char.ToUpperInvariant(name[0]) + name[1..].ToLowerInvariant();
            return RemoveApostrophes(result);
        }
        public static string RemoveApostrophes(string value)
        {
            return
                value.Replace("'", "")
                     .Replace("’", "")
                     .Replace("`", "")
                     .Replace("‘", "");
        }
    }
}
